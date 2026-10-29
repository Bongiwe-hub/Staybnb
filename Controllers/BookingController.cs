
using System;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            ApplicationDbContext context,
            ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // CREATE BOOKING FORM
        // GET: /Booking/Create?propertyId=1
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create(int? propertyId)
        {
            if (propertyId == null || propertyId <= 0)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == propertyId.Value &&
                    p.IsActive);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // ============================================================
        // CONFIRM BOOKING
        // POST: /Booking/Confirm
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            int PropertyId,
            DateTime CheckIn,
            DateTime CheckOut,
            int Guests)
        {
            // Get the ID of the currently authenticated Guest.
            string? guestId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(guestId))
            {
                return Challenge();
            }

            // Find the property and ensure it is active.
            var property = await _context.Properties
                .FirstOrDefaultAsync(p =>
                    p.PropertyId == PropertyId &&
                    p.IsActive);

            if (property == null)
            {
                return NotFound();
            }

            // --------------------------------------------------------
            // 1. VALIDATE THE BOOKING DATES
            // --------------------------------------------------------

            if (CheckIn == default ||
                CheckIn.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(CheckIn),
                    "Check-in cannot be in the past.");
            }

            if (CheckOut == default ||
                CheckIn == default ||
                CheckOut.Date <= CheckIn.Date)
            {
                ModelState.AddModelError(
                    nameof(CheckOut),
                    "Check-out must be after check-in.");
            }

            // --------------------------------------------------------
            // 2. VALIDATE THE NUMBER OF GUESTS
            // --------------------------------------------------------

            if (Guests < 1 || Guests > 50)
            {
                ModelState.AddModelError(
                    nameof(Guests),
                    "The number of guests must be between 1 and 50.");
            }

            // --------------------------------------------------------
            // 3. VALIDATE THE PROPERTY PRICE
            // --------------------------------------------------------

            if (property.PricePerNight <= 0 ||
                property.PricePerNight > 1000000)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This property does not have a valid nightly price.");
            }

            // If validation fails, return to the form.
            if (!ModelState.IsValid)
            {
                ViewBag.CheckIn = CheckIn == default
                    ? string.Empty
                    : CheckIn.ToString("yyyy-MM-dd");

                ViewBag.CheckOut = CheckOut == default
                    ? string.Empty
                    : CheckOut.ToString("yyyy-MM-dd");

                ViewBag.Guests = Guests;

                return View("Create", property);
            }

            // --------------------------------------------------------
            // 4. CALCULATE THE BOOKING TOTAL
            // --------------------------------------------------------

            int nights = (CheckOut.Date - CheckIn.Date).Days;

            // Preserve the existing fee policy.
            decimal cleaningFee = 0m;
            decimal serviceFee = 0m;

            decimal totalPrice =
                (nights * property.PricePerNight)
                + cleaningFee
                + serviceFee;

            if (totalPrice <= 0 ||
                totalPrice > 10000000)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The calculated booking total is outside the allowed range.");

                ViewBag.CheckIn = CheckIn.ToString("yyyy-MM-dd");
                ViewBag.CheckOut = CheckOut.ToString("yyyy-MM-dd");
                ViewBag.Guests = Guests;

                return View("Create", property);
            }

            // --------------------------------------------------------
            // 5. CHECK AVAILABILITY AND SAVE ATOMICALLY
            // --------------------------------------------------------

            // Serializable isolation helps protect the availability
            // check from concurrent booking requests. Database indexes,
            // SQL Server locking and retry handling may also be needed
            // for a complete production concurrency strategy.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                // Recheck for overlapping Pending or Approved bookings
                // inside the transaction, immediately before saving.
                bool hasConflict = await _context.Bookings
                    .AnyAsync(b =>
                        b.PropertyId == property.PropertyId &&
                        (b.Status == "Pending" ||
                         b.Status == "Approved") &&
                        b.CheckInDate < CheckOut.Date &&
                        b.CheckOutDate > CheckIn.Date);

                if (hasConflict)
                {
                    await transaction.RollbackAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "This property already has a pending or approved " +
                        "booking for some or all of these dates. " +
                        "Please choose different dates.");

                    ViewBag.CheckIn = CheckIn.ToString("yyyy-MM-dd");
                    ViewBag.CheckOut = CheckOut.ToString("yyyy-MM-dd");
                    ViewBag.Guests = Guests;

                    return View("Create", property);
                }

                // Build the booking using server-side values.
                // Never accept GuestId, price or total from the form.
                var booking = new Booking
                {
                    GuestId = guestId,
                    PropertyId = property.PropertyId,
                    CheckInDate = CheckIn.Date,
                    CheckOutDate = CheckOut.Date,
                    Guests = Guests,
                    Nights = nights,
                    PricePerNight = property.PricePerNight,
                    CleaningFee = cleaningFee,
                    ServiceFee = serviceFee,
                    TotalPrice = totalPrice,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ----------------------------------------------------
                // 6. PREPARE THE CONFIRMATION VIEW
                // ----------------------------------------------------

                ViewBag.BookingId = booking.BookingId;
                ViewBag.PropertyTitle = property.Title;

                ViewBag.CheckIn =
                    booking.CheckInDate.ToString("dd MMMM yyyy");

                ViewBag.CheckOut =
                    booking.CheckOutDate.ToString("dd MMMM yyyy");

                ViewBag.Guests = booking.Guests;
                ViewBag.Nights = booking.Nights;
                ViewBag.TotalPrice = booking.TotalPrice;
                ViewBag.Status = booking.Status;

                return View("Confirmation");
            }
            catch (Exception ex)
            {
                // Roll back if saving or committing fails.
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackException)
                {
                    _logger.LogError(
                        rollbackException,
                        "Rollback failed while creating a booking for property {PropertyId}.",
                        property.PropertyId);
                }

                // Record technical details in server logs only.
                _logger.LogError(
                    ex,
                    "Booking creation failed for property {PropertyId} and guest {GuestId}.",
                    property.PropertyId,
                    guestId);

                TempData["BookingMessage"] =
                    "We could not complete your booking. " +
                    "Please try again.";

                return RedirectToAction(
                    nameof(Create),
                    new { propertyId = property.PropertyId });
            }
        }

        // ============================================================
        // VIEW MY BOOKINGS
        // GET: /Booking/MyBookings
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            string? guestId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(guestId))
            {
                return Challenge();
            }

            // Return only bookings owned by the current Guest.
            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Identify bookings that already have reviews.
            var reviewedBookingIds = await _context.Reviews
                .Where(r => r.GuestId == guestId)
                .Select(r => r.BookingId)
                .ToListAsync();

            ViewBag.ReviewedBookingIds = reviewedBookingIds;

            return View(bookings);
        }

        // ============================================================
        // CANCEL MY PENDING BOOKING
        // POST: /Booking/Cancel/1
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            string? guestId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(guestId))
            {
                return Challenge();
            }

            // Match both the booking ID and the logged-in Guest ID.
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.GuestId == guestId);

            if (booking == null)
            {
                return NotFound();
            }

            // Guests may cancel only Pending bookings.
            if (booking.Status != "Pending")
            {
                TempData["BookingMessage"] =
                    "Only pending bookings can be cancelled.";

                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["BookingMessage"] =
                "Your booking has been cancelled successfully.";

            return RedirectToAction(nameof(MyBookings));
        }

        // ============================================================
        // VIEW MY CHECK-IN INSTRUCTIONS
        // GET: /Booking/MyCheckInInstructions
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> MyCheckInInstructions()
        {
            string? guestId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(guestId))
            {
                return Challenge();
            }

            // Only this Guest's approved bookings are retrieved.
            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Where(b =>
                    b.GuestId == guestId &&
                    b.Status == "Approved")
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

            var bookingIds = bookings
                .Select(b => b.BookingId)
                .ToList();

            var processes = await _context.CheckInProcesses
                .Where(c => bookingIds.Contains(c.BookingId))
                .ToListAsync();

            // Avoid duplicate dictionary keys if multiple records exist.
            var processesByBooking = processes
                .GroupBy(c => c.BookingId)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

            ViewBag.CheckInProcesses = processesByBooking;

            return View("CheckInInstructions", bookings);
        }
    }
}
