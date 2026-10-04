using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Services
{
    public class ExpiryNotificationService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ExpiryNotificationService> _logger;

        public ExpiryNotificationService(IServiceProvider services, ILogger<ExpiryNotificationService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var nextRun = now.Date.AddDays(1).AddHours(8); // Run daily at 8am UTC
                    var delay = nextRun - now;
                    if (delay.TotalMilliseconds <= 0) delay = TimeSpan.FromHours(24);

                    await Task.Delay(delay, stoppingToken);
                    await RunAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExpiryNotificationService");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationSvc = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.UtcNow.Date;
            var windows = new[] { 7, 3, 1 };

            foreach (var days in windows)
            {
                var target = today.AddDays(days);
                var memberships = await db.Memberships
                    .Include(m => m.Member).ThenInclude(m => m.User)
                    .Include(m => m.Plan)
                    .Where(m => m.EndDate.Date == target &&
                                m.OverrideStatus != MembershipStatus.Cancelled &&
                                m.OverrideStatus != MembershipStatus.Suspended &&
                                m.Member.User != null)
                    .ToListAsync(ct);

                foreach (var membership in memberships)
                {
                    if (membership.Member.User?.Id is { } userId)
                    {
                        await notificationSvc.SendAsync(userId,
                            "Membership Expiring Soon",
                            $"Your {membership.Plan.PlanName} membership expires in {days} day(s) on {membership.EndDate:MMM dd, yyyy}. Please renew to continue access.",
                            NotificationType.Warning);
                    }
                }
            }

            _logger.LogInformation("Expiry notifications sent at {Time}", DateTime.UtcNow);
        }
    }
}
