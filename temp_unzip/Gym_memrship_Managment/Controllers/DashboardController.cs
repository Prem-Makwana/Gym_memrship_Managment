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
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboard;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DashboardController(IDashboardService dashboard,
            UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _dashboard = dashboard;
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (User.IsInRole("Admin"))
            {
                var vm = await _dashboard.GetAdminDashboardAsync();
                return View("Admin", vm);
            }
            else if (User.IsInRole("Staff"))
            {
                var vm = await _dashboard.GetStaffDashboardAsync();
                return View("Staff", vm);
            }
            else
            {
                var memberProfile = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == user.Id);
                if (memberProfile == null) return View("NoProfile");
                var vm = await _dashboard.GetMemberDashboardAsync(memberProfile.MemberId);
                return View("Member", vm);
            }
        }
    }
}
