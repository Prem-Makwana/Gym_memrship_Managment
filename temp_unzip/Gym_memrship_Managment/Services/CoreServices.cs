using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMembershipService _membershipService;
        private readonly IAuditService _auditService;

        public AttendanceService(ApplicationDbContext db, IMembershipService membershipService, IAuditService auditService)
        {
            _db = db;
            _membershipService = membershipService;
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, Attendance? Attendance)> CheckInAsync(
            int memberId, int? batchId, bool overrideChecks = false)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            try
            {
                return await strategy.ExecuteAsync<(bool Success, string Message, Attendance? Attendance)>(async () =>
                {
                    bool isRootTx = _db.Database.CurrentTransaction == null;
                    if (isRootTx) _db.ChangeTracker.Clear();

                    var tx = isRootTx ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted) : null;
                    try
                    {
                        var member = await _db.MemberProfiles.FindAsync(memberId);
                        if (member == null) return (false, "Member not found.", null);

                        if (!overrideChecks)
                        {
                            if (member.Status != MemberStatus.Active)
                                return (false, "Member account is not active.", null);

                            var activeMembership = await _membershipService.GetActiveMembershipAsync(memberId);
                            if (activeMembership == null)
                                return (false, "No active membership found.", null);

                            var pendingFines = await _db.Fines
                                .Where(f => f.MemberId == memberId && f.Status == FineStatus.Pending)
                                .SumAsync(f => f.Amount);
                            if (pendingFines > 0)
                                return (false, $"Member has outstanding fines of {pendingFines:C}. Please clear before check-in.", null);
                        }

                        var existing = await GetActiveCheckInAsync(memberId);
                        if (existing != null)
                            return (false, "Member already has an active check-in.", existing);

                        var attendance = new Attendance
                        {
                            MemberId = memberId,
                            BatchId = batchId,
                            Date = DateTime.UtcNow.Date,
                            CheckInTime = DateTime.UtcNow,
                            Status = AttendanceStatus.Present
                        };
                        _db.Attendances.Add(attendance);
                        await _db.SaveChangesAsync();

                        if (overrideChecks)
                            await _auditService.LogAsync(null, "system", "CheckInOverride",
                                "Attendance", attendance.AttendanceId.ToString(),
                                $"Override check-in for member {memberId}", null);

                        if (tx != null) await tx.CommitAsync();
                        return (true, "Check-in successful.", attendance);
                    }
                    catch (DbUpdateException)
                    {
                        if (tx != null) await tx.RollbackAsync();
                        return (false, "Member already has an active check-in.", null);
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
                return (false, $"Check-in failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> CheckOutAsync(int attendanceId)
        {
            var attendance = await _db.Attendances.FindAsync(attendanceId);
            if (attendance == null) return (false, "Attendance record not found.");
            if (attendance.CheckOutTime.HasValue) return (false, "Already checked out.");

            attendance.CheckOutTime = DateTime.UtcNow;
            attendance.DurationMinutes = (int)(DateTime.UtcNow - attendance.CheckInTime).TotalMinutes;
            attendance.Status = AttendanceStatus.CheckedOut;
            await _db.SaveChangesAsync();

            return (true, "Check-out successful.");
        }

        public async Task<List<Attendance>> GetTodayAttendanceAsync(int? batchId = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _db.Attendances
                .Include(a => a.Member)
                .Include(a => a.Batch)
                .Where(a => a.Date == today);

            if (batchId.HasValue)
                query = query.Where(a => a.BatchId == batchId.Value);

            return await query.OrderByDescending(a => a.CheckInTime).ToListAsync();
        }

        public async Task<Attendance?> GetActiveCheckInAsync(int memberId)
        {
            var today = DateTime.UtcNow.Date;
            return await _db.Attendances
                .Where(a => a.MemberId == memberId && a.Date == today && a.CheckOutTime == null)
                .FirstOrDefaultAsync();
        }
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;

        public PaymentService(ApplicationDbContext db) => _db = db;

        public async Task<string> GenerateReceiptNumberAsync()
        {
            var prefix = $"RCP{DateTime.UtcNow:yyyyMM}";
            var count = await _db.Payments.CountAsync(p => p.ReceiptNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D4}";
        }

        public async Task<Payment> RecordPaymentAsync(int memberId, int? membershipId, decimal amount,
            PaymentMethod method, PaymentType type, string? reference, string? notes, string? receivedBy)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<Payment>(async () =>
            {
                int maxRetries = 3;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    bool isRootTx = _db.Database.CurrentTransaction == null;
                    if (isRootTx) _db.ChangeTracker.Clear();

                    var tx = isRootTx ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted) : null;
                    try
                    {
                        var receipt = await GenerateReceiptNumberAsync();
                        var payment = new Payment
                        {
                            ReceiptNumber = receipt,
                            MemberId = memberId,
                            MembershipId = membershipId,
                            PaymentDate = DateTime.UtcNow,
                            Amount = amount,
                            PaymentMethod = method,
                            PaymentType = type,
                            TransactionReference = reference,
                            Notes = notes,
                            ReceivedBy = receivedBy,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Payments.Add(payment);
                        await _db.SaveChangesAsync();
                        
                        if (tx != null) await tx.CommitAsync();
                        return payment;
                    }
                    catch (DbUpdateException)
                    {
                        if (tx != null) await tx.RollbackAsync();
                        if (attempt == maxRetries - 1) throw new Exception("Failed to generate a unique receipt number after multiple attempts.");
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
                }
                throw new Exception("Failed to record payment due to concurrent receipt generation.");
            });
        }

        public async Task<List<Payment>> GetMemberLedgerAsync(int memberId)
            => await _db.Payments
                .Include(p => p.Membership)
                .ThenInclude(m => m!.Plan)
                .Where(p => p.MemberId == memberId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

        public async Task<decimal> GetTodayCollectionAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _db.Payments
                .Where(p => p.PaymentDate.Date == today)
                .SumAsync(p => p.Amount);
        }

        public async Task<List<MonthlyRevenue>> GetMonthlyRevenueAsync(int months = 12)
        {
            var from = DateTime.UtcNow.AddMonths(-months + 1);
            var payments = await _db.Payments
                .Where(p => p.PaymentDate >= from)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();
            return payments;
        }
    }

    public class FineService : IFineService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;

        public FineService(ApplicationDbContext db, IPaymentService paymentService)
        {
            _db = db;
            _paymentService = paymentService;
        }

        public async Task<Fine> CreateFineAsync(int memberId, int? membershipId, string reason, decimal amount)
        {
            var fine = new Fine
            {
                MemberId = memberId,
                MembershipId = membershipId,
                Reason = reason,
                Amount = amount,
                DateIssued = DateTime.UtcNow,
                Status = FineStatus.Pending
            };
            _db.Fines.Add(fine);
            await _db.SaveChangesAsync();
            return fine;
        }

        public async Task<bool> PayFineAsync(int fineId, string receivedBy)
        {
            var fine = await _db.Fines.FindAsync(fineId);
            if (fine == null || fine.Status != FineStatus.Pending) return false;

            fine.Status = FineStatus.Paid;
            fine.PaidDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _paymentService.RecordPaymentAsync(fine.MemberId, fine.MembershipId,
                fine.Amount, PaymentMethod.Cash, PaymentType.Fine,
                null, $"Fine payment: {fine.Reason}", receivedBy);
            return true;
        }

        public async Task<bool> WaiveFineAsync(int fineId, string notes)
        {
            var fine = await _db.Fines.FindAsync(fineId);
            if (fine == null || fine.Status != FineStatus.Pending) return false;
            fine.Status = FineStatus.Waived;
            fine.Notes = notes;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetPendingFinesAmountAsync(int memberId)
            => await _db.Fines
                .Where(f => f.MemberId == memberId && f.Status == FineStatus.Pending)
                .SumAsync(f => f.Amount);

        public async Task AutoGenerateLateFinesAsync(int membershipId)
        {
            var membership = await _db.Memberships.Include(m => m.Plan).FirstOrDefaultAsync(m => m.MembershipId == membershipId);
            if (membership == null) return;

            var today = DateTime.UtcNow.Date;
            var gracePeriodEnd = membership.EndDate.Date.AddDays(membership.Plan.GracePeriodDays);
            if (today <= gracePeriodEnd) return;

            var lateDays = (today - gracePeriodEnd).Days;
            var fineAmount = lateDays * membership.Plan.LateFeePerDay;

            var existingFine = await _db.Fines
                .AnyAsync(f => f.MembershipId == membershipId && f.Reason.Contains("Late renewal"));
            if (!existingFine)
                await CreateFineAsync(membership.MemberId, membershipId, $"Late renewal fee ({lateDays} days)", fineAmount);
        }
    }

    public class BatchService : IBatchService
    {
        private readonly ApplicationDbContext _db;

        public BatchService(ApplicationDbContext db) => _db = db;

        public async Task<(bool Success, string Message)> EnrollMemberAsync(int memberId, int batchId)
        {
            var batch = await _db.Batches.Include(b => b.Enrollments).FirstOrDefaultAsync(b => b.BatchId == batchId);
            if (batch == null || !batch.IsActive) return (false, "Batch not found or inactive.");

            var activeCount = batch.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
            if (activeCount >= batch.MaximumCapacity) return (false, "Batch is at full capacity.");

            var existing = await _db.BatchEnrollments
                .AnyAsync(be => be.MemberId == memberId && be.BatchId == batchId && be.Status == EnrollmentStatus.Active);
            if (existing) return (false, "Member is already enrolled in this batch.");

            _db.BatchEnrollments.Add(new BatchEnrollment
            {
                MemberId = memberId,
                BatchId = batchId,
                EnrolledDate = DateTime.UtcNow,
                Status = EnrollmentStatus.Active
            });
            await _db.SaveChangesAsync();
            return (true, "Member enrolled in batch successfully.");
        }

        public async Task<(bool Success, string Message)> TransferMemberAsync(int memberId, int fromBatchId, int toBatchId)
        {
            var enrollment = await _db.BatchEnrollments
                .FirstOrDefaultAsync(be => be.MemberId == memberId && be.BatchId == fromBatchId && be.Status == EnrollmentStatus.Active);
            if (enrollment == null) return (false, "Active enrollment not found in source batch.");

            var (success, message) = await EnrollMemberAsync(memberId, toBatchId);
            if (!success) return (false, message);

            enrollment.Status = EnrollmentStatus.Transferred;
            enrollment.TransferredDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, "Member transferred successfully.");
        }

        public async Task<(bool Success, string Message)> DropMemberAsync(int memberId, int batchId)
        {
            var enrollment = await _db.BatchEnrollments
                .FirstOrDefaultAsync(be => be.MemberId == memberId && be.BatchId == batchId && be.Status == EnrollmentStatus.Active);
            if (enrollment == null) return (false, "Active enrollment not found.");

            enrollment.Status = EnrollmentStatus.Dropped;
            await _db.SaveChangesAsync();
            return (true, "Member dropped from batch.");
        }

        public async Task<int> GetCurrentOccupancyAsync(int batchId)
            => await _db.BatchEnrollments
                .CountAsync(be => be.BatchId == batchId && be.Status == EnrollmentStatus.Active);

        public async Task<List<BatchEnrollment>> GetBatchMembersAsync(int batchId)
            => await _db.BatchEnrollments
                .Include(be => be.Member)
                .Where(be => be.BatchId == batchId && be.Status == EnrollmentStatus.Active)
                .ToListAsync();
    }
}
