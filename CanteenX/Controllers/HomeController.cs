using CanteenX.Models;
using CanteenX.Models.Home;
using CanteenX.Service.Home;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CanteenX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeService _homeService;

        public HomeController(ILogger<HomeController> logger, IHomeService homeService)
        {
            _logger = logger;
            _homeService = homeService;
        }

            public async Task<IActionResult> Index(CancellationToken cancellationToken)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Admin"))
                    {
                        return RedirectToAction("AdminDashboard", "Admin");
                    }
                    else if (User.IsInRole("CanteenStaff"))
                    {
                        return RedirectToAction("CanteenOwnerDashboard", "CanteenOwner");
                    }
                }


                var canteens = await _homeService.GetActiveCanteensAsync(cancellationToken);
                var availableMenuItems = await _homeService.GetAvailableMenuItemsAsync(cancellationToken);
                var model = new HomePageViewModel
                {
                ActiveCanteens = canteens,
                PopularItems = availableMenuItems
                };
                return View(model);
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
