using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class CheckInProcess
    {
        public int CheckInProcessId { get; set; }

        // The booking this check-in process belongs to
        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }

        // Instructions provided by the Host
        [Required]
        public string Instructions { get; set; } = string.Empty;

        // Optional access code for the property
        [StringLength(100)]
        public string? AccessCode { get; set; }

        // Indicates whether the guest has completed check-in
        public bool IsCompleted { get; set; } = false;

        // Date and time the check-in was completed
        public DateTime? CompletedAt { get; set; }
    }
}