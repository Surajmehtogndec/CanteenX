using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http.HttpResults;
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
                TempData["LoginError"] =
             result?.Message ??
             "Invalid email/phone number or password.";

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            return RedirectToAction(
                "Index",
                "Home"
            );
        }


        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var client =
                _httpClientFactory.CreateClient("CanteenX.Api");
            var response = await client.PostAsJsonAsync(
                "api/Auth/register",
                new
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword
                });
            var result =
                await response.Content
                    .ReadFromJsonAsync<AuthResponseDto>();
            if (result == null || !result.Success)
            {
                ModelState.AddModelError(
                    "",
                    result?.Message ?? "Registration failed."
                );
                return View(model);
            }
            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleLoginCallback), "Account")

            };
            return Challenge(properties,"Google");
        }


        [HttpGet]
        public async Task<IActionResult> GoogleLoginCallback()
        {
            var googleResult = await HttpContext.AuthenticateAsync(
                "GoogleExternal"
            );


            if (!googleResult.Succeeded)
            {
                TempData["Error"] =
                    "Google authentication failed.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Get ID Token
            var idToken =
                googleResult.Properties?
                    .GetTokenValue("id_token");

            if (string.IsNullOrEmpty(idToken))
            {
                TempData["Error"] =
                    "Google ID Token was not received.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Create API client
            var client =
                _httpClientFactory.CreateClient(
                    "CanteenX.Api"
                );

            // Send ID Token to API
            var response =
                await client.PostAsJsonAsync(
                    "api/Auth/google/login",
                    new
                    {
                        IdToken = idToken
                    }
                );

            // Read API response
            var apiResult =
                await response.Content
                    .ReadFromJsonAsync<AuthResponseDto>();


            // API login failed
            if (!response.IsSuccessStatusCode ||
                apiResult == null ||
                !apiResult.Success)
            {
                TempData["LoginError"] =
            apiResult?.Message ??
            "Invalid email/phone number or password.";

                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }
            // Create CanteenX MVC session
            await CreateUserSessionAsync(apiResult);

            // Remove temporary Google cookie
            await HttpContext.SignOutAsync(
                "GoogleExternal"
            );

            // Login successful
            return RedirectToAction(
                "Index",
                "Home"
            );


        }

        private async Task CreateUserSessionAsync(
        AuthResponseDto result)
        {
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
            result.User.Email ?? string.Empty
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
        ),
        new Claim(
            ClaimTypes.Role,
            result.User.Role
        )
    };


            // Phone Number optional hai
            if (!string.IsNullOrEmpty(result.User.PhoneNumber))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.MobilePhone,
                        result.User.PhoneNumber
                    )
                );
            }


            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );


            var principal = new ClaimsPrincipal(identity);


            // MVC Authentication Cookie Create
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );


            // JWT Access Token Session mein store
            HttpContext.Session.SetString(
                  "AccessToken",
                  result.AccessToken
              );
        }



        //[HttpGet]
        //public IActionResult GoogleRegister()
        //{
        //    var properties = new AuthenticationProperties
        //    {
        //        RedirectUri = Url.Action(nameof(GoogleRegisterCallback), "Account")

        //    };
        //    return Challenge(properties, "Google");

        //}



    //    [HttpGet]
    //    public async Task<IActionResult> GoogleRegisterCallback()
    //    {
    //        var googleResult = await HttpContext.AuthenticateAsync(
    //               "GoogleExternal"
    //           );

    //        if (!googleResult.Succeeded)
    //        {
    //            TempData["Error"] = "Google authentication failed.";

    //            return RedirectToAction(
    //                "Register",
    //                "Account"
    //            );
    //        }

    //        // ab hum google id token ko retrieve karenge
    //        var idToken = googleResult.Properties?.GetTokenValue("id_token");
    //        if (string.IsNullOrEmpty(idToken))
    //        {
    //            TempData["Error"] =
    //                "Google ID Token was not received.";

    //            return RedirectToAction(
    //                "Register",
    //                "Account"
    //            );
    //        }


    //        // CanteenX API client
    //        var client = _httpClientFactory
    //            .CreateClient("CanteenX.Api");



    //        // API ko Google ID Token send karo
    //        var response = await client.PostAsJsonAsync(
    //            "api/Auth/google/register",
    //            new
    //            {
    //                IdToken = idToken
    //            }
    //        );


    //        // API response ko read karo

    //        var apiResult =
    //            await response.Content
    //       .ReadFromJsonAsync<AuthResponseDto>();


    //        if (!response.IsSuccessStatusCode ||
    //                apiResult == null ||
    //                !apiResult.Success)
    //        {
    //            TempData["Error"] =
    //                apiResult?.Message ??
    //                "Google registration failed.";

    //            return RedirectToAction(
    //                "Register",
    //                "Account"
    //            );
    //        }

    //        // MVC authentication cookie create
    //        await CreateUserSessionAsync(apiResult);
    //        // Google temporary authentication cookie remove
    //        await HttpContext.SignOutAsync(
    //            "GoogleExternal"
    //        );

    //        return RedirectToAction(
    //    "Index",
    //    "Home"
    //);

    //    }

    }
}
