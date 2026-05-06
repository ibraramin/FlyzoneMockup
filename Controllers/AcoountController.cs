using Flyzone.Data;
using Flyzone.Models;
using Flyzone.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.RateLimiting;

namespace Flyzone.Controllers
{
    [EnableRateLimiting("AuthRateLimit")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction("Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account is locked out.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        // --- REGISTER ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                UserName = model.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // --- LOGOUT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // --- DASHBOARD ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var applications = await _context.ServiceApplications
                .Where(a => a.UserId == user.Id)
                .Include(a => a.Service)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            int total = applications.Count;
            int inProgress = applications.Count(a => 
                a.Status == ApplicationStatus.Submitted ||
                a.Status == ApplicationStatus.UnderReview ||
                a.Status == ApplicationStatus.WaitingPayment);
            int actionRequired = applications.Count(a => a.Status == ApplicationStatus.ActionRequired);
            int completed = applications.Count(a => 
                a.Status == ApplicationStatus.Completed || a.Status == ApplicationStatus.Rejected);

            var recentActivity = applications.Take(5).Select(a => new RecentActivityItem
            {
                ApplicationId = a.Id,
                ApplicationType = a.Service.ServiceName,
                DateSubmitted = a.CreatedAt,
                Status = a.Status.ToString()
            }).ToList();

            var activeApp = applications
                .FirstOrDefault(a => a.Status != ApplicationStatus.Completed && a.Status != ApplicationStatus.Rejected);

            ActiveApplicationInfo? currentActive = null;
            if (activeApp != null)
            {
                currentActive = new ActiveApplicationInfo
                {
                    ApplicationId = activeApp.Id,
                    ServiceName = activeApp.Service.ServiceName,
                    Status = activeApp.Status
                };
            }

            var model = new DashboardViewModel
            {
                FirstName = user.FirstName,
                TotalApplications = total,
                InProgress = inProgress,
                ActionRequired = actionRequired,
                Completed = completed,
                RecentActivity = recentActivity,
                CurrentActiveApplication = currentActive
            };

            return View(model);
        }
    }
}