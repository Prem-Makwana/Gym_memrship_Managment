using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Gym_memrship_Managment.Controllers
{
    [Authorize(Roles = "Member")]
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripePaymentService _stripe;
        private readonly IEnrollmentService _enrollment;
        private readonly IRenewalService _renewal;
        private readonly IPaymentService _paymentService;

        public ShopController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            StripePaymentService stripe,
            IEnrollmentService enrollment,
            IRenewalService renewal,
            IPaymentService paymentService)
        {
            _db = db;
            _userManager = userManager;
            _stripe = stripe;
            _enrollment = enrollment;
            _renewal = renewal;
            _paymentService = paymentService;
        }

        // GET: /Shop/Plans
        [HttpGet]
        public async Task<IActionResult> Plans()
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user!.Id);
            if (member == null) return RedirectToAction("Index", "Dashboard");

            var activeMembership = await _db.Memberships
                .Include(m => m.Plan)
                .Where(m => m.MemberId == member.MemberId &&
                            m.EndDate >= DateTime.UtcNow.Date &&
                            m.OverrideStatus != MembershipStatus.Cancelled &&
                            m.OverrideStatus != MembershipStatus.Suspended)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            var plans = await _db.MembershipPlans
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Price)
                .ToListAsync();

            ViewBag.ActiveMembership = activeMembership;
            ViewBag.MemberId = member.MemberId;
            return View(plans);
        }

        // POST: /Shop/Checkout/{planId} - Create Stripe Checkout Session for new membership
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int planId)
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user!.Id);
            if (member == null) return RedirectToAction("Plans");

            var plan = await _db.MembershipPlans.FindAsync(planId);
            if (plan == null || !plan.IsActive) return NotFound();

            var existing = await _db.Memberships
                .Where(m => m.MemberId == member.MemberId &&
                            m.EndDate >= DateTime.UtcNow.Date &&
                            m.OverrideStatus != MembershipStatus.Cancelled &&
                            m.OverrideStatus != MembershipStatus.Suspended)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                TempData["Error"] = "You already have an active membership. Please wait until it expires to purchase a new one.";
                return RedirectToAction("Plans");
            }

            var totalAmount = plan.Price + plan.RegistrationFee;

            var successUrl = Url.Action("Success", "Shop", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action("Plans", "Shop", null, Request.Scheme);

            var session = await _stripe.CreatePlanCheckoutSessionAsync(
                plan, member, totalAmount, successUrl!, cancelUrl!);

            return Redirect(session.Url);
        }

        // POST: /Shop/RenewCheckout/{membershipId} - Create Stripe Checkout for renewal
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewCheckout(int membershipId)
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user!.Id);
            if (member == null) return RedirectToAction("Plans");

            var membership = await _db.Memberships
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId && m.MemberId == member.MemberId);

            if (membership == null) return NotFound();

            var renewalAmount = membership.Plan.RenewalFee > 0 ? membership.Plan.RenewalFee : membership.Plan.Price;
            var successUrl = Url.Action("Success", "Shop", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action("Plans", "Shop", null, Request.Scheme);

            var session = await _stripe.CreateRenewalCheckoutSessionAsync(
                membership, renewalAmount, successUrl!, cancelUrl!);

            return Redirect(session.Url);
        }

        // GET: /Shop/Success - Stripe redirects here after successful payment
        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
                return RedirectToAction("Plans");

            var user = await _userManager.GetUserAsync(User);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user!.Id);
            if (member == null) return RedirectToAction("Plans");

            try
            {
                // Retrieve Stripe session to confirm payment and get metadata
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(session_id);

                if (session.PaymentStatus != "paid")
                {
                    TempData["Error"] = "Payment was not completed. Please try again.";
                    return RedirectToAction("Plans");
                }

                // Check what type of payment this was
                var sessionType = session.Metadata.ContainsKey("type") ? session.Metadata["type"] : "new";
                var planId = session.Metadata.ContainsKey("planId") ? int.Parse(session.Metadata["planId"]) : 0;
                var membershipId = session.Metadata.ContainsKey("membershipId") ? int.Parse(session.Metadata["membershipId"]) : 0;

                if (sessionType == "renewal" && membershipId > 0)
                {
                    var renewalResult = await _renewal.RenewAsync(
                        membershipId, planId > 0 ? planId : (int?)null, 0, "Online Payment");

                    var membership = await _db.Memberships.Include(m => m.Plan)
                        .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

                    if (membership != null)
                    {
                        await _paymentService.RecordPaymentAsync(
                            member.MemberId, membershipId,
                            (decimal)(session.AmountTotal ?? 0) / 100,
                            PaymentMethod.Online, PaymentType.Renewal,
                            session.PaymentIntentId, "Online renewal via Stripe", "Online");
                    }

                    ViewBag.Type = "renewal";
                }
                else if (planId > 0)
                {
                    var alreadyEnrolled = await _db.Memberships
                        .AnyAsync(m => m.MemberId == member.MemberId &&
                                       m.PlanId == planId &&
                                       m.CreatedAt >= DateTime.UtcNow.AddMinutes(-10));

                    if (!alreadyEnrolled)
                    {
                        var (enrollOk, enrollMsg, newMembershipId) = await _enrollment.EnrollAsync(
                            member.MemberId, planId, DateTime.UtcNow.Date, 0, "Online Payment");

                        if (enrollOk && newMembershipId.HasValue)
                        {
                            await _paymentService.RecordPaymentAsync(
                                member.MemberId, newMembershipId.Value,
                                (decimal)(session.AmountTotal ?? 0) / 100,
                                PaymentMethod.Online, PaymentType.Registration,
                                session.PaymentIntentId, "New membership via Stripe", "Online");
                        }
                    }

                    ViewBag.Type = "new";
                }

                var newMembership = await _db.Memberships
                    .Include(m => m.Plan)
                    .Where(m => m.MemberId == member.MemberId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                ViewBag.Member = member;
                ViewBag.SessionId = session_id;
                ViewBag.AmountPaid = (decimal)(session.AmountTotal ?? 0) / 100;
                return View(newMembership);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Payment verification failed: " + ex.Message;
                return RedirectToAction("Plans");
            }
        }

        // GET: /Shop/Cancel
        [HttpGet]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment was cancelled. No charges were made.";
            return RedirectToAction("Plans");
        }
    }
}
