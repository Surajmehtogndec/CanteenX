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
        public IActionResult AddMenuItem() => View();

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


    }
}
