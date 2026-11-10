using CanteenX.Models.CanteenOwner;
using CanteenX.Models.Auth;
namespace CanteenX.Service.CanteenOwner
{
    public interface ICanteenOwnerService
    {
        Task<int> GetCanteenIdByUserIdAsync(string userId, string token);
        Task<ApiResponseDto?> AddMenuItemAsync(AddMenuItems dto, CancellationToken cancellationToken);
        Task RevokeTokenAsync(string refreshToken);
        Task<CanteenProfileDto?> GetCanteenProfileDataAsync(string userId, string token CancellationToken cancellationToken);

    }
}
