using CanteenX.Models.Admin;
using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;

namespace CanteenX.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AdminController> _logger;

        private const string ApiClientName = "CanteenX.Api";

        public AdminController(IHttpClientFactory httpClientFactory, ILogger<AdminController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public IActionResult AdminDashboard()
        {
            return View();
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]

        public IActionResult UserDirectory(){
            return View();
        }
        public IActionResult Transactions()
        {
            return View();
        }

        public IActionResult OrderHistorys()
        {
            return View();
        }

        public IActionResult CanteenDirectory()
        {
            return View();
        }



        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            // 1. Logged-in user ki ID nikalein (Testing ke liye default id fallback add hai)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "test-admin-id";

            // 2. Apne production-ready method ko call karein
            var profileData = await LoadProfileDataAsync(userId);

            // 3. Agar API fail ho jaye ya data na mile, toh empty model banayein taaki page crash na ho
            if (profileData == null)
            {
                TempData["ErrorMessage"] = "Could not load profile data from the server.";
                profileData = new UserProfileDto(); // Khali form dikhane ke liye fallback
            }

            // 4. Data bhara hua model View ko pass kar dein
            return View(profileData);
        }

        public IActionResult RegisterCanteenLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCanteenLogin(RegisterCanteensDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient(ApiClientName);
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
                TempData["ErrorMessage"] = result?.Message ?? "An error occurred while registering the canteen ";
                return View(model);
            }
            ModelState.Clear();

            // Success message set karein
            TempData["SuccessMessage"] = "Canteen staff account created successfully!";
            return View(new RegisterCanteensDto());
        }




        [HttpGet]
        
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient(ApiClientName);
                    await client.PostAsJsonAsync("api/Auth/revoke-token", new { Token = refreshToken });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API Error: Could not revoke token during logout.");
                }
            }
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
                );
            HttpContext.Session.Clear();
            Response.Cookies.Delete("jwt_token");
            Response.Cookies.Delete("refresh_token");
            return RedirectToAction("Index", "Home");
        }





        [HttpGet]
        public async Task<IActionResult> AddCanteens()
        {
            await LoadCanteenStaffAsync();

            return View(new AddCanteens());
        }






        private async Task LoadCanteenStaffAsync()
        {
            var client =
                _httpClientFactory.CreateClient(ApiClientName);

            var response =
                await client.GetAsync(
                    "api/admin/canteen-staff");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.CanteenStaff =
                    new List<CanteenStaffDto>();

                return;
            }

            var result =
                await response.Content
                    .ReadFromJsonAsync<CanteenStaffResponseDto>();

            ViewBag.CanteenStaff =
                result?.Data ??
                new List<CanteenStaffDto>();
        }



        private async Task<UserProfileDto?> LoadProfileDataAsync(string userId, CancellationToken cancellationToken = default)
        {
            // 1. Validate Input
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("LoadProfileDataAsync called with empty userId.");
                return null;
            }

            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);

                // 2. String interpolation ($) use karein taaki {userId} dynamically URL mein pass ho
                var requestUrl = $"api/admin/loadprofiledata/{userId}";

                var response = await client.GetAsync(requestUrl, cancellationToken);

                // 3. Status check karein
                if (response.IsSuccessStatusCode)
                {
                    // 4. Lightweight JSON Deserialization (System.Text.Json)
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiAdminResponseDto<UserProfileDto>>(cancellationToken: cancellationToken);

                    return apiResponse?.Data;
                }
                else
                {
                    // API ne error return kiya (e.g., 404 Not Found, 500 Internal Server Error)
                    _logger.LogWarning("API Error: {StatusCode} while fetching profile for user {UserId}", response.StatusCode, userId);
                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Network errors ya API down hone par handle karein
                _logger.LogError(httpEx, "Network error while connecting to API.");
                return null;
            }
            catch (Exception ex)
            {
                // Unexpected errors catch karein taaki app crash na ho
                _logger.LogError(ex, "Unexpected error in LoadProfileDataAsync.");
                return null;
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCanteens(AddCanteens dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCanteenStaffAsync();
                return View(dto);
            }

            var client = _httpClientFactory.CreateClient(ApiClientName);

            using var formdata = new MultipartFormDataContent();

            formdata.Add(
                new StringContent(dto.UserId),
                "UserId");

            formdata.Add(
              new StringContent(dto.Location ?? ""),
              "Location");

            formdata.Add(
             new StringContent(dto.Description ?? ""),
             "Description");

            formdata.Add(
               new StringContent(dto.IsActive.ToString()),
               "IsActive");



            // images

            if (dto.ImageUrl != null)
            {
                var streamContent =
                    new StreamContent(dto.ImageUrl.OpenReadStream());



                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                      dto.ImageUrl.ContentType);



                formdata.Add(
                 streamContent,
                "ImageUrl", dto.ImageUrl.FileName);
            }



            var response = await client.PostAsync("api/admin/canteen", formdata);

            var result = await response.Content.ReadFromJsonAsync<ApiResponseDto>();

            if (!response.IsSuccessStatusCode || result == null || !result.Success)
            {
                await LoadCanteenStaffAsync();

                // Fix 2: Agar API ne koi blank error bheja hai, toh hum apna custom error dikhayenge taaki dabba khali na rahe
                string errorMessage = (result != null && !string.IsNullOrWhiteSpace(result.Message))
                                      ? result.Message
                                      : $"API Request Failed (Status: {(int)response.StatusCode}). Please check your data or ensure the image is attached.";

                ModelState.AddModelError("", errorMessage);
                return View(dto);
            }

            ModelState.Clear();

            // Dropdown list ko dobara load karein taaki page crash na ho
            await LoadCanteenStaffAsync();

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

            // Login cookie se access token lena
            var accessToken = User.FindFirst("AccessToken")?.Value;
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient(ApiClientName);

            // API request create krna 
            var requestUrl = $"api/admin/updateprofiledata/{userId}";
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

            // JWT token API ko bhejna
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Content = JsonContent.Create(new
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                IsActive = model.IsActive
            });
            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                TempData["ProfileSuccess"] = "Profile updated successfully!";
                if(User.Identity is ClaimsIdentity identity)
                {
                    var nameClaimType = identity.NameClaimType;
                    var existingClaim = identity.FindFirst(nameClaimType) ?? identity.FindFirst("FullName");
                    if (existingClaim != null)
                    {
                        identity.RemoveClaim(existingClaim);
                    }

                    identity.AddClaim(new Claim(existingClaim?.Type ?? ClaimTypes.Name, model.FullName));
                    await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, // Ya aapka custom scheme name
            new ClaimsPrincipal(identity)
        );
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
             response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                TempData["ProfileError"] =
                    "Your session may have expired or you are not authorized.";
            }
            else
            {
                TempData["ProfileError"] =
                    "Unable to update profile. Please check your details and try again.";
            }
            return RedirectToAction("UserProfile");
        }



        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserDirectory(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? status = null,
            string? role = null,
            CancellationToken cancellationToken = default)
            
        {

            try {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var accessToken = User.FindFirst("AccessToken")?.Value;

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Access token is missing. Please sign in again."
                    });
                }


                // Build the API query string.
                var query = new Dictionary<string, string?>
                {
                    ["PageNumber"] = Math.Max(1, pageNumber).ToString(),
                    ["PageSize"] = Math.Clamp(pageSize, 1, 50).ToString(),
                    ["SearchTerm"] = searchTerm,
                    ["Status"] = status,
                    ["Role"] = role
                };


                var queryString = string.Join("&",
           query
               .Where(x => !string.IsNullOrWhiteSpace(x.Value))
               .Select(x =>
                   $"{Uri.EscapeDataString(x.Key)}=" +
                   $"{Uri.EscapeDataString(x.Value!)}"));

                var apiUrl = $"api/admin/user-directory?{queryString}";

                // Forward the user's JWT to the protected API.
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, apiUrl);


                request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

                request.Headers.Accept.ParseAdd("application/json");

                using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

                var json = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "The User Directory API request failed."
                    });
                }


                // Forward the API's JSON response to the browser.
                using var document = JsonDocument.Parse(json);

                return Content(
                    document.RootElement.GetRawText(),
                    "application/json");



            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Return a 499 (Client Closed Request) or 400 Bad Request
                return StatusCode(499, new
                {
                    success = false,
                    message = "The request was cancelled by the client."
                });
            }
            catch (JsonException)
            {
                return StatusCode(502, new
                {
                    success = false,
                    message = "The API returned invalid JSON."
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    success = false,
                    message = "Unable to connect to the User Directory API."
                });
            }

        }
        
    
    }

    }
