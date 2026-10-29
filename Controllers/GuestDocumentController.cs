
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

[Authorize]
public class GuestDocumentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public GuestDocumentController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // Display documents belonging to the current guest.
    [HttpGet]
    public async Task<IActionResult> MyDocuments()
    {
        string? guestId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(guestId))
        {
            return Challenge();
        }

        var documents = await _context.GuestDocuments
            .Include(d => d.Booking)
                .ThenInclude(b => b!.Property)
            .Where(d => d.GuestId == guestId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return View(documents);
    }

    // Upload a document for one of the current guest's approved bookings.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        int bookingId,
        IFormFile? document)
    {
        string? guestId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(guestId))
        {
            return Challenge();
        }

        if (document == null || document.Length == 0)
        {
            TempData["Error"] = "Please select a document to upload.";
            return RedirectToAction(
                "MyCheckInInstructions", "Booking");
        }

        // Only allow uploads for bookings owned by this guest.
        var booking = await _context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b =>
                b.BookingId == bookingId &&
                b.GuestId == guestId &&
                b.Status == "Approved");

        if (booking == null)
        {
            return Forbid();
        }

        // Accept only PDF or image files, with a 5 MB limit.
        string extension =
            Path.GetExtension(document.FileName).ToLowerInvariant();

        string[] allowedExtensions =
            { ".pdf", ".jpg", ".jpeg", ".png" };

        if (!allowedExtensions.Contains(extension))
        {
            TempData["Error"] =
                "Only PDF, JPG, JPEG, and PNG files are allowed.";

            return RedirectToAction(
                "MyCheckInInstructions", "Booking");
        }

        if (document.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] =
                "The document must be no larger than 5 MB.";

            return RedirectToAction(
                "MyCheckInInstructions", "Booking");
        }

        // Create a unique file name instead of trusting the original name.
        string uploadFolder = Path.Combine(
            _environment.ContentRootPath,
            "PrivateUploads",
            "GuestDocuments");

        Directory.CreateDirectory(uploadFolder);

        string savedFileName =
            $"{Guid.NewGuid()}{extension}";

        string fullPath =
            Path.Combine(uploadFolder, savedFileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await document.CopyToAsync(stream);
        }

        // Save the document record in the database.
        var guestDocument = new GuestDocument
        {
            BookingId = booking.BookingId,
            GuestId = guestId,
            FileName = Path.GetFileName(document.FileName),
            FilePath = Path.Combine(
                "PrivateUploads",
                "GuestDocuments",
                savedFileName),
            UploadedAt = DateTime.UtcNow,
            IsVerified = false
        };

        _context.GuestDocuments.Add(guestDocument);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Your document was uploaded and is awaiting host verification.";

        return RedirectToAction(
            "MyCheckInInstructions", "Booking");
    }
}
