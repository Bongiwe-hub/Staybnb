using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class HostApplicationImage
    {
        public int HostApplicationImageId { get; set; }

        [Required]
        public int HostApplicationId { get; set; }

        public HostApplication? HostApplication { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
    }
}