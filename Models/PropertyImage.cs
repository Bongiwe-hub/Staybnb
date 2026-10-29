namespace Staybnb.Models
{
    public class PropertyImage
    {
        public int PropertyImageId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public int PropertyId { get; set; }

        public Property? Property { get; set; }
    }
}