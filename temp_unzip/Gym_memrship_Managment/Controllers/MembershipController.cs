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
    public class MembershipPlanController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public MembershipPlanController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var plans = await _db.MembershipPlans.Where(p => !p.IsDeleted).OrderBy(p => p.Price).ToListAsync();
            return View(plans);
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new MembershipPlan());

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(MembershipPlan plan)
        {
            if (!ModelState.IsValid) return View(plan);
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;
            _db.MembershipPlans.Add(plan);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "CreatePlan",
                "MembershipPlan", plan.PlanId.ToString(), $"Created plan {plan.PlanName}", null);
            TempData["Success"] = "Plan created.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await _db.MembershipPlans.FindAsync(id);
            if (plan == null || plan.IsDeleted) return NotFound();
            return View(plan);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(MembershipPlan model)
        {
            if (!ModelState.IsValid) return View(model);
            var plan = await _db.MembershipPlans.FindAsync(model.PlanId);
            if (plan == null || plan.IsDeleted) return NotFound();

            plan.PlanName = model.PlanName;
            plan.Description = model.Description;
            plan.DurationInDays = model.DurationInDays;
            plan.Price = model.Price;
            plan.RegistrationFee = model.RegistrationFee;
            plan.RenewalFee = model.RenewalFee;
            plan.LateFeePerDay = model.LateFeePerDay;
            plan.GracePeriodDays = model.GracePeriodDays;
            plan.IsActive = model.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var plan = await _db.MembershipPlans.FindAsync(id);
            if (plan == null) return NotFound();
            plan.IsDeleted = true;
            plan.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan deleted.";
            return RedirectToAction("Index");
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEnrollmentService _enrollment;
        private readonly IRenewalService _renewal;
        private readonly IMembershipService _membershipService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembershipController(ApplicationDbContext db, IEnrollmentService enrollment,
            IRenewalService renewal, IMembershipService membershipService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _enrollment = enrollment;
            _renewal = renewal;
            _membershipService = membershipService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            const int pageSize = 15;
            var memberships = await _db.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<MembershipStatus>(status, out var s))
                    memberships = memberships.Where(m => _membershipService.ComputeStatus(m) == s).ToList();
            }

            var total = memberships.Count;
            var paged = memberships.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.StatusFilter = status;
            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Enroll(int memberId)
        {
            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null || member.IsDeleted) return NotFound();

            return View(new EnrollVm
            {
                MemberId = memberId,
                Member = member,
                Plans = await _db.MembershipPlans.Where(p => p.IsActive && !p.IsDeleted).ToListAsync(),
                StartDate = DateTime.Today
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(EnrollVm model)
        {
            if (!ModelState.IsValid)
            {
                model.Member = await _db.MemberProfiles.FindAsync(model.MemberId);
                model.Plans = await _db.MembershipPlans.Where(p => p.IsActive && !p.IsDeleted).ToListAsync();
                return View(model);
            }

            var receivedBy = User.Identity?.Name ?? "staff";
            var (success, message, membershipId) = await _enrollment.EnrollAsync(
                model.MemberId, model.PlanId, model.StartDate, model.Discount, receivedBy);

            if (!success)
            {
                TempData["Error"] = message;
                model.Member = await _db.MemberProfiles.FindAsync(model.MemberId);
                model.Plans = await _db.MembershipPlans.Where(p => p.IsActive && !p.IsDeleted).ToListAsync();
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Details", "Member", new { id = model.MemberId });
        }

        [HttpGet]
        public async Task<IActionResult> Renew(int id)
        {
            var membership = await _db.Memberships.Include(m => m.Member).Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null) return NotFound();

            var lateFee = await _renewal.CalculateLateFeeAsync(id);
            return View(new RenewVm
            {
                MembershipId = id,
                Membership = membership,
                Plans = await _db.MembershipPlans.Where(p => p.IsActive && !p.IsDeleted).ToListAsync(),
                LateFee = lateFee
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(RenewVm model)
        {
            var receivedBy = User.Identity?.Name ?? "staff";
            var (success, message, newId) = await _renewal.RenewAsync(
                model.MembershipId, model.NewPlanId, model.Discount, receivedBy);

            if (!success) TempData["Error"] = message;
            else TempData["Success"] = message;

            var membership = await _db.Memberships.FindAsync(model.MembershipId);
            return RedirectToAction("Details", "Member", new { id = membership?.MemberId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var membership = await _db.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .Include(m => m.Payments)
                .Include(m => m.Fines)
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null) return NotFound();
            ViewBag.Status = _membershipService.ComputeStatus(membership);
            return View(membership);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Suspend(int id, string reason)
        {
            await _membershipService.SuspendAsync(id, reason);
            TempData["Success"] = "Membership suspended.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reactivate(int id)
        {
            await _membershipService.ReactivateAsync(id);
            TempData["Success"] = "Membership reactivated.";
            return RedirectToAction("Details", new { id });
        }
    }
}
