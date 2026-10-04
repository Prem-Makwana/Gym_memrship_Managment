using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Gym_memrship_Managment.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReportController : Controller
    {
        private readonly IReportService _reports;
        private readonly ApplicationDbContext _db;

        public ReportController(IReportService reports, ApplicationDbContext db)
        {
            _reports = reports;
            _db = db;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> Members(DateTime? from, DateTime? to, bool export = false)
        {
            var data = await _reports.GetMemberReportAsync(from, to);
            if (export) return ExportCsv(data, "members_report.csv");
            ViewBag.From = from;
            ViewBag.To = to;
            return View("MembersReport", data);
        }

        [HttpGet]
        public async Task<IActionResult> Payments(DateTime? from, DateTime? to, bool export = false)
        {
            var data = await _reports.GetPaymentReportAsync(from, to);
            if (export) return ExportCsv(data, "payments_report.csv");
            ViewBag.From = from;
            ViewBag.To = to;
            return View("PaymentsReport", data);
        }

        [HttpGet]
        public async Task<IActionResult> Attendance(DateTime? from, DateTime? to, bool export = false)
        {
            var data = await _reports.GetAttendanceReportAsync(from, to);
            if (export) return ExportCsv(data, "attendance_report.csv");
            ViewBag.From = from;
            ViewBag.To = to;
            return View("AttendanceReport", data);
        }

        [HttpGet]
        public async Task<IActionResult> Expiring(int days = 30, bool export = false)
        {
            var data = await _reports.GetExpiringMembershipsAsync(days);
            if (export) return ExportCsv(data, "expiring_memberships.csv");
            ViewBag.Days = days;
            return View("ExpiringReport", data);
        }

        [HttpGet]
        public async Task<IActionResult> Fines(DateTime? from, DateTime? to, bool export = false)
        {
            var data = await _reports.GetFineReportAsync(from, to);
            if (export) return ExportCsv(data, "fines_report.csv");
            ViewBag.From = from;
            ViewBag.To = to;
            return View("FinesReport", data);
        }

        private FileContentResult ExportCsv<T>(IEnumerable<T> data, string filename)
        {
            var props = typeof(T).GetProperties();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
            foreach (var item in data)
                sb.AppendLine(string.Join(",", props.Select(p => $"\"{p.GetValue(item)}\"")));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", filename);
        }
    }

    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuditLogController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? action, DateTime? from, DateTime? to, int page = 1)
        {
            const int pageSize = 25;
            var query = _db.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value.AddDays(1));

            var total = await query.CountAsync();
            var logs = await query.OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.ActionFilter = action;
            ViewBag.From = from;
            ViewBag.To = to;

            return View(logs);
        }
    }

    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ISystemSettingService _settings;

        public SettingsController(ISystemSettingService settings) => _settings = settings;

        public async Task<IActionResult> Index()
        {
            var all = await _settings.GetAllAsync();
            return View(all);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Dictionary<string, string> settings)
        {
            foreach (var kv in settings)
                await _settings.SetAsync(kv.Key, kv.Value);
            TempData["Success"] = "Settings updated.";
            return RedirectToAction("Index");
        }
    }
}
