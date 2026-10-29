using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class GuestDocument
    {
        public int GuestDocumentId { get; set; }

        // The booking associated with this document
        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }

        // The guest who uploaded the document
        [Required]
        public string GuestId { get; set; } = string.Empty;

        public ApplicationUser? Guest { get; set; }

        // Name of the uploaded document
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        // Location where the uploaded document is stored
        [Required]
        public string FilePath { get; set; } = string.Empty;

        // Date and time the document was uploaded
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Whether the document has been verified
        public bool IsVerified { get; set; } = false;
    }
}
