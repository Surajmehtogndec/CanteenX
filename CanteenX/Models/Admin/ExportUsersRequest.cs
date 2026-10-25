using System.ComponentModel.DataAnnotations;

namespace CanteenX.Models.Admin
{
    public sealed class ExportUsersRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public List<string> UserIds { get; set; } = [];
    }
}
