using CanteenX.Models.Home;

namespace CanteenX.Service.Home
{
    public interface IHomeService
    {
        Task<IEnumerable<CanteenCardDto>> GetActiveCanteensAsync(CancellationToken cancellationToken = default);
    }
}
