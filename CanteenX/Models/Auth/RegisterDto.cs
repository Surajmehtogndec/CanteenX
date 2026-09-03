using System.ComponentModel.DataAnnotations;

namespace CanteenX.Models.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(50, ErrorMessage = "Full name must be between 2 and 50 characters.")]
        [RegularExpression(
            @"^[A-Za-z ]+$",
            ErrorMessage = "Full name can contain only letters and spaces."
        )]
        public string FullName { get; set; } = string.Empty; 


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(12, ErrorMessage = "Phone number must be between 10 and 12 characters.")]
        [RegularExpression(
            @"^[6-9][0-9]{9}$",
            ErrorMessage = "Please enter a valid 10-digit Indian mobile number."
        )]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(
           100,
           MinimumLength = 8,
           ErrorMessage = "Password must be at least 8 characters."
       )]
        [RegularExpression(
           @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$",
           ErrorMessage =
               "Password must contain uppercase, lowercase, number and special character."
       )]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(
            "Password",
            ErrorMessage = "Passwords do not match."
        )]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
