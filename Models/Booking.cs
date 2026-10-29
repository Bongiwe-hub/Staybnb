using System;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        // Guest who made the booking.
        [Required]
        public string GuestId { get; set; } = string.Empty;

        public ApplicationUser? Guest { get; set; }

        // Property being booked.
        [Required]
        public int PropertyId { get; set; }

        public Property? Property { get; set; }

        // Booking dates.
        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        // Number of guests included in the booking.
        [Range(1, 50)]
        public int Guests { get; set; } = 1;

        // Number of nights.
        public int Nights { get; set; }

        // Price per night at the time of booking.
        [Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        // Additional booking charges.
        [Range(0, 1000000)]
        public decimal CleaningFee { get; set; }

        [Range(0, 1000000)]
        public decimal ServiceFee { get; set; }

        // Final booking amount.
        [Range(0, 10000000)]
        public decimal TotalPrice { get; set; }

        // Examples: Pending, Approved, Rejected, Cancelled.
        [Required]
        public string Status { get; set; } = "Pending";

        // Date the booking was created.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Date the booking was approved or rejected.
        public DateTime? ReviewedAt { get; set; }

        // Optional comment from the host.
        public string? HostComment { get; set; }
    }
}
