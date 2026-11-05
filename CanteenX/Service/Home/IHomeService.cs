using CanteenX.Models.Home;

namespace CanteenX.Service.Home
{
    public interface IHomeService
    {
        Task<IEnumerable<MenuItemResponseDto>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<MenuItemResponseDto>> GetAvailableMenuItemsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CanteenCardDto>> GetActiveCanteensAsync(CancellationToken cancellationToken = default);
    }
}
