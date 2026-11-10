using CanteenX.Models.CanteenOwner;
using CanteenX.Service.CanteenOwner;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanteenX.Controllers
{
    [Authorize(Roles = "CanteenStaff")]
    public class CanteenOwnerController : Controller
    {
        private readonly ICanteenOwnerService _canteenOwnerService;
        private readonly ILogger<CanteenOwnerController> _logger;

        public CanteenOwnerController(ICanteenOwnerService canteenOwnerService,ILogger<CanteenOwnerController> logger)
        {
            _canteenOwnerService = canteenOwnerService;
            _logger = logger;
        }
        public IActionResult CanteenOwnerDashboard() => View();
        public IActionResult CanteenOwnerAccounts() => View();
      

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _canteenOwnerService.RevokeTokenAsync(refreshToken);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("jwt_token");
            Response.Cookies.Delete("refresh_token");
            return RedirectToAction("Index", "Home");

        }

        [HttpGet]
        public async Task<IActionResult> AddMenuItem()
        {
            // Logged in user ki ID nikalna (agar zarurat ho)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            // 1. Cookie se JWT token nikalna
            var token = Request.Cookies["jwt_token"] ?? "";
            int canteenIdFromService = await _canteenOwnerService.GetCanteenIdByUserIdAsync(userId, token);
            // Service call karke CanteenId lana
            //int canteenId = await _canteenOwnerService.GetCanteenIdByUserIdAsync(userId);

            if (canteenIdFromService > 0)
            {
                // Agar CanteenId mil gayi, toh Model mein daal kar View ko bhej dein
                var model = new AddMenuItems
                {
                    CanteenId = canteenIdFromService
                };
                return View(model);
            }

            // Agar CanteenId nahi mili (0 aayi)
            TempData["ErrorMessage"] = "Could not find your canteen details.";
            return View(new AddMenuItems());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMenuItem(AddMenuItems model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // 1. POST request mein Token nikalna zaroori hai
            var token = Request.Cookies["jwt_token"] ?? "";
            var result = await _canteenOwnerService.AddMenuItemAsync(model,cancellationToken);
            if (result == null || !result.Success)
            {
                TempData["ErrorMessage"] = "Menu item could not be added. Please try again.";
                return View(model);
            }
            ModelState.Clear();
            TempData["SuccessMessage"] = "Menu item added successfully!";
            return RedirectToAction("AddMenuItem");
        }

        [HttpGet]
        public async Task<IActionResult> CanteenProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var cancellationToken = HttpContext.RequestAborted;
            var canteenProfile = await _canteenOwnerService.GetCanteenProfileDataAsync(userId, cancellationToken);
            if (canteenProfile == null)
            {
                TempData["ErrorMessage"] = "Could not retrieve your canteen profile.";
                return View(new CanteenProfileDto());
            }
            return View(canteenProfile);
        }

    }
}
