using System.ComponentModel.DataAnnotations;
namespace CanteenX.Models.Admin
{
    public class AddCanteens
    {
        [Required(ErrorMessage = "Canteen Name is required.")]
        [StringLength(150, ErrorMessage = "Canteen Name cannot exceed 150 characters.")]

        public string CanteenName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(250, ErrorMessage = "Location cannot exceed 250 characters.")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Image URL is required.")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

    }
}
