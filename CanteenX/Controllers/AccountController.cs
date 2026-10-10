using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanteenX.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;

        // Constants for magic strings (Prevents typos in production)
        private const string ApiClientName = "CanteenX.Api";
        private const string GoogleScheme = "GoogleExternal";
        private const string SessionAccessToken = "AccessToken";

        public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // ==========================================
        // LOCAL LOGIN (Optimized for AJAX / SweetAlert)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/Auth/login", new
                {
                    model.EmailOrPhone,
                    model.Password
                });

                var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                // Agar login fail hua
                if (result == null || !result.Success)
                {
                    TempData["LoginError"] = result?.Message ?? "Invalid email or password.";
                    return View(model);
                }

                // Login Success - Session Create karein
                await CreateUserSessionAsync(result,model.RememberMe);

                // Naye page (Dashboard) par SweetAlert dikhane ke liye TempData set karein
                TempData["LoginSuccess"] = "Welcome back! You have successfully logged in.";

                // Role ke hisaab se seedha redirect karein (No JSON)
                switch (result.User.Role)
                {
                    case "Admin":
                        return RedirectToAction("Index", "Admin");

                    case "CanteenStaff":
                        return RedirectToAction("Index", "CanteenOwner");

                    case "Student":
                        return RedirectToAction("Index", "Home");

                    default:
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        TempData["LoginError"] = "Invalid user role configuration.";
                        return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during local login for {Email}", model.EmailOrPhone);
                TempData["LoginError"] = "Server is currently unreachable. Please try again later.";
                return View(model);
            }
        }

        // ==========================================
        // GOOGLE LOGIN (Lightweight & Protected)
        // ==========================================
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleLoginCallback)) };
            return Challenge(properties, "Google");
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleLoginCallback()
        {
            try
            {
                var googleResult = await HttpContext.AuthenticateAsync(GoogleScheme);

                if (!googleResult.Succeeded)
                {
                    TempData["LoginError"] = "Google authentication was cancelled or failed.";
                    return RedirectToAction(nameof(Login));
                }

                var idToken = googleResult.Properties?.GetTokenValue("id_token");
                if (string.IsNullOrEmpty(idToken))
                {
                    TempData["LoginError"] = "Google configuration error. ID Token missing.";
                    return RedirectToAction(nameof(Login));
                }

                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/Auth/google/login", new { IdToken = idToken });
                var apiResult = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                if (!response.IsSuccessStatusCode || apiResult == null || !apiResult.Success)
                {
                    TempData["LoginError"] = apiResult?.Message ?? "Google login failed on the server.";
                    await HttpContext.SignOutAsync(GoogleScheme);
                    return RedirectToAction(nameof(Login));
                }

                await CreateUserSessionAsync(apiResult, true);
                await HttpContext.SignOutAsync(GoogleScheme);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during Google Login Callback.");
                TempData["LoginError"] = "An unexpected network error occurred. Please try again.";
                return RedirectToAction(nameof(Login));
            }
        }

        // ==========================================
        // REGISTRATION & PASSWORD MANAGEMENT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/Auth/register", model);
                var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                if (result == null || !result.Success)
                {
                    ModelState.AddModelError("", result?.Message ?? "Registration failed.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Account created successfully! Please login.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration.");
                ModelState.AddModelError("", "Service unavailable. Please try again later.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/Auth/forgot-password", new { model.Email });
                var result = await response.Content.ReadFromJsonAsync<ApiResponseDto>();

                if (!response.IsSuccessStatusCode || result == null || !result.Success)
                {
                    ViewBag.ErrorMessage = result?.Message ?? "This email is not registered with us.";
                    return View(model);
                }

                ViewBag.SuccessMessage = "Password reset email sent successfully.";
                ModelState.Clear();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset.");
                ViewBag.ErrorMessage = "Server error. Please try again later.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? email, string? token)
        {
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient(ApiClientName);
                    var url = $"api/Auth/validate-reset-token?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["Error"] = "This password reset link has expired or is invalid.";
                        return RedirectToAction(nameof(ForgotPassword));
                    }

                    HttpContext.Session.SetString("ResetPasswordEmail", email);
                    HttpContext.Session.SetString("ResetPasswordToken", token);
                    return RedirectToAction(nameof(ResetPassword));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating reset token.");
                    TempData["Error"] = "Service unavailable. Please try again.";
                    return RedirectToAction(nameof(ForgotPassword));
                }
            }

            var sessionEmail = HttpContext.Session.GetString("ResetPasswordEmail");
            var sessionToken = HttpContext.Session.GetString("ResetPasswordToken");

            if (string.IsNullOrWhiteSpace(sessionEmail) || string.IsNullOrWhiteSpace(sessionToken))
            {
                TempData["Error"] = "Invalid or expired session.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordDto { Email = sessionEmail, Token = sessionToken });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/Auth/reset-password", model);
                var result = await response.Content.ReadFromJsonAsync<ApiResponseDto>();

                if (!response.IsSuccessStatusCode || result == null || !result.Success)
                {
                    ViewBag.ErrorMessage = result?.Message ?? "Unable to reset password.";
                    return View(model);
                }

                HttpContext.Session.Remove("ResetPasswordEmail");
                HttpContext.Session.Remove("ResetPasswordToken");

                ViewBag.SuccessMessage = "Password reset successfully. You can now login.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password.");
                ViewBag.ErrorMessage = "Server error. Please try again later.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        private async Task CreateUserSessionAsync(AuthResponseDto result, bool RememberMe = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User.Id),
                new Claim(ClaimTypes.Name, result.User.FullName),
                new Claim(ClaimTypes.Email, result.User.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, result.User.Role),
                new Claim(SessionAccessToken, result.AccessToken)
            };

            if (!string.IsNullOrEmpty(result.User.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, result.User.PhoneNumber));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe
            };
            if (RememberMe)
            {
                authProperties.ExpiresUtc =
                 DateTimeOffset.UtcNow.AddDays(30);

                authProperties.AllowRefresh = true;
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            HttpContext.Session.SetString(SessionAccessToken, result.AccessToken);
        }
    }
}