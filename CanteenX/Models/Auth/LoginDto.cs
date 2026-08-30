using System.ComponentModel.DataAnnotations;

namespace CanteenX.Models.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email or phone number is required.")]
        public string EmailOrPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
