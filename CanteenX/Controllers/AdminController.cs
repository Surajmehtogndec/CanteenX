using CanteenX.Models.Admin;
using CanteenX.Models.Auth;
using CanteenX.Service.Interface;
using CanteenX.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace CanteenX.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        public IActionResult AdminDashboard() => View();
        public IActionResult UserDirectory() => View();
        public IActionResult Transactions() => View();
        public IActionResult OrderHistorys() => View();
        public IActionResult CanteenDirectory() => View();
        public IActionResult RegisterCanteenLogin() => View();

        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "test-admin-id";
            var profileData = await _adminService.GetProfileDataAsync(userId);

            if (profileData == null)
            {
                TempData["ErrorMessage"] = "Could not load profile data from the server.";
                profileData = new UserProfileDto();
            }

            return View(profileData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCanteenLogin(RegisterCanteensDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _adminService.RegisterCanteenStaffAsync(model);

            if (result == null || !result.Success)
            {
                TempData["ErrorMessage"] = result?.Message ?? "An error occurred while registering the canteen.";
                return View(model);
            }

            ModelState.Clear();
            TempData["SuccessMessage"] = "Canteen staff account created successfully!";
            return View(new RegisterCanteensDto());
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _adminService.RevokeTokenAsync(refreshToken);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("jwt_token");
            Response.Cookies.Delete("refresh_token");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> AddCanteens()
        {
            ViewBag.CanteenStaff = await _adminService.GetCanteenStaffAsync();
            return View(new AddCanteens());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCanteens(AddCanteens dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CanteenStaff = await _adminService.GetCanteenStaffAsync();
                return View(dto);
            }

            var result = await _adminService.AddCanteenAsync(dto);

            if (result == null || !result.Success)
            {
                ViewBag.CanteenStaff = await _adminService.GetCanteenStaffAsync();
                ModelState.AddModelError("", result?.Message ?? "API Request Failed. Please check your data.");
                return View(dto);
            }

            ModelState.Clear();
            ViewBag.CanteenStaff = await _adminService.GetCanteenStaffAsync();
            ViewBag.SuccessMessage = "Canteen added successfully.";
            return View(new AddCanteens());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = "Please enter valid profile details.";
                return RedirectToAction("UserProfile");
            }

            var accessToken = User.FindFirst("AccessToken")?.Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return RedirectToAction("Login", "Account");

            var response = await _adminService.UpdateProfileAsync(userId, model, accessToken);

            if (response.IsSuccessStatusCode)
            {
                TempData["ProfileSuccess"] = "Profile updated successfully!";
                if (User.Identity is ClaimsIdentity identity)
                {
                    var existingClaim = identity.FindFirst(identity.NameClaimType) ?? identity.FindFirst("FullName");
                    if (existingClaim != null) identity.RemoveClaim(existingClaim);
                    identity.AddClaim(new Claim(existingClaim?.Type ?? ClaimTypes.Name, model.FullName!));
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                }
            }
            else
            {
                TempData["ProfileError"] = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Your session may have expired."
                    : "Unable to update profile.";
            }

            return RedirectToAction("UserProfile");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDirectory(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? status = null, string? role = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var accessToken = User.FindFirst("AccessToken")?.Value;
                if (string.IsNullOrWhiteSpace(accessToken))
                    return Unauthorized(new { success = false, message = "Access token is missing." });

                var query = new Dictionary<string, string?>
                {
                    ["PageNumber"] = Math.Max(1, pageNumber).ToString(),
                    ["PageSize"] = Math.Clamp(pageSize, 1, 50).ToString(),
                    ["SearchTerm"] = searchTerm,
                    ["Status"] = status,
                    ["Role"] = role
                };

                var queryString = string.Join("&", query.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

                var response = await _adminService.GetUserDirectoryAsync(queryString, accessToken, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { success = false, message = "API request failed." });

                using var document = JsonDocument.Parse(json);
                return Content(document.RootElement.GetRawText(), "application/json");
            }
            catch (OperationCanceledException) { return StatusCode(499, new { success = false, message = "Request cancelled." }); }
            catch (Exception) { return StatusCode(502, new { success = false, message = "Unable to connect." }); }
        }

        [HttpGet]
        public IActionResult EditUser(string id, string fullName, string email, string role, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction(nameof(UserDirectory));
            LoadEditUserRoles(role);
            return View(new EditUserViewModel { Id = id, FullName = fullName, Email = email, Role = role, IsActive = isActive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                LoadEditUserRoles(model.Role);
                return View(model);
            }

            var accessToken = User.FindFirst("AccessToken")?.Value;
            if (string.IsNullOrWhiteSpace(accessToken)) return RedirectToAction("Login", "Account");

            try
            {
                var response = await _adminService.UpdateUserAsync(model.Id!, model, accessToken, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.SuccessMessage = "User details updated successfully.";
                    return RedirectToAction(nameof(UserDirectory));
                }

                ModelState.AddModelError(string.Empty, response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "You are not authorized."
                    : "Unable to update user details.");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to the API.");
            }

            LoadEditUserRoles(model.Role);
            return View(model);
        }

        [HttpDelete("Admin/DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id)) return Json(new { success = false, message = "User ID is required." });

            var accessToken = User.FindFirst("AccessToken")?.Value;
            if (string.IsNullOrWhiteSpace(accessToken)) return Json(new { success = false, message = "Session expired." });

            try
            {
                var response = await _adminService.DeleteUserAsync(id, accessToken, cancellationToken);

                if (response.IsSuccessStatusCode) return Json(new { success = true, message = "User deleted successfully." });
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return Json(new { success = false, message = "User not found." });

                return Json(new { success = false, message = "Failed to delete user." });
            }
            catch (Exception) { return Json(new { success = false, message = "Server error occurred." }); }
        }

        [HttpPost("Admin/ExportUserPdf")]
        public IActionResult ExportUsersPdf([FromBody] List<ExportPdfDto> users)
        {
            if (users == null || !users.Any()) return BadRequest(new { message = "No users data received." });

            try
            {
                byte[] pdfBytes = _adminService.GenerateUsersPdf(users);
                return File(pdfBytes, "application/pdf", "CanteenX_Selected_Users.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QUESTPDF ERROR");
                return StatusCode(500, new { message = "PDF Generation Error: " + ex.Message });
            }
        }

        private void LoadEditUserRoles(string? selectedRole)
        {
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                new[] { new { Value = "Admin", Text = "Admin" }, new { Value = "Student", Text = "Student" }, new { Value = "CanteenStaff", Text = "Canteen Staff" } },
                "Value", "Text", selectedRole);
        }
    }
}