// using System.Diagnostics;
// using Microsoft.AspNetCore.Mvc;
// using FlyzoneMockup.Models;

// namespace FlyzoneMockup.Controllers;

// public class HomeController : Controller
// {
//     public IActionResult Index()
//     {
//         return View();
//     }

//     public IActionResult Privacy()
//     {
//         return View();
//     }

//     [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//     public IActionResult Error()
//     {
//         return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//     }
// }


using Microsoft.AspNetCore.Mvc;

namespace FlyzoneMockup.Controllers
{    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard()
        {
            return View();
        }
    }
}