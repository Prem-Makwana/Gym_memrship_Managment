using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendance;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(IAttendanceService attendance, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _attendance = attendance;
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            return View(new CheckInVm
            {
                Batches = await _db.Batches.Where(b => b.IsActive && !b.IsDeleted).ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckIn(int memberId, int? batchId, bool overrideChecks = false)
        {
            var (success, message, attendance) = await _attendance.CheckInAsync(memberId, batchId, overrideChecks);
            return Json(new { success, message, attendanceId = attendance?.AttendanceId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckOut(int attendanceId)
        {
            var (success, message) = await _attendance.CheckOutAsync(attendanceId);
            return Json(new { success, message });
        }

        [HttpGet]
        public async Task<IActionResult> MemberStatus(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null) return Json(new { found = false });

            var activeCheckIn = await _attendance.GetActiveCheckInAsync(memberId);
            var activeMembership = await _db.Memberships
                .Include(m => m.Plan)
                .Where(m => m.MemberId == memberId && m.EndDate >= DateTime.UtcNow.Date &&
                            m.OverrideStatus != MembershipStatus.Cancelled && m.OverrideStatus != MembershipStatus.Suspended)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            return Json(new
            {
                found = true,
                memberId = member.MemberId,
                fullName = member.FullName,
                membershipNumber = member.MembershipNumber,
                phone = member.Phone,
                status = member.Status.ToString(),
                hasMembership = activeMembership != null,
                planName = activeMembership?.Plan?.PlanName,
                expiryDate = activeMembership?.EndDate.ToString("yyyy-MM-dd"),
                checkedIn = activeCheckIn != null,
                attendanceId = activeCheckIn?.AttendanceId
            });
        }

        [HttpGet]
        public async Task<IActionResult> History(DateTime? from, DateTime? to, int? batchId, int page = 1)
        {
            const int pageSize = 20;
            var query = _db.Attendances.Include(a => a.Member).Include(a => a.Batch).AsQueryable();

            if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(a => a.Date <= to.Value.Date);
            if (batchId.HasValue) query = query.Where(a => a.BatchId == batchId);

            var total = await query.CountAsync();
            var records = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.CheckInTime)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Batches = await _db.Batches.Where(b => b.IsActive && !b.IsDeleted).ToListAsync();
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.BatchId = batchId;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View(records);
        }

        [HttpGet]
        public async Task<IActionResult> GetTodayStats()
        {
            var today = DateTime.UtcNow.Date;
            var total = await _db.Attendances.CountAsync(a => a.Date == today);
            var active = await _db.Attendances.CountAsync(a => a.Date == today && a.CheckOutTime == null);
            return Json(new { total, active });
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchMembers(int batchId)
        {
            var enrollments = await _db.BatchEnrollments
                .Include(be => be.Member)
                .Where(be => be.BatchId == batchId && be.Status == EnrollmentStatus.Active)
                .Select(be => new {
                    memberId = be.Member.MemberId,
                    fullName = be.Member.FullName,
                    membershipNumber = be.Member.MembershipNumber
                })
                .ToListAsync();

            return Json(enrollments);
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly ISystemSettingService _settings;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext db, IPaymentService paymentService,
            ISystemSettingService settings, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _paymentService = paymentService;
            _settings = settings;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int page = 1)
        {
            const int pageSize = 20;
            var query = _db.Payments.Include(p => p.Member).AsQueryable();
            if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value);
            if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value.AddDays(1));

            var total = await query.CountAsync();
            var payments = await query.OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.TodayCollection = await _paymentService.GetTodayCollectionAsync();

            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null) return NotFound();

            var memberships = await _db.Memberships.Include(m => m.Plan)
                .Where(m => m.MemberId == memberId).OrderByDescending(m => m.CreatedAt).ToListAsync();

            return View(new PaymentCreateVm
            {
                MemberId = memberId,
                Member = member,
                Memberships = memberships
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentCreateVm model)
        {
            if (!ModelState.IsValid)
            {
                model.Member = await _db.MemberProfiles.FindAsync(model.MemberId);
                model.Memberships = await _db.Memberships.Include(m => m.Plan)
                    .Where(m => m.MemberId == model.MemberId).ToListAsync();
                return View(model);
            }

            var receivedBy = User.Identity?.Name;
            var payment = await _paymentService.RecordPaymentAsync(
                model.MemberId, model.MembershipId, model.Amount,
                model.PaymentMethod, model.PaymentType,
                model.TransactionReference, model.Notes, receivedBy);

            TempData["Success"] = $"Payment of {model.Amount:C} recorded. Receipt: {payment.ReceiptNumber}";
            return RedirectToAction("Receipt", new { id = payment.PaymentId });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var payment = await _db.Payments
                .Include(p => p.Member)
                .Include(p => p.Membership).ThenInclude(m => m!.Plan)
                .FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return NotFound();

            return View(new ReceiptVm
            {
                Payment = payment,
                GymName = await _settings.GetAsync("GymName") ?? "FitZone Pro Gym",
                GymAddress = await _settings.GetAsync("GymAddress") ?? "",
                GymPhone = await _settings.GetAsync("GymPhone") ?? ""
            });
        }

        [HttpGet]
        public async Task<IActionResult> Ledger(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null) return NotFound();

            var payments = await _paymentService.GetMemberLedgerAsync(memberId);
            ViewBag.Member = member;
            return View(payments);
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    public class FineController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFineService _fineService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FineController(ApplicationDbContext db, IFineService fineService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _fineService = fineService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            const int pageSize = 20;
            var query = _db.Fines.Include(f => f.Member).AsQueryable();
            if (Enum.TryParse<FineStatus>(status, out var s))
                query = query.Where(f => f.Status == s);

            var total = await query.CountAsync();
            var fines = await query.OrderByDescending(f => f.DateIssued)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.StatusFilter = status;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View(fines);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null) return NotFound();
            ViewBag.Member = member;
            return View(new Fine { MemberId = memberId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fine model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Member = await _db.MemberProfiles.FindAsync(model.MemberId);
                return View(model);
            }
            await _fineService.CreateFineAsync(model.MemberId, model.MembershipId, model.Reason, model.Amount);
            TempData["Success"] = "Fine created.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            var receivedBy = User.Identity?.Name ?? "staff";
            await _fineService.PayFineAsync(id, receivedBy);
            TempData["Success"] = "Fine marked as paid.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Waive(int id, string notes)
        {
            await _fineService.WaiveFineAsync(id, notes);
            TempData["Success"] = "Fine waived.";
            return RedirectToAction("Index");
        }
    }

    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var notifications = await _notificationService.GetUnreadAsync(user.Id);
            return View(notifications);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkReadAsync(id);
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _notificationService.MarkAllReadAsync(user.Id);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });
            var count = await _notificationService.GetUnreadCountAsync(user.Id);
            return Json(new { count });
        }
    }
}
