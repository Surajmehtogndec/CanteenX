namespace CanteenX.Models.Admin
{
    public class ApiAdminResponseDto<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        // Data ka type 'T' hoga taaki yeh dynamically koi bhi DTO accept kar sake
        public T? Data { get; set; }
    }
}
