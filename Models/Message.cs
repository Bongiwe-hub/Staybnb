using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        // User who sent the message
        [Required]
        public string SenderId { get; set; } = string.Empty;

        public ApplicationUser? Sender { get; set; }

        // User who receives the message
        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        public ApplicationUser? Receiver { get; set; }

        // The message content
        [Required]
        public string Content { get; set; } = string.Empty;

        // Date and time the message was sent
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Whether the receiver has read the message
        public bool IsRead { get; set; } = false;
    }
}