using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace CanteenX.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateCanteen()
        {
            return View();
        }

        public IActionResult RegisterCanteenLogin()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCanteenLogin( RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateCanteen", model);
            }

            var client = _httpClientFactory.CreateClient("CanteenX.Api");
            var response = await client.PostAsJsonAsync("api/admin/CanteenStaffRegister",
                new
                {
                  FullName = model.FullName,
                  Email = model.Email,
                  PhoneNumber = model.PhoneNumber,
                  Password = model.Password,
                  ConfirmPassword = model.ConfirmPassword
                });

            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result == null || !result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result?.Message ?? "Registration failed."
                );
                return View("CreateCanteen", model);
            }
            ModelState.Clear();

            // Success message set karein
            ViewBag.SuccessMessage = "Canteen staff account created successfully!";

            return View("CreateCanteen", new RegisterDto());
        }


       

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
                );
            return RedirectToAction("Index", "Home");
        }


        public IActionResult AddCanteens()
        {
            return View();
        }

    }
}
