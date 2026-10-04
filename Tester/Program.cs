using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Services;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.Interfaces;

namespace Tester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GymTestDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<IFineService, FineService>();

            var provider = services.BuildServiceProvider();
            var db = provider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            var paymentService = provider.GetRequiredService<IPaymentService>();
            var auditService = provider.GetRequiredService<IAuditService>();
            var enrollmentService = provider.GetRequiredService<IEnrollmentService>();
            var attendanceService = provider.GetRequiredService<IAttendanceService>();

            var member = new MemberProfile { FullName = "John Doe", Phone = "123", Status = MemberStatus.Active };
            db.MemberProfiles.Add(member);
            var plan = new MembershipPlan { PlanName = "Basic", Price = 100, DurationInDays = 30, IsActive = true };
            db.MembershipPlans.Add(plan);
            await db.SaveChangesAsync();

            Console.WriteLine("Enrolling...");
            await enrollmentService.EnrollAsync(member.MemberId, plan.PlanId, DateTime.UtcNow, 0, "System");

            Console.WriteLine("Testing Concurrent Check-In...");
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = provider.CreateScope();
                    var scopedAttendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
                    var result = await scopedAttendanceService.CheckInAsync(member.MemberId, null, false);
                    Console.WriteLine($"CheckIn Result: {result.Success} - {result.Message}");
                }));
            }
            
            await Task.WhenAll(tasks);
            
            var attendances = await db.Attendances.ToListAsync();
            Console.WriteLine($"Total Attendances for John: {attendances.Count}");
        }
    }

    public class MembershipService : IMembershipService
    {
        private readonly ApplicationDbContext _db;
        public MembershipService(ApplicationDbContext db) => _db = db;
        public async Task<Membership?> GetActiveMembershipAsync(int memberId) 
        {
            return await _db.Memberships.FirstOrDefaultAsync(m => m.MemberId == memberId);
        }
        public Task<bool> CancelMembershipAsync(int membershipId, string reason) => throw new NotImplementedException();
        public Task<bool> FreezeMembershipAsync(int membershipId, int days, string reason) => throw new NotImplementedException();
        public Task<bool> UnfreezeMembershipAsync(int membershipId) => throw new NotImplementedException();
        public Task<Membership> GetMembershipDetailsAsync(int membershipId) => throw new NotImplementedException();
        public Task UpdateMembershipStatusAsync() => throw new NotImplementedException();

        public MembershipStatus ComputeStatus(Membership membership, int days) => throw new NotImplementedException();
        public int GetDaysRemaining(Membership membership) => throw new NotImplementedException();
        public Task<List<Membership>> GetExpiringMembershipsAsync(int days) => throw new NotImplementedException();
        public Task SuspendAsync(int id, string reason) => throw new NotImplementedException();
        public Task CancelAsync(int id, string reason) => throw new NotImplementedException();
        public Task ReactivateAsync(int id) => throw new NotImplementedException();
    }

    public class AuditService : IAuditService
    {
        public Task LogAsync(string? userId, string? userEmail, string action, string? entityName, string? entityId, string? description, string? ipAddress, object? metadata = null)
        {
            return Task.CompletedTask;
        }
    }
}
