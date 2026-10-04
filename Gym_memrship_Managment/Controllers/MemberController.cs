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
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly IMembershipService _membershipService;

        public MemberController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
            IAuditService audit, IMembershipService membershipService)
        {
            _db = db;
            _userManager = userManager;
            _audit = audit;
            _membershipService = membershipService;
        }

        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            const int pageSize = 15;
            var query = _db.MemberProfiles.Where(m => !m.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.FullName.Contains(search) ||
                                         m.Phone.Contains(search) ||
                                         m.MembershipNumber.Contains(search) ||
                                         m.Email.Contains(search));

            if (Enum.TryParse<MemberStatus>(status, out var statusEnum))
                query = query.Where(m => m.Status == statusEnum);

            var total = await query.CountAsync();
            var members = await query.OrderByDescending(m => m.CreatedAt)
                .OrderByDescending(x => x.MemberId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return View(new MemberListVm
            {
                Members = members,
                SearchTerm = search,
                StatusFilter = status,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                TotalCount = total
            });
        }

        [HttpGet]
        public IActionResult Create() => View(new MemberCreateVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberCreateVm model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.MemberProfiles.AnyAsync(m => m.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var counter = await _db.MemberProfiles.CountAsync() + 1;
            var member = new MemberProfile
            {
                MembershipNumber = $"GYM{DateTime.UtcNow.Year}{counter:D4}",
                FullName = model.FullName,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                EmergencyContactName = model.EmergencyContactName,
                EmergencyContactPhone = model.EmergencyContactPhone,
                JoinDate = DateTime.UtcNow,
                Status = MemberStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (model.CreateUserAccount && !string.IsNullOrEmpty(model.Password))
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    member.UserId = user.Id;
                }
                else
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError("", e.Description);
                    return View(model);
                }
            }

            _db.MemberProfiles.Add(member);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "CreateMember",
                "MemberProfile", member.MemberId.ToString(), $"Created member {member.FullName}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = $"Member '{member.FullName}' created successfully.";
            return RedirectToAction("Details", new { id = member.MemberId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var member = await _db.MemberProfiles
                .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
                .Include(m => m.Payments)
                .Include(m => m.Fines)
                .Include(m => m.BatchEnrollments).ThenInclude(be => be.Batch).ThenInclude(b => b.Trainer)
                .FirstOrDefaultAsync(m => m.MemberId == id && !m.IsDeleted);

            if (member == null) return NotFound();

            var activeMembership = await _membershipService.GetActiveMembershipAsync(id);
            var recentAttendance = await _db.Attendances
                .Include(a => a.Batch)
                .Where(a => a.MemberId == id)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            return View(new MemberDetailsVm
            {
                Member = member,
                ActiveMembership = activeMembership,
                MembershipHistory = member.Memberships.OrderByDescending(m => m.CreatedAt).ToList(),
                RecentPayments = member.Payments.OrderByDescending(p => p.PaymentDate).Take(5).ToList(),
                Fines = member.Fines.ToList(),
                RecentAttendance = recentAttendance,
                CurrentBatch = member.BatchEnrollments.FirstOrDefault(be => be.Status == EnrollmentStatus.Active)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var member = await _db.MemberProfiles.FindAsync(id);
            if (member == null || member.IsDeleted) return NotFound();

            return View(new MemberEditVm
            {
                MemberId = member.MemberId,
                FullName = member.FullName,
                Gender = member.Gender,
                DateOfBirth = member.DateOfBirth,
                Phone = member.Phone,
                Email = member.Email,
                Address = member.Address,
                EmergencyContactName = member.EmergencyContactName,
                EmergencyContactPhone = member.EmergencyContactPhone,
                Status = member.Status
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MemberEditVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var member = await _db.MemberProfiles.FindAsync(model.MemberId);
            if (member == null || member.IsDeleted) return NotFound();

            if (await _db.MemberProfiles.AnyAsync(m => m.Email == model.Email && m.MemberId != model.MemberId))
            {
                ModelState.AddModelError("Email", "Email already in use.");
                return View(model);
            }

            member.FullName = model.FullName;
            member.Gender = model.Gender;
            member.DateOfBirth = model.DateOfBirth;
            member.Phone = model.Phone;
            member.Email = model.Email;
            member.Address = model.Address;
            member.EmergencyContactName = model.EmergencyContactName;
            member.EmergencyContactPhone = model.EmergencyContactPhone;
            member.Status = model.Status;
            member.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "EditMember",
                "MemberProfile", member.MemberId.ToString(), $"Updated member {member.FullName}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Member updated successfully.";
            return RedirectToAction("Details", new { id = member.MemberId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _db.MemberProfiles.FindAsync(id);
            if (member == null) return NotFound();

            member.IsDeleted = true;
            member.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "DeleteMember",
                "MemberProfile", id.ToString(), $"Soft-deleted member {member.FullName}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "Member deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new object[] { });
            var members = await _db.MemberProfiles
                .Where(m => !m.IsDeleted && (m.FullName.Contains(term) || m.MembershipNumber.Contains(term) || m.Phone.Contains(term)))
                .Select(m => new { m.MemberId, m.FullName, m.MembershipNumber, m.Phone, StatusText = m.Status.ToString() })
                .Take(10)
                .ToListAsync();
            return Json(members);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MyProfile()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return Challenge();
            if (!User.IsInRole("Member")) return Forbid();

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var member = await _db.MemberProfiles
                .Include(m => m.Memberships).ThenInclude(ms => ms.Plan)
                .Include(m => m.BatchEnrollments).ThenInclude(be => be.Batch).ThenInclude(b => b.Trainer)
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsDeleted);

            if (member == null) 
            {
                TempData["Error"] = "Your member profile was not found or has been deleted.";
                return Redirect("/Dashboard");
            }

            var activeMembership = await _membershipService.GetActiveMembershipAsync(member.MemberId);
            var recentAttendance = await _db.Attendances
                .Include(a => a.Batch)
                .Where(a => a.MemberId == member.MemberId)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            var recentPayments = await _db.Payments
                .Where(p => p.MemberId == member.MemberId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            var pendingFines = await _db.Fines
                .Where(f => f.MemberId == member.MemberId)
                .ToListAsync();

            return View(new MemberDetailsVm
            {
                Member = member,
                ActiveMembership = activeMembership,
                MembershipHistory = member.Memberships.OrderByDescending(m => m.CreatedAt).ToList(),
                RecentPayments = recentPayments,
                Fines = pendingFines,
                RecentAttendance = recentAttendance,
                CurrentBatch = member.BatchEnrollments.FirstOrDefault(be => be.Status == EnrollmentStatus.Active)
            });
        }
    
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(int memberId, IFormFile profileImage)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return Challenge();
            if (!User.IsInRole("Admin") && !User.IsInRole("Staff") && !User.IsInRole("Member")) return Forbid();

            var member = await _db.MemberProfiles.FindAsync(memberId);
            if (member == null || member.IsDeleted) return NotFound();

            // Security: If logged in as Member, can only update own profile
            if (User.IsInRole("Member"))
            {
                var userId = _userManager.GetUserId(User);
                if (member.UserId != userId) return Forbid();
            }

            if (profileImage != null && profileImage.Length > 0)
            {
                // Validate extension
                var ext = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Only JPG and PNG images are allowed.";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }

                if (profileImage.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Image must be under 5MB.";
                    return Redirect(Request.Headers["Referer"].ToString() ?? "/");
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"profile_{member.MemberId}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(member.ProfileImage))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", member.ProfileImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                member.ProfileImage = "/images/profiles/" + uniqueFileName;
                await _db.SaveChangesAsync();

                await _audit.LogAsync(User.Identity?.Name, User.Identity?.Name, "Update Profile Image", "MemberProfile", member.MemberId.ToString(), "Uploaded new profile photo.", HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["Success"] = "Profile photo updated successfully!";
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

    }
}

