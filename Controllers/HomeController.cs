
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    // Connect the controller to the Staybnb database.
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Display the home page with active properties and their photos.
    public async Task<IActionResult> Index()
    {
        // Retrieve only active properties.
        var properties = await _context.Properties
            .Where(property => property.IsActive)
            .OrderBy(property => property.Title)
            .ToListAsync();

        // Get the IDs of the properties being displayed.
        var propertyIds = properties
            .Select(property => property.PropertyId)
            .ToList();

        // Retrieve images linked to these properties.
        var images = await _context.PropertyImages
            .Where(image => propertyIds.Contains(image.PropertyId))
            .ToListAsync();

        // Create a dictionary containing each property's first image URL.
        var propertyImages = images
            .Where(image =>
                !string.IsNullOrWhiteSpace(image.ImageUrl))
            .GroupBy(image => image.PropertyId)
            .ToDictionary(
                group => group.Key,
                group => group.First().ImageUrl
            );

        // Make the image dictionary available to the home page.
        ViewBag.PropertyImages = propertyImages;

        return View(properties);
    }

    // Display details, photos, and reviews for one active property.
    public async Task<IActionResult> Details(int? id)
    {
        // Check whether a property ID was provided.
        if (id == null)
        {
            return NotFound();
        }

        // Find the requested active property.
        var property = await _context.Properties
            .FirstOrDefaultAsync(p =>
                p.PropertyId == id && p.IsActive);

        // Stop if the property does not exist or is inactive.
        if (property == null)
        {
            return NotFound();
        }

        // Retrieve the first saved image URL for this property.
        var imageUrl = await _context.PropertyImages
            .Where(image => image.PropertyId == property.PropertyId)
            .Select(image => image.ImageUrl)
            .FirstOrDefaultAsync();

        // Retrieve this property's reviews and the guest who wrote each one.
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(review => review.Guest)
            .Where(review => review.PropertyId == property.PropertyId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync();

        // Calculate the average rating, or use zero if there are no reviews.
        double averageRating = reviews.Count > 0
            ? reviews.Average(review => review.Rating)
            : 0;

        // Combine the property information, image, and reviews.
        var viewModel = new PropertyDetailsViewModel
        {
            Property = property,
            PropertyImageUrl = imageUrl,
            Reviews = reviews,
            AverageRating = averageRating,
            ReviewCount = reviews.Count
        };

        // Send the combined information to the Details view.
        return View(viewModel);
    }

    // Display the privacy page.
    public IActionResult Privacy()
    {
        return View();
    }

    // Display the error page when an unhandled error occurs.
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
