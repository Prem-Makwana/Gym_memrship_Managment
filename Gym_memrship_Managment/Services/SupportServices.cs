using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Gym_memrship_Managment.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db) => _db = db;

        public async Task SendAsync(string userId, string title, string message, NotificationType type)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadAsync(string userId)
            => await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

        public async Task MarkReadAsync(int notificationId)
        {
            var n = await _db.Notifications.FindAsync(notificationId);
            if (n != null) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in notifications) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; }
            await _db.SaveChangesAsync();
        }

                public async Task<int> GetUnreadCountAsync(string userId)
            => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task BroadcastAsync(string title, string message, NotificationType type, string targetAudience, string? specificUserId = null)
        {
            var usersQuery = _db.Users.AsQueryable();
            
            if (targetAudience == "Staff")
            {
                var roleId = _db.Roles.FirstOrDefault(r => r.Name == "Staff")?.Id;
                if (roleId != null)
                    usersQuery = usersQuery.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            }
            else if (targetAudience == "Members")
            {
                var roleId = _db.Roles.FirstOrDefault(r => r.Name == "Member")?.Id;
                if (roleId != null)
                    usersQuery = usersQuery.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            }
            else if (targetAudience == "Specific" && !string.IsNullOrEmpty(specificUserId))
            {
                usersQuery = usersQuery.Where(u => u.Id == specificUserId);
            }

            var userIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(usersQuery.Select(u => u.Id));
            var notifications = userIds.Select(id => new Notification
            {
                UserId = id,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });

            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;

        public AuditService(ApplicationDbContext db) => _db = db;

        public async Task LogAsync(string? userId, string? userEmail, string action,
            string? entityName, string? entityId, string? description,
            string? ipAddress, object? metadata = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null
            });
            await _db.SaveChangesAsync();
        }
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db) => _db = db;

        public async Task<List<MemberReportItem>> GetMemberReportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.MemberProfiles
                .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
                .Where(m => !m.IsDeleted);
            if (from.HasValue) query = query.Where(m => m.JoinDate >= from.Value);
            if (to.HasValue) query = query.Where(m => m.JoinDate <= to.Value);

            var members = await query.ToListAsync();
            return members.Select(m =>
            {
                var active = m.Memberships
                    .Where(ms => ms.OverrideStatus != MembershipStatus.Cancelled)
                    .OrderByDescending(ms => ms.EndDate)
                    .FirstOrDefault();
                return new MemberReportItem
                {
                    MemberId = m.MemberId,
                    MembershipNumber = m.MembershipNumber,
                    FullName = m.FullName,
                    Phone = m.Phone,
                    Email = m.Email,
                    Status = m.Status.ToString(),
                    JoinDate = m.JoinDate,
                    ActivePlan = active?.Plan?.PlanName,
                    MembershipExpiry = active?.EndDate
                };
            }).ToList();
        }

        public async Task<List<PaymentReportItem>> GetPaymentReportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Payments.Include(p => p.Member).AsQueryable();
            if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value);
            if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value.AddDays(1));
            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
            return payments.Select(p => new PaymentReportItem
            {
                PaymentId = p.PaymentId,
                ReceiptNumber = p.ReceiptNumber,
                MemberName = p.Member.FullName,
                MembershipNumber = p.Member.MembershipNumber,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentType = p.PaymentType.ToString(),
                PaymentMethod = p.PaymentMethod.ToString()
            }).ToList();
        }

        public async Task<List<AttendanceReportItem>> GetAttendanceReportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Attendances
                .Include(a => a.Member)
                .Include(a => a.Batch)
                .AsQueryable();
            if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(a => a.Date <= to.Value.Date);
            var records = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.CheckInTime).ToListAsync();
            return records.Select(a => new AttendanceReportItem
            {
                AttendanceId = a.AttendanceId,
                MemberName = a.Member.FullName,
                MembershipNumber = a.Member.MembershipNumber,
                Date = a.Date,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                DurationMinutes = a.DurationMinutes,
                BatchName = a.Batch?.BatchName,
                Status = a.Status.ToString()
            }).ToList();
        }

        public async Task<List<ExpiringMembershipItem>> GetExpiringMembershipsReportAsync(int days)
        {
            var today = DateTime.UtcNow.Date;
            var threshold = today.AddDays(days);
            var items = await _db.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .Where(m => m.EndDate.Date >= today && m.EndDate.Date <= threshold &&
                            m.OverrideStatus != MembershipStatus.Cancelled &&
                            m.OverrideStatus != MembershipStatus.Suspended)
                .OrderBy(m => m.EndDate)
                .ToListAsync();
            return items.Select(m => new ExpiringMembershipItem
            {
                MemberId = m.MemberId,
                MemberName = m.Member.FullName,
                Phone = m.Member.Phone,
                PlanName = m.Plan.PlanName,
                ExpiryDate = m.EndDate,
                DaysRemaining = (m.EndDate.Date - today).Days
            }).ToList();
        }

        public async Task<List<FineReportItem>> GetFineReportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Fines.Include(f => f.Member).AsQueryable();
            if (from.HasValue) query = query.Where(f => f.DateIssued >= from.Value);
            if (to.HasValue) query = query.Where(f => f.DateIssued <= to.Value.AddDays(1));
            var fines = await query.OrderByDescending(f => f.DateIssued).ToListAsync();
            return fines.Select(f => new FineReportItem
            {
                FineId = f.FineId,
                MemberName = f.Member.FullName,
                MembershipNumber = f.Member.MembershipNumber,
                Reason = f.Reason,
                Amount = f.Amount,
                DateIssued = f.DateIssued,
                Status = f.Status.ToString()
            }).ToList();
        }
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IMembershipService _membershipService;
        private readonly ISystemSettingService _settings;

        public DashboardService(ApplicationDbContext db, IPaymentService paymentService,
            IMembershipService membershipService, ISystemSettingService settings)
        {
            _db = db;
            _paymentService = paymentService;
            _membershipService = membershipService;
            _settings = settings;
        }

        public async Task<AdminDashboardVm> GetAdminDashboardAsync()
        {
            var thresholdDays = await _settings.GetAsync<int>("ExpiryThresholdDays");
            if (thresholdDays == 0) thresholdDays = 7;

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var allMemberships = await _db.Memberships
                .Include(m => m.Plan)
                .Include(m => m.Member)
                .Where(m => m.OverrideStatus != MembershipStatus.Cancelled)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var active = allMemberships.Where(m => _membershipService.ComputeStatus(m) == MembershipStatus.Active).ToList();
            var expiring = allMemberships.Where(m => _membershipService.ComputeStatus(m) == MembershipStatus.ExpiringSoon).ToList();
            var expired = allMemberships.Where(m => _membershipService.ComputeStatus(m) == MembershipStatus.Expired).ToList();

            var revenue = await _paymentService.GetMonthlyRevenueAsync(12);
            var todayRevenue = await _paymentService.GetTodayCollectionAsync();
            var monthRevenue = await _db.Payments.Where(p => p.PaymentDate >= monthStart).SumAsync(p => p.Amount);
            var yearRevenue = await _db.Payments.Where(p => p.PaymentDate >= yearStart).SumAsync(p => p.Amount);
            var todayAttendance = await _db.Attendances.CountAsync(a => a.Date == today);
            var pendingFines = await _db.Fines.CountAsync(f => f.Status == FineStatus.Pending);
            var activeBatches = await _db.Batches.CountAsync(b => b.IsActive && !b.IsDeleted);
            var totalTrainers = await _db.Trainers.CountAsync(t => t.Status == TrainerStatus.Active && !t.IsDeleted);

            var planDistribution = await _db.Memberships
                .Include(m => m.Plan)
                .Where(m => m.EndDate >= today && m.OverrideStatus != MembershipStatus.Cancelled)
                .GroupBy(m => m.Plan.PlanName)
                .Select(g => new PlanDistributionItem { PlanName = g.Key, Count = g.Count() })
                .ToListAsync();

            var batches = await _db.Batches
                .Include(b => b.Enrollments)
                .Where(b => b.IsActive && !b.IsDeleted)
                .ToListAsync();
            var batchOccupancy = batches.Select(b => new BatchOccupancyItem
            {
                BatchName = b.BatchName,
                Enrolled = b.Enrollments.Count(e => e.Status == EnrollmentStatus.Active),
                Capacity = b.MaximumCapacity
            }).ToList();

            var last14Days = Enumerable.Range(0, 14)
                .Select(i => today.AddDays(-13 + i))
                .ToList();
            var attendanceCounts = await _db.Attendances
                .Where(a => a.Date >= last14Days.First() && a.Date <= today)
                .GroupBy(a => a.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();
            var attendanceChart = last14Days.Select(d => new DailyAttendanceItem
            {
                Date = d.ToString("MMM dd"),
                Count = attendanceCounts.FirstOrDefault(ac => ac.Date == d)?.Count ?? 0
            }).ToList();

            return new AdminDashboardVm
            {
                TotalMembers = await _db.MemberProfiles.CountAsync(m => !m.IsDeleted),
                ActiveMembers = active.Count + expiring.Count,
                ExpiringThisWeek = expiring.Count,
                ExpiredMembers = expired.Count,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                YearRevenue = yearRevenue,
                TodayCheckIns = todayAttendance,
                ActiveBatches = activeBatches,
                TotalTrainers = totalTrainers,
                PendingFines = pendingFines,
                RevenueChart = revenue,
                PlanDistribution = planDistribution,
                BatchOccupancy = batchOccupancy,
                AttendanceChart = attendanceChart,
                RecentMemberships = allMemberships.Take(10).ToList()
            };
        }

        public async Task<StaffDashboardVm> GetStaffDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var expiringMemberships = await _membershipService.GetExpiringMembershipsAsync(7);
            var todayAttendance = await _db.Attendances
                .Include(a => a.Member).Include(a => a.Batch)
                .Where(a => a.Date == today).OrderByDescending(a => a.CheckInTime).Take(20).ToListAsync();
            var recentPayments = await _db.Payments
                .Include(p => p.Member)
                .Where(p => p.PaymentDate.Date == today)
                .OrderByDescending(p => p.CreatedAt).Take(10).ToListAsync();

            return new StaffDashboardVm
            {
                TodayCheckIns = todayAttendance.Count,
                TodayCollection = await _paymentService.GetTodayCollectionAsync(),
                ExpiringThisWeek = expiringMemberships.Count,
                ExpiringMemberships = expiringMemberships,
                TodayAttendance = todayAttendance,
                RecentPayments = recentPayments
            };
        }

        public async Task<MemberDashboardVm> GetMemberDashboardAsync(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            var activeMembership = await _membershipService.GetActiveMembershipAsync(memberId);
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var totalAttendance = await _db.Attendances.CountAsync(a => a.MemberId == memberId);
            var monthAttendance = await _db.Attendances.CountAsync(a => a.MemberId == memberId && a.Date >= monthStart);
            var payments = await _db.Payments.Where(p => p.MemberId == memberId)
                .OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();
            var fines = await _db.Fines.Where(f => f.MemberId == memberId && f.Status == FineStatus.Pending).ToListAsync();
            var batchEnrollment = await _db.BatchEnrollments
                .Include(be => be.Batch).ThenInclude(b => b.Trainer)
                .Where(be => be.MemberId == memberId && be.Status == EnrollmentStatus.Active)
                .FirstOrDefaultAsync();

            return new MemberDashboardVm
            {
                Member = member!,
                ActiveMembership = activeMembership,
                TotalAttendance = totalAttendance,
                MonthAttendance = monthAttendance,
                RecentPayments = payments,
                PendingFines = fines,
                CurrentBatch = batchEnrollment
            };
        }
    }

    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private const string CacheKey = "SystemSettings";

        public SystemSettingService(ApplicationDbContext db, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private async Task<Dictionary<string, string>> LoadAsync()
        {
            if (_cache.TryGetValue(CacheKey, out object? cachedObj) && cachedObj is Dictionary<string, string> cached)
                return cached;
            var settings = await _db.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
            _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(30));
            return settings;
        }

        public async Task<string?> GetAsync(string key)
        {
            var all = await LoadAsync();
            return all.TryGetValue(key, out var val) ? val : null;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var val = await GetAsync(key);
            if (val == null) return default;
            try { return (T)Convert.ChangeType(val, typeof(T)); }
            catch { return default; }
        }

        public async Task SetAsync(string key, string value)
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null) { setting.Value = value; setting.UpdatedAt = DateTime.UtcNow; }
            else _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            InvalidateCache();
        }

        public async Task<Dictionary<string, string>> GetAllAsync()
            => await _db.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        public void InvalidateCache() => _cache.Remove(CacheKey);
    }
}



