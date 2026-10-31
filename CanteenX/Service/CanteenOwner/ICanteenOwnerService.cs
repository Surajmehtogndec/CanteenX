using CanteenX.Models.CanteenOwner;

namespace CanteenX.Service.CanteenOwner
{
    public interface ICanteenOwnerService
    {
        Task<(bool IsSuccess, string Message)> LinkBankAccountAsync(CanteenAccountDetailsDto dto, CancellationToken cancellationToken);
        Task RevokeTokenAsync(string refreshToken);
    }
}
