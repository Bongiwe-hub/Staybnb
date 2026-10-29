using Microsoft.AspNetCore.Identity;

namespace Staybnb.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
// Holds the surname of the user
        public string LastName { get; set; } = string.Empty;
//Stores the path to the user's profile picture & '?' means it allowed to be left empty
        public string? ProfilePictureUrl { get; set; }
//Keeps track of when the account was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}