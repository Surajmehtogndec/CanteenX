namespace CanteenX.Models.Auth
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public UserInfoDto User { get; set; } = new();
    }
}
