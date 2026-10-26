using CanteenX.Models.Admin;
using CanteenX.Models.Auth;

namespace CanteenX.Service.Interface
{
    public interface IAdminService
    {
        // PDF Generation
        byte[] GenerateUsersPdf(List<ExportPdfDto> users);

        // API Calls
        Task<UserProfileDto?> GetProfileDataAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<CanteenStaffDto>> GetCanteenStaffAsync();
        Task<AuthResponseDto?> RegisterCanteenStaffAsync(RegisterCanteensDto model);
        Task RevokeTokenAsync(string refreshToken);
        Task<ApiResponseDto?> AddCanteenAsync(AddCanteens dto);
        Task<HttpResponseMessage> UpdateProfileAsync(string userId, UserProfileDto model, string token);
        Task<HttpResponseMessage> GetUserDirectoryAsync(string queryString, string token, CancellationToken cancellationToken);
        Task<HttpResponseMessage> UpdateUserAsync(string id, EditUserViewModel model, string token, CancellationToken cancellationToken);
        Task<HttpResponseMessage> DeleteUserAsync(string id, string token, CancellationToken cancellationToken);

    }
}
