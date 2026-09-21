using System.ComponentModel.DataAnnotations;
namespace CanteenX.Models.Admin
{
    public class AddCanteens
    {
        [Required(ErrorMessage = "Please select a canteen .")]
        public string UserId { get; set; } = string.Empty;

       
        public string CanteenName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Location is required.")]
        [StringLength(250, MinimumLength = 3, ErrorMessage = "Location must be between 3 and 250 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])[a-zA-Z0-9\s,.'\-]+$", ErrorMessage = "Location must contain letters. It cannot be just numbers or symbols.")]
        public string? Location { get; set; }


        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters long.")]
        // Description mein paragraph aur normal punctuation allow karega, par `<` aur `>` ko block kar dega taaki koi script na likh sake.
        [RegularExpression(@"^(?=.*[a-zA-Z])[^<>]+$", ErrorMessage = "Description must contain letters and cannot contain HTML tags (<, >).")]
        public string? Description { get; set; }

       
        public IFormFile? ImageUrl { get; set; }

        public bool IsActive { get; set; } 

    }
}
