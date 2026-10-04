using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IAuditService _auditService;

        public EnrollmentService(ApplicationDbContext db, IPaymentService paymentService, IAuditService auditService)
        {
            _db = db;
            _paymentService = paymentService;
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, int? MembershipId)> EnrollAsync(
            int memberId, int planId, DateTime startDate, decimal discount, string receivedBy)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            try
            {
                return await strategy.ExecuteAsync<(bool Success, string Message, int? MembershipId)>(async () =>
                {
                    bool isRootTx = _db.Database.CurrentTransaction == null;
                    if (isRootTx) _db.ChangeTracker.Clear();

                    var tx = isRootTx ? await _db.Database.BeginTransactionAsync() : null;
                    try
                    {
                        var member = await _db.MemberProfiles.FindAsync(memberId);
                        if (member == null) return (false, "Member not found.", null);
                        if (member.Status != MemberStatus.Active) return (false, "Member account is not active.", null);

                        var plan = await _db.MembershipPlans.FindAsync(planId);
                        if (plan == null || !plan.IsActive) return (false, "Plan not found or inactive.", null);

                        var existing = await _db.Memberships
                            .Where(m => m.MemberId == memberId &&
                                        m.EndDate >= DateTime.UtcNow.Date &&
                                        m.OverrideStatus != MembershipStatus.Cancelled &&
                                        m.OverrideStatus != MembershipStatus.Suspended)
                            .FirstOrDefaultAsync();
                        if (existing != null) return (false, "Member already has an active membership.", null);

                        var endDate = startDate.AddDays(plan.DurationInDays);
                        var baseAmount = plan.Price + plan.RegistrationFee;
                        var finalAmount = baseAmount - discount;

                        var membership = new Membership
                        {
                            MemberId = memberId,
                            PlanId = planId,
                            StartDate = startDate,
                            EndDate = endDate,
                            BaseAmount = baseAmount,
                            Discount = discount,
                            FinalAmount = finalAmount,
                            PaidAmount = 0,
                            DueAmount = finalAmount,
                            RenewalCount = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Memberships.Add(membership);
                        await _db.SaveChangesAsync();

                        if (finalAmount > 0)
                        {
                            var payment = await _paymentService.RecordPaymentAsync(
                                memberId, membership.MembershipId, finalAmount,
                                PaymentMethod.Cash, PaymentType.Registration,
                                null, "Initial enrollment payment", receivedBy);

                            membership.PaidAmount = finalAmount;
                            membership.DueAmount = 0;
                            await _db.SaveChangesAsync();
                        }

                        await _auditService.LogAsync(null, receivedBy, "Enroll",
                            "Membership", membership.MembershipId.ToString(),
                            $"Enrolled member {memberId} to plan {planId}", null);

                        if (tx != null) await tx.CommitAsync();
                        return (true, "Enrollment successful.", membership.MembershipId);
                    }
                    catch
                    {
                        if (tx != null) await tx.RollbackAsync();
                        throw;
                    }
                    finally
                    {
                        if (tx != null) await tx.DisposeAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                return (false, $"Enrollment failed: {ex.Message}", null);
            }
        }
    }

    public class RenewalService : IRenewalService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IFineService _fineService;
        private readonly IAuditService _auditService;

        public RenewalService(ApplicationDbContext db, IPaymentService paymentService,
            IFineService fineService, IAuditService auditService)
        {
            _db = db;
            _paymentService = paymentService;
            _fineService = fineService;
            _auditService = auditService;
        }

        public async Task<decimal> CalculateLateFeeAsync(int membershipId)
        {
            var membership = await _db.Memberships.Include(m => m.Plan).FirstOrDefaultAsync(m => m.MembershipId == membershipId);
            if (membership == null) return 0;

            var today = DateTime.UtcNow.Date;
            var gracePeriodEnd = membership.EndDate.Date.AddDays(membership.Plan.GracePeriodDays);
            if (today <= gracePeriodEnd) return 0;

            var lateDays = (today - gracePeriodEnd).Days;
            return lateDays * membership.Plan.LateFeePerDay;
        }

        public async Task<(bool Success, string Message, int? MembershipId)> RenewAsync(
            int existingMembershipId, int? newPlanId, decimal discount, string receivedBy)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            try
            {
                return await strategy.ExecuteAsync<(bool Success, string Message, int? MembershipId)>(async () =>
                {
                    bool isRootTx = _db.Database.CurrentTransaction == null;
                    if (isRootTx) _db.ChangeTracker.Clear();

                    var tx = isRootTx ? await _db.Database.BeginTransactionAsync() : null;
                    try
                    {
                        var existing = await _db.Memberships
                            .Include(m => m.Plan)
                            .Include(m => m.Member)
                            .FirstOrDefaultAsync(m => m.MembershipId == existingMembershipId);

                        if (existing == null) return (false, "Membership not found.", null);

                        var plan = newPlanId.HasValue
                            ? await _db.MembershipPlans.FindAsync(newPlanId.Value)
                            : existing.Plan;
                        if (plan == null || !plan.IsActive) return (false, "Plan not found or inactive.", null);

                        var lateFee = await CalculateLateFeeAsync(existingMembershipId);
                        if (lateFee > 0)
                            await _fineService.CreateFineAsync(existing.MemberId, existingMembershipId, "Late renewal fee", lateFee);

                        var startDate = existing.EndDate.Date >= DateTime.UtcNow.Date
                            ? existing.EndDate.Date.AddDays(1)
                            : DateTime.UtcNow.Date;
                        var endDate = startDate.AddDays(plan.DurationInDays);
                        var baseAmount = plan.Price + plan.RenewalFee + lateFee;
                        var finalAmount = baseAmount - discount;

                        var newMembership = new Membership
                        {
                            MemberId = existing.MemberId,
                            PlanId = plan.PlanId,
                            StartDate = startDate,
                            EndDate = endDate,
                            BaseAmount = plan.Price + plan.RenewalFee,
                            Discount = discount,
                            FinalAmount = finalAmount,
                            PaidAmount = 0,
                            DueAmount = finalAmount,
                            RenewalCount = existing.RenewalCount + 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Memberships.Add(newMembership);
                        await _db.SaveChangesAsync();

                        if (finalAmount > 0)
                        {
                            await _paymentService.RecordPaymentAsync(
                                existing.MemberId, newMembership.MembershipId, finalAmount,
                                PaymentMethod.Cash, PaymentType.Renewal,
                                null, "Membership renewal", receivedBy);

                            newMembership.PaidAmount = finalAmount;
                            newMembership.DueAmount = 0;
                            await _db.SaveChangesAsync();
                        }

                        await _auditService.LogAsync(null, receivedBy, "Renew",
                            "Membership", newMembership.MembershipId.ToString(),
                            $"Renewed membership for member {existing.MemberId}", null);

                        if (tx != null) await tx.CommitAsync();
                        return (true, "Renewal successful.", newMembership.MembershipId);
                    }
                    catch
                    {
                        if (tx != null) await tx.RollbackAsync();
                        throw;
                    }
                    finally
                    {
                        if (tx != null) await tx.DisposeAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                return (false, $"Renewal failed: {ex.Message}", null);
            }
        }
    }
}
