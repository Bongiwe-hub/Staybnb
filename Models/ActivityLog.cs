using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class ActivityLog
    {
        public int ActivityLogId { get; set; }

        // User who performed the action
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        // Description of the action
        [Required]
        [StringLength(500)]
        public string Action { get; set; } = string.Empty;

        // Additional information about the action
        public string? Details { get; set; }

        // Date and time when the action occurred
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // IP address from which the action was performed
        [StringLength(45)]
        public string? IpAddress { get; set; }
    }
}