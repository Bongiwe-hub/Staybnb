using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        // Guest who wrote the review
        [Required]
        public string GuestId { get; set; } = string.Empty;

        public ApplicationUser? Guest { get; set; }

        // Property being reviewed
        [Required]
        public int PropertyId { get; set; }

        public Property? Property { get; set; }

        // Booking associated with the review
        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }

        // Rating from 1 to 5
        [Range(1, 5)]
        public int Rating { get; set; }

        // Optional written review
        [StringLength(2000)]
        public string? Comment { get; set; }

        // Date and time the review was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}