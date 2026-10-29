using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Property
    {
        public int PropertyId { get; set; }

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

        public bool IsActive { get; set; } = true;

        public string? HostId { get; set; }

        public ApplicationUser? Host { get; set; }
    }
}