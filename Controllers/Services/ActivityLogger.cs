
using System.Security.Claims;
using Staybnb.Data;
using Staybnb.Models;
using Microsoft.EntityFrameworkCore;

namespace Staybnb.Services
{
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogger(ApplicationDbContext context)
        {
            _context = context;
        }
      

      public async Task LogAsync(
    ClaimsPrincipal user,
    string action,
    string? details = null,
    string? ipAddress = null)
{
    Console.WriteLine("STEP 1: ActivityLogger was called.");

    try
    {
        // Show which database the application is using.
        Console.WriteLine(
            $"Database: {_context.Database.GetDbConnection().Database}");

        Console.WriteLine(
            $"Server: {_context.Database.GetDbConnection().DataSource}");

        // Create the activity record.
        var activity = new ActivityLog
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        // Add the record to Entity Framework's tracking.
        _context.ActivityLogs.Add(activity);

        Console.WriteLine("Activity added. Attempting to save...");

        // Save the record to SQL Server.
        int rowsAffected = await _context.SaveChangesAsync();

        Console.WriteLine($"Rows affected: {rowsAffected}");
        Console.WriteLine($"Saved ActivityLogId: {activity.ActivityLogId}");
    }
    catch (Exception ex)
    {
        // Display the actual database error if saving fails.
        Console.WriteLine($"ACTIVITY LOG ERROR: {ex}");
        throw;
    }
}
        
    }
}
