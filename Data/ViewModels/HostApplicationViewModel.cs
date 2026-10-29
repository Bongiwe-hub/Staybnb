using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Staybnb.ViewModels
{
    public class HostApplicationViewModel
    {
        [Required]
        [StringLength(150)]
        [Display(Name = "Property Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Property Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        [Display(Name = "Price Per Night")]
        public decimal PricePerNight { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        [Display(Name = "Property Type")]
        public string? PropertyType { get; set; }

        [Required]
        [Display(Name = "Property Image")]
        public IFormFile? PropertyImage { get; set; }
    }
}