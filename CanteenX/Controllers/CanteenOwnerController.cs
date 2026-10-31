using CanteenX.Models.CanteenOwner;
using CanteenX.Service.CanteenOwner;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanteenX.Controllers
{
    
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkBankAccount(CanteenAccountDetailsDto dto,CancellationToken cancellationToken)
        {
            // 1. Logged-in user ki Identity ID get karna
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Session expired or invalid user. Please login again.";
                return RedirectToAction("Login", "Account"); // Apna Login route check kar lein
            }
            // 2. DTO mein UserId assign karna
            dto.UserId = userId;
            if (!ModelState.IsValid)    
            {
                return RedirectToAction("CanteenOwnerDashboard");
            }
            var result = await _canteenOwnerService.LinkBankAccountAsync(dto, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["Success"] = result.Message;
                // Success ke baad owner ko dashboard par bhej dein
                return RedirectToAction("CanteenOwnerDashboard");
            }

            // Error aane par form wapas dikhayein aur error message display karein
            TempData["Error"] = result.Message;
            return RedirectToAction("CanteenOwnerDashboard");
        }

    }
}
