using CanteenX.Models.Auth;
using CanteenX.Models.CanteenOwner;
using CanteenX.Services;
using System.Net.Http.Headers;
using System.Security.Claims;
namespace CanteenX.Service.CanteenOwner
{
    public class CanteenOwnerService : ICanteenOwnerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CanteenOwnerService> _logger;
        private const string ApiClientName = "CanteenX.Api";


        public CanteenOwnerService(IHttpClientFactory httpClientFactory, ILogger<CanteenOwnerService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        public async Task RevokeTokenAsync(string refreshToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            await client.PostAsJsonAsync("api/Auth/revoke-token", new { Token = refreshToken });
        }

        public async Task<int> GetCanteenIdByUserIdAsync(string userId, string token)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            // API call se pehle Token ko header mein attach karein
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            try
            {
                var response = await client.GetAsync("api/CanteenStaff/my-canteen-id");
                if (response.IsSuccessStatusCode)
                {
                    // 2. JSON response ko read karein (jaise { "canteenId": 5 })
                    var responseData = await response.Content.ReadFromJsonAsync<CanteenIdResponse>();

                    if (responseData != null && responseData.CanteenId > 0)
                    {
                      
                        return responseData.CanteenId; // Form CanteenId ke sath load hoga
                    }
                }
                // Agar CanteenId nahi mili toh user ko error dikhayein

                return 0;
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public async Task<ApiResponseDto?> AddMenuItemAsync(AddMenuItems dto, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var formdata = new MultipartFormDataContent();

            formdata.Add(new StringContent(dto.CanteenId.ToString()), "CanteenId");
            formdata.Add(new StringContent(dto.ItemName), "ItemName");
            formdata.Add(new StringContent(dto.Price.ToString()), "Price");
            formdata.Add(new StringContent(dto.IsAvailable.ToString()), "IsAvailable");
            if(dto.ImageUrl != null)
            {
                var imageContent = new StreamContent(dto.ImageUrl.OpenReadStream());
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(dto.ImageUrl.ContentType);
                formdata.Add(imageContent, "ImageUrl", dto.ImageUrl.FileName);
            }

            var response = await client.PostAsync("api/CanteenStaff/add-menu-item", formdata, cancellationToken);
            if(!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ApiResponseDto>();
        }

        public async Task<CanteenProfileDto?> GetCanteenProfileDataAsync(string userId, string token, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            var response = await client.GetAsync($"api/CanteenStaff/canteen-profile", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CanteenProfileDto>(cancellationToken: cancellationToken);
        }

    }
}
