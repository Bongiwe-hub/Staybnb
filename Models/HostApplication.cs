using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class HostApplication
    {
        public int HostApplicationId { get; set; }

        // User who submitted the application
        [Required]
        public string ApplicantId { get; set; } = string.Empty;

        public ApplicationUser? Applicant { get; set; }

        // Property information supplied in the application
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        public decimal PricePerNight { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? PropertyType { get; set; }

        // Stores the application status.
        // Examples: Pending, Approved, Rejected
        [Required]
        public string Status { get; set; } = "Pending";

        // Date and time when the application was submitted
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Date and time when an administrator reviewed the application
        public DateTime? ReviewedAt { get; set; }

        // Administrator who reviewed the application
        public string? ReviewedById { get; set; }

        public ApplicationUser? ReviewedBy { get; set; }

        // Optional explanation when an application is rejected
        public string? ReviewComment { get; set; }
    }
}