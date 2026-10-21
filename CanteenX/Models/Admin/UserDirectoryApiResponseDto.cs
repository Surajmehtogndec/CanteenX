namespace CanteenX.Models.Admin
{
    public sealed class UserDirectoryApiResponseDto
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public UserDirectoryResponseDto? Data { get; set; }
    }
    public sealed class UserDirectoryResponseDto
    {
        public List<UserDirectoryItemDto> Items { get; set; } = [];

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
    public sealed class UserDirectoryItemDto
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

}
