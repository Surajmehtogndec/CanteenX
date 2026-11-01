using CanteenX.Models.Home;
using CanteenX.Service.CanteenOwner;
using System.Text.Json;

namespace CanteenX.Service.Home
{
    public class HomeService : IHomeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeService> _logger;
        private const string ApiClientName = "CanteenX.Api";

        public HomeService (IHttpClientFactory httpClientFactory, ILogger<HomeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<CanteenCardDto>> GetActiveCanteensAsync(CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var response = await client.GetAsync("api/home/active-canteens", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<IEnumerable<CanteenCardDto>>(jsonString, options)
                       ?? Enumerable.Empty<CanteenCardDto>();
            }
            // Agar API fail ho jaye toh empty list bhej de, taaki UI crash na ho
            return Enumerable.Empty<CanteenCardDto>();

        }

    }
}
