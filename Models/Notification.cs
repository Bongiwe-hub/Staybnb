using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        // User who should receive the notification
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // Notification heading
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // Notification message
        [Required]
        public string Message { get; set; } = string.Empty;

        // Date and time the notification was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Whether the user has viewed the notification
        public bool IsRead { get; set; } = false;
    }
}