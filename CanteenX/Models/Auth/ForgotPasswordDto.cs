using System.ComponentModel.DataAnnotations;

namespace CanteenX.Models.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100,ErrorMessage ="Email cannot be longer than 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|yahoo\.com|outlook\.com)$",
         ErrorMessage = "Only Gmail, Yahoo, or Outlook addresses are allowed.")]
        public string Email { get; set; } = string.Empty;
    }
}
