
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Controllers
{
    // Only Admin and SuperAdmin users may access this controller.
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.HostApplications
                .Include(application => application.Applicant)
                .OrderByDescending(application => application.SubmittedAt)
                .ToListAsync();

            return View(applications);
        }

        // ============================================================
        // APPROVE A HOST APPLICATION
        // Assigns Host role, creates the property, copies images,
        // updates the application and records the administrative action.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.HostApplications
                .Include(a => a.Applicant)
                .FirstOrDefaultAsync(a => a.HostApplicationId == id);

            if (application == null)
            {
                TempData["Error"] = "Host application was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This application has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            if (application.Applicant == null)
            {
                TempData["Error"] =
                    "The applicant account could not be found.";

                return RedirectToAction(nameof(Index));
            }

            var applicant = application.Applicant;

            var applicationImages = await _context.HostApplicationImages
                .Where(image => image.HostApplicationId == id)
                .ToListAsync();

            var originalRoles = await _userManager.GetRolesAsync(applicant);
            bool addedHostRole = false;
            bool removedGuestRole = false;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Assign the Host role if the applicant does not have it.
                if (!originalRoles.Contains("Host"))
                {
                    var addHostResult =
                        await _userManager.AddToRoleAsync(applicant, "Host");

                    if (!addHostResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Host role assignment failed.");
                    }

                    addedHostRole = true;
                }

                // Remove the Guest role if the applicant currently has it.
                if (originalRoles.Contains("Guest"))
                {
                    var removeGuestResult =
                        await _userManager.RemoveFromRoleAsync(applicant, "Guest");

                    if (!removeGuestResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Guest role removal failed.");
                    }

                    removedGuestRole = true;
                }

                // Create the property from the approved application.
                var property = new Property
                {
                    Title = application.Title,
                    Description = application.Description,
                    PricePerNight = application.PricePerNight,
                    Address = application.Address,
                    City = application.City,
                    PropertyType = application.PropertyType,
                    HostId = applicant.Id,
                    IsActive = true
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                // Copy submitted images to the newly created property.
                foreach (var image in applicationImages)
                {
                    var propertyImage = new PropertyImage
                    {
                        PropertyId = property.PropertyId,
                        ImageUrl = image.ImageUrl
                    };

                    _context.PropertyImages.Add(propertyImage);
                }

                // Mark the application as approved.
                application.Status = "Approved";
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedById =
                    _userManager.GetUserId(User);
                application.ReviewComment = "Application approved.";

                await _context.SaveChangesAsync();

                // Commit the application and property changes.
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Restore the applicant's original roles if necessary.
                if (removedGuestRole)
                {
                    var restoreGuestResult =
                        await _userManager.AddToRoleAsync(applicant, "Guest");

                    if (!restoreGuestResult.Succeeded)
                    {
                        _logger.LogError(
                            "Could not restore Guest role for applicant {UserId}.",
                            applicant.Id);
                    }
                }

                if (addedHostRole)
                {
                    var restoreHostResult =
                        await _userManager.RemoveFromRoleAsync(applicant, "Host");

                    if (!restoreHostResult.Succeeded)
                    {
                        _logger.LogError(
                            "Could not remove Host role from applicant {UserId}.",
                            applicant.Id);
                    }
                }

                // Keep technical details in server logs, not in the UI.
                _logger.LogError(
                    ex,
                    "Failed to approve host application {ApplicationId}.",
                    id);

                TempData["Error"] =
                    "The application could not be approved. Please try again.";

                return RedirectToAction(nameof(Index));
            }

            // Record the approval only after the database transaction succeeds.
            try
            {
                await _activityLogger.LogAsync(
                    User,
                    "Host Application Approved",
                    $"Host application ID {application.HostApplicationId} " +
                    $"was approved for applicant ID {applicant.Id}.");
            }
            catch (Exception ex)
            {
                // Do not undo an approval that has already been committed.
                _logger.LogError(
                    ex,
                    "Could not audit approval of host application {ApplicationId}.",
                    id);
            }

            TempData["Success"] =
                "Host application approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // REJECT A HOST APPLICATION
        // Updates the application, records the reviewer and comment,
        // and writes an audit entry.
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? reviewComment)
        {
            var application = await _context.HostApplications
                .FirstOrDefaultAsync(a => a.HostApplicationId == id);

            if (application == null)
            {
                TempData["Error"] = "Host application was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This application has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            application.Status = "Rejected";
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedById =
                _userManager.GetUserId(User);

            // Limit the length of the review comment.
            if (!string.IsNullOrWhiteSpace(reviewComment))
            {
                reviewComment = reviewComment.Trim();

                if (reviewComment.Length > 1000)
                {
                    ModelState.AddModelError(
                        nameof(reviewComment),
                        "The review comment cannot exceed 1,000 characters.");

                    TempData["Error"] =
                        "The review comment is too long.";

                    return RedirectToAction(nameof(Index));
                }
            }

            application.ReviewComment =
                string.IsNullOrWhiteSpace(reviewComment)
                    ? "Application rejected."
                    : reviewComment;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reject host application {ApplicationId}.",
                    id);

                TempData["Error"] =
                    "The application could not be rejected. Please try again.";

                return RedirectToAction(nameof(Index));
            }

            // Record the rejection after the application update succeeds.
            try
            {
                await _activityLogger.LogAsync(
                    User,
                    "Host Application Rejected",
                    $"Host application ID {application.HostApplicationId} " +
                    $"was rejected. Applicant ID: {application.ApplicantId}.");
            }
            catch (Exception ex)
            {
                // Keep audit failures in server logs.
                _logger.LogError(
                    ex,
                    "Could not audit rejection of host application {ApplicationId}.",
                    id);
            }

            TempData["Success"] =
                "Host application rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
