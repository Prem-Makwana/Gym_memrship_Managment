using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MembersController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.MemberProfiles.Where(m => !m.IsDeleted).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.FullName.Contains(search) || m.MembershipNumber.Contains(search) || m.Phone.Contains(search));

            var total = await query.CountAsync();
            var members = await query.OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new { m.MemberId, m.MembershipNumber, m.FullName, m.Phone, m.Email, Status = m.Status.ToString(), m.JoinDate })
                .ToListAsync();

            return Ok(new { total, page, pageSize, data = members });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var m = await _db.MemberProfiles.Include(mp => mp.Memberships).ThenInclude(ms => ms.Plan)
                .FirstOrDefaultAsync(mp => mp.MemberId == id && !mp.IsDeleted);
            if (m == null) return NotFound();
            return Ok(m);
        }

        [HttpDelete("{id}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.MemberProfiles.FindAsync(id);
            if (m == null) return NotFound();
            m.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlansController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public PlansController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var plans = await _db.MembershipPlans.Where(p => !p.IsDeleted && p.IsActive).ToListAsync();
            return Ok(plans);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _db.MembershipPlans.FindAsync(id);
            if (p == null || p.IsDeleted) return NotFound();
            return Ok(p);
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBatchService _batchService;
        public BatchesController(ApplicationDbContext db, IBatchService batchService) { _db = db; _batchService = batchService; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var batches = await _db.Batches
                .Include(b => b.Trainer)
                .Include(b => b.Enrollments)
                .Where(b => b.IsActive && !b.IsDeleted)
                .Select(b => new
                {
                    b.BatchId, b.BatchName, b.StartTime, b.EndTime, b.MaximumCapacity, b.Location,
                    TrainerName = b.Trainer != null ? b.Trainer.FullName : null,
                    Enrolled = b.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)
                })
                .ToListAsync();
            return Ok(batches);
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var members = await _batchService.GetBatchMembersAsync(id);
            return Ok(members.Select(be => new { be.MemberId, be.Member.FullName, be.Member.MembershipNumber, be.EnrolledDate }));
        }

        [HttpGet("{id}/occupancy")]
        public async Task<IActionResult> GetOccupancy(int id)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null) return NotFound();
            var occupied = await _batchService.GetCurrentOccupancyAsync(id);
            return Ok(new { batchId = id, occupied, capacity = batch.MaximumCapacity, percent = batch.MaximumCapacity > 0 ? occupied * 100.0 / batch.MaximumCapacity : 0 });
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAttendanceService _attendance;
        public AttendanceController(ApplicationDbContext db, IAttendanceService attendance) { _db = db; _attendance = attendance; }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? date, [FromQuery] int? batchId)
        {
            var query = _db.Attendances.Include(a => a.Member).Include(a => a.Batch).AsQueryable();
            if (date.HasValue) query = query.Where(a => a.Date == date.Value.Date);
            if (batchId.HasValue) query = query.Where(a => a.BatchId == batchId);
            var records = await query.OrderByDescending(a => a.CheckInTime).Take(100).ToListAsync();
            return Ok(records.Select(a => new { a.AttendanceId, MemberName = a.Member.FullName, a.Member.MembershipNumber, a.Date, a.CheckInTime, a.CheckOutTime, a.DurationMinutes, BatchName = a.Batch?.BatchName, Status = a.Status.ToString() }));
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest req)
        {
            var (success, message, att) = await _attendance.CheckInAsync(req.MemberId, req.BatchId, req.Override);
            if (!success) return BadRequest(new { message });
            return Ok(new { attendanceId = att?.AttendanceId, message });
        }

        [HttpGet("batch/{batchId}/date/{date}")]
        public async Task<IActionResult> GetByBatchDate(int batchId, DateTime date)
        {
            var records = await _db.Attendances.Include(a => a.Member)
                .Where(a => a.BatchId == batchId && a.Date == date.Date)
                .ToListAsync();
            return Ok(records);
        }
    }

    public class CheckInRequest
    {
        public int MemberId { get; set; }
        public int? BatchId { get; set; }
        public bool Override { get; set; }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        public PaymentsController(ApplicationDbContext db, IPaymentService paymentService) { _db = db; _paymentService = paymentService; }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _db.Payments.Include(p => p.Member).AsQueryable();
            if (from.HasValue) query = query.Where(p => p.PaymentDate >= from);
            if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value.AddDays(1));
            var payments = await query.OrderByDescending(p => p.PaymentDate).Take(100).ToListAsync();
            return Ok(payments.Select(p => new { p.PaymentId, p.ReceiptNumber, MemberName = p.Member.FullName, p.Amount, p.PaymentDate, Type = p.PaymentType.ToString(), Method = p.PaymentMethod.ToString() }));
        }

        [HttpGet("member/{id}/history")]
        public async Task<IActionResult> GetMemberHistory(int id)
        {
            var payments = await _paymentService.GetMemberLedgerAsync(id);
            return Ok(payments);
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly IReportService _reports;
        public ReportsController(IPaymentService payments, IReportService reports) { _payments = payments; _reports = reports; }

        [HttpGet("revenue-monthly")]
        public async Task<IActionResult> RevenueMonthly([FromQuery] int months = 12)
        {
            var data = await _payments.GetMonthlyRevenueAsync(months);
            return Ok(data);
        }

        [HttpGet("expiring-memberships")]
        public async Task<IActionResult> ExpiringMemberships([FromQuery] int days = 7)
        {
            var data = await _reports.GetExpiringMembershipsAsync(days);
            return Ok(data);
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboard;
        public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var vm = await _dashboard.GetAdminDashboardAsync();
            return Ok(new
            {
                vm.TotalMembers, vm.ActiveMembers, vm.ExpiringThisWeek,
                vm.TodayRevenue, vm.TodayCheckIns, vm.ActiveBatches,
                vm.PendingFines
            });
        }
    }

    [Area("Api")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;
        public NotificationsController(INotificationService notifications) => _notifications = notifications;

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetForUser(string userId)
        {
            var items = await _notifications.GetUnreadAsync(userId);
            return Ok(items);
        }
    }
}
