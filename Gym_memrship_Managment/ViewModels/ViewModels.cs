using System.ComponentModel.DataAnnotations;
using Gym_memrship_Managment.Models;

namespace Gym_memrship_Managment.ViewModels
{
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Dashboard VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class AdminDashboardVm
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int ExpiringThisWeek { get; set; }
        public int ExpiredMembers { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public int TodayCheckIns { get; set; }
        public int ActiveBatches { get; set; }
        public int TotalTrainers { get; set; }
        public int PendingFines { get; set; }
        public List<Interfaces.MonthlyRevenue> RevenueChart { get; set; } = new();
        public List<PlanDistributionItem> PlanDistribution { get; set; } = new();
        public List<BatchOccupancyItem> BatchOccupancy { get; set; } = new();
        public List<DailyAttendanceItem> AttendanceChart { get; set; } = new();
        public List<Membership> RecentMemberships { get; set; } = new();
    }

    public class PlanDistributionItem
    {
        public string PlanName { get; set; } = "";
        public int Count { get; set; }
    }

    public class BatchOccupancyItem
    {
        public string BatchName { get; set; } = "";
        public int Enrolled { get; set; }
        public int Capacity { get; set; }
    }

    public class DailyAttendanceItem
    {
        public string Date { get; set; } = "";
        public int Count { get; set; }
    }

    public class StaffDashboardVm
    {
        public int TodayCheckIns { get; set; }
        public decimal TodayCollection { get; set; }
        public int ExpiringThisWeek { get; set; }
        public List<Membership> ExpiringMemberships { get; set; } = new();
        public List<Attendance> TodayAttendance { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
    }

    public class MemberDashboardVm
    {
        public MemberProfile Member { get; set; } = null!;
        public Membership? ActiveMembership { get; set; }
        public int TotalAttendance { get; set; }
        public int MonthAttendance { get; set; }
        public List<Payment> RecentPayments { get; set; } = new();
        public List<Fine> PendingFines { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public BatchEnrollment? CurrentBatch { get; set; }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Member VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class MemberListVm
    {
        public List<MemberProfile> Members { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class MemberCreateVm
    {
        public string FullName { get; set; } = "";
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public bool CreateUserAccount { get; set; } = true;
        public string? Password { get; set; }
    }

    public class MemberEditVm
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = "";
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public MemberStatus Status { get; set; }
    }

    public class MemberDetailsVm
    {
        public MemberProfile Member { get; set; } = null!;
        public Membership? ActiveMembership { get; set; }
        public List<Membership> MembershipHistory { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
        public List<Fine> Fines { get; set; } = new();
        public List<Attendance> RecentAttendance { get; set; } = new();
        public BatchEnrollment? CurrentBatch { get; set; }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Membership VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class EnrollVm
    {
        public int MemberId { get; set; }
        public MemberProfile? Member { get; set; }
        public int PlanId { get; set; }
        public List<MembershipPlan> Plans { get; set; } = new();
        public DateTime StartDate { get; set; } = DateTime.Today;
        public decimal Discount { get; set; }
        public string? Notes { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class RenewVm
    {
        public int MembershipId { get; set; }
        public Membership? Membership { get; set; }
        public int? NewPlanId { get; set; }
        public List<MembershipPlan> Plans { get; set; } = new();
        public decimal Discount { get; set; }
        public decimal LateFee { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Batch VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class BatchCreateVm
    {
        public string BatchName { get; set; } = "";
        public string? Description { get; set; }
        public int? TrainerId { get; set; }
        public List<Trainer> Trainers { get; set; } = new();
        public TimeOnly StartTime { get; set; } = new TimeOnly(6, 0);
        public TimeOnly EndTime { get; set; } = new TimeOnly(7, 0);
        public int MaximumCapacity { get; set; } = 20;
        public string? Location { get; set; }
        public List<DayOfWeek> SelectedDays { get; set; } = new();
    }

    public class BatchMembersVm
    {
        public Batch Batch { get; set; } = null!;
        public List<BatchEnrollment> Enrollments { get; set; } = new();
        public int Occupied { get; set; }
        public int Capacity { get; set; }
        public double OccupancyPercent => Capacity > 0 ? (Occupied * 100.0 / Capacity) : 0;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Attendance VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class CheckInVm
    {
        public string? SearchTerm { get; set; }
        public MemberProfile? FoundMember { get; set; }
        public Attendance? ActiveCheckIn { get; set; }
        public List<Batch> Batches { get; set; } = new();
        public int? SelectedBatchId { get; set; }
        public bool OverrideChecks { get; set; }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Payment VMs â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class PaymentCreateVm
    {
        public int MemberId { get; set; }
        public MemberProfile? Member { get; set; }
        public int? MembershipId { get; set; }
        public List<Membership> Memberships { get; set; } = new();
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentType PaymentType { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
    }

    public class ReceiptVm
    {
        public Payment Payment { get; set; } = null!;
        public string GymName { get; set; } = "";
        public string GymAddress { get; set; } = "";
        public string GymPhone { get; set; } = "";
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Login VM â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class LoginVm
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = "";
        
        public bool RememberMe { get; set; }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€ Trainer VM â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class TrainerCreateVm
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string? Specialization { get; set; }
        public int ExperienceYears { get; set; }
        public DateTime JoiningDate { get; set; } = DateTime.Today;
    }
    public class NotificationCreateVm 
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; } = NotificationType.Info;
        
        [Required]
        public string TargetAudience { get; set; } = "All"; 
        
        public string? SpecificUserId { get; set; }
    }
}
