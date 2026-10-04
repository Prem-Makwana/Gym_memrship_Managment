using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Gym_memrship_Managment.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public AccountController(SignInManager<ApplicationUser> signIn,
            UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _signIn = signIn;
            _userManager = userManager;
            _audit = audit;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) 
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                await _audit.LogAsync(user?.Id, model.Email, "Login", null, null, "User logged in",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account is locked out. Try again later.");
            else
                ModelState.AddModelError("", "Invalid email or password.");

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

                        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                await _signIn.SignOutAsync();
                await _audit.LogAsync(userId, User.Identity?.Name, "Logout", null, null, "User logged out",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        
    }
}











