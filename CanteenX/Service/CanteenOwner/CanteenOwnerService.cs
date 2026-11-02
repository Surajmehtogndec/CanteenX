using CanteenX.Models.Auth;
using CanteenX.Models.CanteenOwner;
using CanteenX.Services;
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

     public async Task<ApiResponseDto?> AddMenuItemAsync(AddMenuItems dto, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var formdata = new MultipartFormDataContent();

        }

    }
}
