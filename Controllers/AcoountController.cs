using Microsoft.AspNetCore.Mvc;

namespace Flyzone.Controllers
{
    public class AccountController : Controller
    {
        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Accept ANY credentials for the mockup
            TempData["AuthAction"] = "Logged In";
            TempData["Email"] = email;
            TempData["Password"] = password; 
            
            // Redirect straight to the dummy dashboard
            return RedirectToAction("Dashboard");
        }

        // --- REGISTER ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string email, string phoneNumber, string password)
        {
            // Accept ANY credentials for the mockup
            TempData["AuthAction"] = "Registered";
            TempData["Name"] = $"{firstName} {lastName}";
            TempData["Email"] = email;
            TempData["Phone"] = phoneNumber;
            TempData["Password"] = password;

            // Redirect straight to the dummy dashboard
            return RedirectToAction("Dashboard");
        }

        // --- DASHBOARD (Dummy) ---
        [HttpGet]
        public IActionResult Dashboard()
        {
            // If someone tries to access the dashboard directly without logging in/registering,
            // TempData will be null. We can just let it load anyway for the mockup.
            return View();
        }
    }
}