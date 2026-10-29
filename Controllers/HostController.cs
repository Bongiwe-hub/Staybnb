
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Host")]
    public class HostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        public HostController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        // ============================================================
        // HOST DASHBOARD
        // ============================================================

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var properties = await _context.Properties
                .Where(p => p.HostId == userId)
                .ToListAsync();

            return View(properties);
        }

        // ============================================================
        // MY PROPERTIES
        // ============================================================

        public async Task<IActionResult> Properties()
        {
            var userId = _userManager.GetUserId(User);

            var properties = await _context.Properties
                .Where(p => p.HostId == userId)
                .OrderBy(p => p.Title)
                .ToListAsync();

            return View(properties);
        }

        // ============================================================
        // ACTIVATE OR DEACTIVATE A PROPERTY
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePropertyStatus(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Only retrieve a property belonging to this host.
            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == id &&
                    p.HostId == userId);

            if (property == null)
            {
                return NotFound();
            }

            property.IsActive = !property.IsActive;

            await _context.SaveChangesAsync();

            // Log the status change.
            await _activityLogger.LogAsync(
                User,
                "Property Status Changed",
                $"Property ID {property.PropertyId} status changed to " +
                $"{(property.IsActive ? "Active" : "Inactive")}.");

            TempData["SuccessMessage"] = property.IsActive
                ? "Your property is now active."
                : "Your property has been deactivated.";

            return RedirectToAction(nameof(Properties));
        }

        // ============================================================
        // MANAGE BOOKINGS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Property)
                .Where(b => b.Property != null &&
                            b.Property.HostId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ============================================================
        // APPROVE A BOOKING
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.Property != null &&
                    b.Property.HostId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != "Pending")
            {
                TempData["BookingMessage"] =
                    "Only pending bookings can be approved.";

                return RedirectToAction(nameof(Bookings));
            }

            booking.Status = "Approved";
            booking.ReviewedAt = DateTime.UtcNow;
            booking.HostComment = "Booking approved.";

            var notification = new Notification
            {
                UserId = booking.GuestId,
                Title = "Booking Approved",
                Message =
                    $"Your booking for {booking.Property!.Title} has been approved.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Record the booking approval.
            await _activityLogger.LogAsync(
                User,
                "Booking Approved",
                $"Booking ID {booking.BookingId} was approved.");

            TempData["BookingMessage"] =
                "Booking approved successfully.";

            return RedirectToAction(nameof(Bookings));
        }

        // ============================================================
        // REJECT A BOOKING
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(
            int id,
            string? hostComment)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.Property != null &&
                    b.Property.HostId == userId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != "Pending")
            {
                TempData["BookingMessage"] =
                    "Only pending bookings can be rejected.";

                return RedirectToAction(nameof(Bookings));
            }

            booking.Status = "Rejected";
            booking.ReviewedAt = DateTime.UtcNow;

            booking.HostComment =
                string.IsNullOrWhiteSpace(hostComment)
                    ? "Booking rejected."
                    : hostComment.Trim();

            var notification = new Notification
            {
                UserId = booking.GuestId,
                Title = "Booking Rejected",
                Message =
                    $"Your booking for {booking.Property!.Title} has been rejected.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Record the booking rejection.
            await _activityLogger.LogAsync(
                User,
                "Booking Rejected",
                $"Booking ID {booking.BookingId} was rejected.");

            TempData["BookingMessage"] = "Booking rejected.";

            return RedirectToAction(nameof(Bookings));
        }

        // ============================================================
        // CHECK-IN SETTINGS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Where(b => b.Property != null &&
                            b.Property.HostId == userId &&
                            b.Status == "Approved")
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

            var bookingIds = bookings
                .Select(b => b.BookingId)
                .ToList();

            var checkInProcesses = await _context.CheckInProcesses
                .Where(c => bookingIds.Contains(c.BookingId))
                .ToListAsync();

            // Group by booking to prevent duplicate dictionary keys.
            var processesByBooking = checkInProcesses
                .GroupBy(c => c.BookingId)
                .ToDictionary(g => g.Key, g => g.First());

            ViewBag.CheckInProcesses = processesByBooking;

            return View(bookings);
        }

        // ============================================================
        // SAVE CHECK-IN INSTRUCTIONS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCheckIn(
            int bookingId,
            string instructions,
            string? accessCode)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(instructions))
            {
                TempData["CheckInMessage"] =
                    "Please enter check-in instructions.";

                return RedirectToAction(nameof(CheckIn));
            }

            if (instructions.Length > 2000)
            {
                TempData["CheckInMessage"] =
                    "Instructions cannot exceed 2,000 characters.";

                return RedirectToAction(nameof(CheckIn));
            }

            if (accessCode?.Length > 100)
            {
                TempData["CheckInMessage"] =
                    "The access code cannot exceed 100 characters.";

                return RedirectToAction(nameof(CheckIn));
            }

            // Ensure the approved booking belongs to this host.
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.Property != null &&
                    b.Property.HostId == userId &&
                    b.Status == "Approved");

            if (booking == null)
            {
                return NotFound();
            }

            var checkInProcess = await _context.CheckInProcesses
                .FirstOrDefaultAsync(c => c.BookingId == bookingId);

            if (checkInProcess == null)
            {
                checkInProcess = new CheckInProcess
                {
                    BookingId = bookingId,
                    Instructions = instructions.Trim(),
                    AccessCode = string.IsNullOrWhiteSpace(accessCode)
                        ? null
                        : accessCode.Trim(),
                    IsCompleted = false
                };

                _context.CheckInProcesses.Add(checkInProcess);
            }
            else
            {
                checkInProcess.Instructions = instructions.Trim();

                checkInProcess.AccessCode =
                    string.IsNullOrWhiteSpace(accessCode)
                        ? null
                        : accessCode.Trim();
            }

            await _context.SaveChangesAsync();

            // Do not put the access code or instructions in the audit log.
            await _activityLogger.LogAsync(
                User,
                "Check-In Settings Updated",
                $"Check-in settings updated for Booking ID {booking.BookingId}.");

            TempData["CheckInMessage"] =
                "Check-in settings saved successfully.";

            return RedirectToAction(nameof(CheckIn));
        }

        // ============================================================
        // MARK CHECK-IN AS COMPLETED
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteCheckIn(int bookingId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Only update an approved booking owned by this host.
            var booking = await _context.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.Property != null &&
                    b.Property.HostId == userId &&
                    b.Status == "Approved");

            if (booking == null)
            {
                return NotFound();
            }

            var checkInProcess = await _context.CheckInProcesses
                .FirstOrDefaultAsync(c => c.BookingId == bookingId);

            if (checkInProcess == null)
            {
                TempData["CheckInMessage"] =
                    "Please save the check-in instructions first.";

                return RedirectToAction(nameof(CheckIn));
            }

            if (checkInProcess.IsCompleted)
            {
                TempData["CheckInMessage"] =
                    "This guest's check-in is already completed.";

                return RedirectToAction(nameof(CheckIn));
            }

            checkInProcess.IsCompleted = true;
            checkInProcess.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Record the completed check-in.
            await _activityLogger.LogAsync(
                User,
                "Guest Check-In Completed",
                $"Check-in completed for Booking ID {booking.BookingId}.");

            TempData["CheckInMessage"] =
                "Guest check-in marked as completed successfully.";

            return RedirectToAction(nameof(CheckIn));
        }
    }
}
