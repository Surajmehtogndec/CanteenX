using System.ComponentModel.DataAnnotations;
namespace CanteenX.Models.CanteenOwner
{
    public class AddMenuItems
    {
        public int CanteenId { get; set; }

        [Required(ErrorMessage = "Item Name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Item Name must be between 2 and 150 characters.")]
        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
      
        public string Price { get; set; } = string.Empty;

        [Required(ErrorMessage = "image is required.")]
        public IFormFile? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }

    }
}
