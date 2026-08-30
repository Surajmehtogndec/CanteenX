using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanteenX.Controllers
{
    public class AccountController : Controller
    {
        // 1. Private readonly variable declare karein
        private readonly IHttpClientFactory _httpClientFactory;

        // 2. Constructor ke through inject karein
        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client =
                _httpClientFactory.CreateClient("CanteenX.Api");

            var response = await client.PostAsJsonAsync(
                "api/Auth/login",
                new
                {
                    EmailOrPhone = model.EmailOrPhone,
                    Password = model.Password
                });

            var result =
                await response.Content
                    .ReadFromJsonAsync<AuthResponseDto>();

            if (result == null || !result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result?.Message ?? "Login failed."
                );

                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            result.User.Id
        ),

        new Claim(
            ClaimTypes.Name,
            result.User.FullName
        ),

        new Claim(
            ClaimTypes.Email,
            result.User.Email ?? ""
        ),

        new Claim(
            ClaimTypes.MobilePhone,
            result.User.PhoneNumber ?? ""
        ),

        new Claim(
            ClaimTypes.Role,
            result.User.Role
        ),

        new Claim(
            "AccessToken",
            result.AccessToken
        )
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction(
                "Index",
                "Home"
            );
        }
    }
}
