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

        public async Task<(bool IsSuccess, string Message)> LinkBankAccountAsync(CanteenAccountDetailsDto dto, CancellationToken cancellationToken)
        {
           
            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.PostAsJsonAsync("api/payments/link-bank", new
                {
                    dto.UserId,
                    dto.AccountHolderName,
                    dto.AccountNumber,
                    dto.IFSC,
                    dto.BankName

                }, cancellationToken);

                // Response check
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Bank account successfully linked with Razorpay!");
                }

                // Error read karna
                var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("API Error while linking bank: {Error}", errorResponse);

                return (false, $"API Error: {errorResponse}");


            }
            catch (Exception ex)
            {
                // Exception handle karna aur log karna
                _logger.LogError(ex, "Exception in LinkBankAccountAsync for UserId {UserId}", dto.UserId);
                return (false, $"Connection Failed: {ex.Message}");
            }
        }

    }
}
