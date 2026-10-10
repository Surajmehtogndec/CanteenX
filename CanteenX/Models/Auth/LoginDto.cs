using System.ComponentModel.DataAnnotations;

namespace CanteenX.Models.Auth
{
    public class LoginDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
