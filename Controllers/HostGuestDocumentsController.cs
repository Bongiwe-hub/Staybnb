
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Services;

namespace Staybnb.Controllers
{
    // Hosts can access documents for their own properties.
    // Admins and SuperAdmins can access documents across properties.
    [Authorize(Roles = "Host,Admin,SuperAdmin")]
    public class HostGuestDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ActivityLogger _activityLogger;
        private readonly ILogger<HostGuestDocumentsController> _logger;

        public HostGuestDocumentsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ActivityLogger activityLogger,
            ILogger<HostGuestDocumentsController> logger)
        {
            _context = context;
            _environment = environment;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        // ============================================================
        // VIEW GUEST DOCUMENTS
        // Hosts see documents for their own properties only.
        // Admins and SuperAdmins can see all guest documents.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            bool isHost = User.IsInRole("Host");

            var query = _context.GuestDocuments
                .Include(d => d.Guest)
                .Include(d => d.Booking)
                    .ThenInclude(b => b!.Property)
                .AsQueryable();

            if (isHost)
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                // Never trust a property or host ID supplied by the browser.
                query = query.Where(d =>
                    d.Booking != null &&
                    d.Booking.Property != null &&
                    d.Booking.Property.HostId == userId);
            }

            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View(documents);
        }

        // ============================================================
        // SECURE DOCUMENT DOWNLOAD
        // Checks authorization before reading the private file.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var document = await _context.GuestDocuments
                .Include(d => d.Booking)
                    .ThenInclude(b => b!.Property)
                .FirstOrDefaultAsync(d =>
                    d.GuestDocumentId == id);

            if (document == null ||
                document.Booking == null ||
                document.Booking.Property == null)
            {
                return NotFound();
            }

            // A Host may download only documents for their own property.
            if (User.IsInRole("Host"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId) ||
                    document.Booking.Property.HostId != userId)
                {
                    return NotFound();
                }
            }

            // Files must remain in the fixed private upload directory.
            // Use only the stored filename, never a supplied file path.
            string storedFileName =
                Path.GetFileName(document.FilePath);

            if (string.IsNullOrWhiteSpace(storedFileName) ||
                storedFileName != document.FilePath.Replace('\\', '/')
                    .Split('/')
                    .Last())
            {
                return NotFound();
            }

            string privateFolder = Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    "PrivateUploads",
                    "GuestDocuments"));

            string fullPath = Path.GetFullPath(
                Path.Combine(privateFolder, storedFileName));

            // Prevent path traversal outside the private folder.
            string expectedPrefix =
                privateFolder + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    expectedPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning(
                    "Guest document file is missing for document ID {DocumentId}.",
                    id);

                return NotFound();
            }

            string extension =
                Path.GetExtension(storedFileName).ToLowerInvariant();

            string contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Do not log the document's contents or personal details.
            await _activityLogger.LogAsync(
                User,
                "Guest Document Downloaded",
                $"Document ID {document.GuestDocumentId} downloaded.");

            // Force a download instead of displaying the file inline.
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(
                fullPath,
                contentType,
                Path.GetFileName(document.FileName));
        }

        // ============================================================
        // VERIFY A GUEST DOCUMENT
        // Only a Host who owns the associated property, or an Admin
        // or SuperAdmin, may verify the document.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var document = await _context.GuestDocuments
                .Include(d => d.Booking)
                    .ThenInclude(b => b!.Property)
                .FirstOrDefaultAsync(d =>
                    d.GuestDocumentId == id);

            if (document == null ||
                document.Booking == null ||
                document.Booking.Property == null)
            {
                return NotFound();
            }

            // Enforce property ownership for Hosts.
            if (User.IsInRole("Host"))
            {
                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId) ||
                    document.Booking.Property.HostId != userId)
                {
                    return NotFound();
                }
            }

            if (document.IsVerified)
            {
                TempData["DocumentMessage"] =
                    "This document has already been verified.";

                return RedirectToAction(nameof(Index));
            }

            document.IsVerified = true;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                User,
                "Guest Document Verified",
                $"Document ID {document.GuestDocumentId} verified.");

            TempData["DocumentMessage"] =
                "Guest document verified successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
