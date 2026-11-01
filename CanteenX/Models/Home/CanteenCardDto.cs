namespace CanteenX.Models.Home
{
    public class CanteenCardDto
    {
        public int CanteenId { get; set; }
        public string CanteenName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CrowdStatus { get; set; } = string.Empty;
        public string EstimatedWaitTime { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
    }
}
