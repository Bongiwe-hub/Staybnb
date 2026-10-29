using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "Guest")]
    public class HostApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public HostApplicationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Apply()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            HostApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PropertyImage == null ||
                model.PropertyImage.Length == 0)
            {
                ModelState.AddModelError(
                    "PropertyImage",
                    "At least one property image is required.");

                return View(model);
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(
                    model.PropertyImage.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "PropertyImage",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");

                return View(model);
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (model.PropertyImage.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "PropertyImage",
                    "The image must be smaller than 5 MB.");

                return View(model);
            }

            var userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var hostApplication = new HostApplication
            {
                ApplicantId = userId,
                Title = model.Title,
                Description = model.Description,
                PricePerNight = model.PricePerNight,
                Address = model.Address,
                City = model.City,
                PropertyType = model.PropertyType,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.HostApplications.Add(hostApplication);

            await _context.SaveChangesAsync();

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "host-applications");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName =
                Guid.NewGuid().ToString()
                + extension;

            var filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.PropertyImage.CopyToAsync(stream);
            }

            var image = new HostApplicationImage
            {
                HostApplicationId =
                    hostApplication.HostApplicationId,

                ImageUrl =
                    "/uploads/host-applications/"
                    + uniqueFileName
            };

            _context.HostApplicationImages.Add(image);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your Host application has been submitted successfully and is awaiting review.";

            return RedirectToAction(
                nameof(Apply));
        }
    }
}