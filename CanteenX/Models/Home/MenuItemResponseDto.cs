namespace CanteenX.Models.Home
{
    public class MenuItemResponseDto
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public string Price { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int CanteenId { get; set; }
        public string CanteenName { get; set; } = null!;
    }
}
