using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISystemSettingService _settings;

        public MembershipService(ApplicationDbContext db, ISystemSettingService settings)
        {
            _db = db;
            _settings = settings;
        }

        public MembershipStatus ComputeStatus(Membership membership, int expiryThresholdDays = 7)
        {
            if (membership.OverrideStatus.HasValue &&
                (membership.OverrideStatus == MembershipStatus.Suspended ||
                 membership.OverrideStatus == MembershipStatus.Cancelled))
                return membership.OverrideStatus.Value;

            var today = DateTime.UtcNow.Date;
            if (membership.EndDate.Date < today) return MembershipStatus.Expired;
            if ((membership.EndDate.Date - today).TotalDays <= expiryThresholdDays) return MembershipStatus.ExpiringSoon;
            return MembershipStatus.Active;
        }

        public int GetDaysRemaining(Membership membership)
            => Math.Max(0, (membership.EndDate.Date - DateTime.UtcNow.Date).Days);

        public async Task<Membership?> GetActiveMembershipAsync(int memberId)
        {
            var memberships = await _db.Memberships
                .Include(m => m.Plan)
                .Where(m => m.MemberId == memberId &&
                            m.OverrideStatus != MembershipStatus.Cancelled &&
                            m.OverrideStatus != MembershipStatus.Suspended)
                .OrderByDescending(m => m.EndDate)
                .ToListAsync();

            return memberships.FirstOrDefault(m => ComputeStatus(m) != MembershipStatus.Expired);
        }

        public async Task<List<Membership>> GetExpiringMembershipsAsync(int thresholdDays)
        {
            var today = DateTime.UtcNow.Date;
            var threshold = today.AddDays(thresholdDays);
            return await _db.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .Where(m => m.EndDate.Date >= today && m.EndDate.Date <= threshold &&
                            m.OverrideStatus != MembershipStatus.Cancelled &&
                            m.OverrideStatus != MembershipStatus.Suspended)
                .OrderBy(m => m.EndDate)
                .ToListAsync();
        }

        public async Task SuspendAsync(int membershipId, string reason)
        {
            var m = await _db.Memberships.FindAsync(membershipId);
            if (m != null)
            {
                m.OverrideStatus = MembershipStatus.Suspended;
                m.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task CancelAsync(int membershipId, string reason)
        {
            var m = await _db.Memberships.FindAsync(membershipId);
            if (m != null)
            {
                m.OverrideStatus = MembershipStatus.Cancelled;
                m.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task ReactivateAsync(int membershipId)
        {
            var m = await _db.Memberships.FindAsync(membershipId);
            if (m != null)
            {
                m.OverrideStatus = null;
                m.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
