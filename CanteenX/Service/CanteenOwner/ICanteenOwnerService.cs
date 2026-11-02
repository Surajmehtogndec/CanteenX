using CanteenX.Models.CanteenOwner;
using CanteenX.Models.Auth;
namespace CanteenX.Service.CanteenOwner
{
    public interface ICanteenOwnerService
    {
        Task<ApiResponseDto?> AddMenuItemAsync(AddMenuItems dto,CancellationToken cancellationToken);
        Task RevokeTokenAsync(string refreshToken);
    }
}
