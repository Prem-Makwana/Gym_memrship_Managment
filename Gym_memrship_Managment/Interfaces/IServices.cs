using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.ViewModels;

namespace Gym_memrship_Managment.Interfaces
{
    public interface IMembershipService
    {
        MembershipStatus ComputeStatus(Membership membership, int expiryThresholdDays = 7);
        int GetDaysRemaining(Membership membership);
        Task<Membership?> GetActiveMembershipAsync(int memberId);
        Task<List<Membership>> GetExpiringMembershipsAsync(int thresholdDays);
        Task SuspendAsync(int membershipId, string reason);
        Task CancelAsync(int membershipId, string reason);
        Task ReactivateAsync(int membershipId);
    }

    public interface IEnrollmentService
    {
        Task<(bool Success, string Message, int? MembershipId)> EnrollAsync(
            int memberId, int planId, DateTime startDate, decimal discount, string receivedBy);
    }

    public interface IRenewalService
    {
        Task<(bool Success, string Message, int? MembershipId)> RenewAsync(
            int existingMembershipId, int? newPlanId, decimal discount, string receivedBy);
        Task<decimal> CalculateLateFeeAsync(int membershipId);
    }

    public interface IAttendanceService
    {
        Task<(bool Success, string Message, Attendance? Attendance)> CheckInAsync(int memberId, int? batchId, bool overrideChecks = false);
        Task<(bool Success, string Message)> CheckOutAsync(int attendanceId);
        Task<List<Attendance>> GetTodayAttendanceAsync(int? batchId = null);
        Task<Attendance?> GetActiveCheckInAsync(int memberId);
    }

    public interface IPaymentService
    {
        Task<Payment> RecordPaymentAsync(int memberId, int? membershipId, decimal amount,
            PaymentMethod method, PaymentType type, string? reference, string? notes, string? receivedBy);
        Task<string> GenerateReceiptNumberAsync(int offset = 0);
        Task<List<Payment>> GetMemberLedgerAsync(int memberId);
        Task<decimal> GetTodayCollectionAsync();
        Task<List<MonthlyRevenue>> GetMonthlyRevenueAsync(int months = 12);
    }

    public interface IFineService
    {
        Task<Fine> CreateFineAsync(int memberId, int? membershipId, string reason, decimal amount);
        Task<bool> PayFineAsync(int fineId, string receivedBy);
        Task<bool> WaiveFineAsync(int fineId, string notes);
        Task<decimal> GetPendingFinesAmountAsync(int memberId);
        Task AutoGenerateLateFinesAsync(int membershipId);
    }

    public interface IBatchService
    {
        Task<(bool Success, string Message)> EnrollMemberAsync(int memberId, int batchId);
        Task<(bool Success, string Message)> TransferMemberAsync(int memberId, int fromBatchId, int toBatchId);
        Task<(bool Success, string Message)> DropMemberAsync(int memberId, int batchId);
        Task<int> GetCurrentOccupancyAsync(int batchId);
        Task<List<BatchEnrollment>> GetBatchMembersAsync(int batchId);
    }

    public interface INotificationService
    {
        Task SendAsync(string userId, string title, string message, NotificationType type);
        Task<List<Notification>> GetUnreadAsync(string userId);
        Task MarkReadAsync(int notificationId);
        Task MarkAllReadAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task BroadcastAsync(string title, string message, NotificationType type, string targetAudience, string? specificUserId = null);
    }

    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userEmail, string action,
            string? entityName, string? entityId, string? description,
            string? ipAddress, object? metadata = null);
    }

    public interface IReportService
    {
        Task<List<MemberReportItem>> GetMemberReportAsync(DateTime? from, DateTime? to);
        Task<List<PaymentReportItem>> GetPaymentReportAsync(DateTime? from, DateTime? to);
        Task<List<AttendanceReportItem>> GetAttendanceReportAsync(DateTime? from, DateTime? to);
        Task<List<ExpiringMembershipItem>> GetExpiringMembershipsReportAsync(int days);
        Task<List<FineReportItem>> GetFineReportAsync(DateTime? from, DateTime? to);
    }

    public interface IStripePaymentService
    {
        Task<Stripe.Checkout.Session> CreateCheckoutSessionAsync(Models.Membership membership, string successUrl, string cancelUrl);
    }

    public interface IDashboardService
    {
        Task<AdminDashboardVm> GetAdminDashboardAsync();
        Task<StaffDashboardVm> GetStaffDashboardAsync();
        Task<MemberDashboardVm> GetMemberDashboardAsync(int memberId);
    }

    public interface ISystemSettingService
    {
        Task<string?> GetAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task SetAsync(string key, string value);
        Task<Dictionary<string, string>> GetAllAsync();
        void InvalidateCache();
    }

    // DTOs for reports
    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
        public decimal Revenue { get; set; }
    }

    public class MemberReportItem
    {
        public int MemberId { get; set; }
        public string MembershipNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime JoinDate { get; set; }
        public string? ActivePlan { get; set; }
        public DateTime? MembershipExpiry { get; set; }
    }

    public class PaymentReportItem
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string MembershipNumber { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
    }

    public class AttendanceReportItem
    {
        public int AttendanceId { get; set; }
        public string MemberName { get; set; } = "";
        public string MembershipNumber { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string? BatchName { get; set; }
        public string Status { get; set; } = "";
    }

    public class ExpiringMembershipItem
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string PlanName { get; set; } = "";
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class FineReportItem
    {
        public int FineId { get; set; }
        public string MemberName { get; set; } = "";
        public string MembershipNumber { get; set; } = "";
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime DateIssued { get; set; }
        public string Status { get; set; } = "";
    }
}



