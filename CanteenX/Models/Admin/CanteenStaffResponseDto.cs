namespace CanteenX.Models.Admin
{
    public class CanteenStaffResponseDto
    {
        public bool Success { get; set; }
        public List<CanteenStaffDto> Data { get; set; } = new();
    }
}
