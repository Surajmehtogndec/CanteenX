using CanteenX.Models.Admin;
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

        public IActionResult RegisterCanteens()
        {
            return View();
        }

        public IActionResult RegisterCanteenLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCanteenLogin( RegisterCanteensDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("RegisterCanteens", model);
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
                return View("RegisterCanteens", model);
            }
            ModelState.Clear();

            // Success message set karein
            ViewBag.SuccessMessage = "Canteen staff account created successfully!";

            return View("RegisterCanteens", new RegisterCanteensDto());
        }


       

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
                );
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
                _httpClientFactory.CreateClient("CanteenX.Api");

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCanteens(AddCanteens dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadCanteenStaffAsync();
                return View(dto); 
            }

            var client = _httpClientFactory.CreateClient("CanteenX.Api");

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
                "ImageUrl",dto.ImageUrl.FileName);
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


    }

    }
