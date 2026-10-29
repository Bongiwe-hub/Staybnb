
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;

namespace Staybnb.Controllers
{
    // Only Admin and SuperAdmin users can access this controller.
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ActivityLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Receive the database context through dependency injection.
        public ActivityLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display activity records, newest first.
        public async Task<IActionResult> Index()
        {
            var activities = await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(activities);
        }
    }
}
