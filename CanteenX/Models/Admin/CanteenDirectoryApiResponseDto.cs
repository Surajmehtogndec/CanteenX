namespace CanteenX.Models.Admin
{
    public sealed class CanteenDirectoryApiResponseDto
    {
     public bool Success { get; set; }
     public string? Message { get; set; }

        public CanteenDirectoryApiResponseDto? Data { get; set; }
    }

    public sealed class CanteenDirectoryResponseDto
    {
        public List<CanteenDirectoryItemDto> Items { get; set; } = [];

        public int PageNumber { get; set; }
        

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }

    public sealed class CanteenDirectoryItemDto
    {
        public int CanteenId { get; set; }
        public string CanteenName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
