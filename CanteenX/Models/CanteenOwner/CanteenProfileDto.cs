namespace CanteenX.Models.CanteenOwner
{
    public class CanteenProfileDto
    {
        public int CanteenId { get; set; }
        public string Id { get; set; } = string.Empty;
        public string CanteenName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; }

    }
}
