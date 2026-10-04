using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Gym_memrship_Managment.Models;

namespace Gym_memrship_Managment.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MemberProfile> MemberProfiles { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<BatchSchedule> BatchSchedules { get; set; }
        public DbSet<BatchEnrollment> BatchEnrollments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<TrainerSlot> TrainerSlots { get; set; }
        public DbSet<PTBooking> PTBookings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique indexes
            builder.Entity<MemberProfile>()
                .HasIndex(m => m.MembershipNumber).IsUnique();

            builder.Entity<MemberProfile>()
                .HasIndex(m => m.Email).IsUnique();

            builder.Entity<Payment>()
                .HasIndex(p => p.ReceiptNumber).IsUnique();

            builder.Entity<SystemSetting>()
                .HasIndex(s => s.Key).IsUnique();

            // Prevent duplicate active enrollments
            builder.Entity<BatchEnrollment>()
                .HasIndex(be => new { be.MemberId, be.BatchId, be.Status });

            // Cascade delete behavior
            builder.Entity<MemberProfile>()
                .HasOne(m => m.User)
                .WithOne(u => u.MemberProfile)
                .HasForeignKey<MemberProfile>(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<StaffProfile>()
                .HasOne(s => s.User)
                .WithOne(u => u.StaffProfile)
                .HasForeignKey<StaffProfile>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Membership>()
                .HasOne(m => m.Member)
                .WithMany(mp => mp.Memberships)
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Membership>()
                .HasOne(m => m.Plan)
                .WithMany(p => p.Memberships)
                .HasForeignKey(m => m.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Fine>()
                .HasOne(f => f.Member)
                .WithMany(m => m.Fines)
                .HasForeignKey(f => f.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Fine>()
                .HasOne(f => f.Membership)
                .WithMany(m => m.Fines)
                .HasForeignKey(f => f.MembershipId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Membership)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MembershipId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany(m => m.Attendances)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
                .HasOne(a => a.Batch)
                .WithMany(b => b.Attendances)
                .HasForeignKey(a => a.BatchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Prevent multiple check-ins on the same day
            builder.Entity<Attendance>()
                .HasIndex(a => new { a.MemberId, a.Date })
                .IsUnique();

            builder.Entity<BatchEnrollment>()
                .HasOne(be => be.Member)
                .WithMany(m => m.BatchEnrollments)
                .HasForeignKey(be => be.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BatchEnrollment>()
                .HasOne(be => be.Batch)
                .WithMany(b => b.Enrollments)
                .HasForeignKey(be => be.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Batch>()
                .HasOne(b => b.Trainer)
                .WithMany(t => t.Batches)
                .HasForeignKey(b => b.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<BatchSchedule>()
                .HasOne(bs => bs.Batch)
                .WithMany(b => b.Schedules)
                .HasForeignKey(bs => bs.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}


