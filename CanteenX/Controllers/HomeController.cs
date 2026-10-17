using CanteenX.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CanteenX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //if(User.Identity != null && User.Identity.IsAuthenticated)
            //{
            //    if (User.IsInRole("Admin"))
            //    {
            //        return RedirectToAction("Index", "Admin");
            //    }
            //    else if (User.IsInRole("CanteenStaff"))
            //    {
            //        return RedirectToAction("Index", "CanteenOwner");
            //    }
            //   else if (User.IsInRole("Student"))
            //    {
            //        return RedirectToAction("Index", "Home");
            //    }

            //}
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
