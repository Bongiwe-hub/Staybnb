
using System.Collections.Generic;
using Staybnb.Models;

namespace Staybnb.ViewModels
{
    public class PropertyDetailsViewModel
    {
        // Stores the property information.
        public Property Property { get; set; } = null!;

        // Stores the property's main image URL.
        public string? PropertyImageUrl { get; set; }

        // Stores all reviews for this property.
        public List<Review> Reviews { get; set; } = new();

        // Stores the average rating from 1 to 5.
        public double AverageRating { get; set; }

        // Stores the total number of reviews.
        public int ReviewCount { get; set; }
    }
}
