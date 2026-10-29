using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // SUPERADMIN DASHBOARD
        // ============================================================

        public IActionResult Index()
        {
            var totalUsers = _context.Users.Count();

            var totalHostApplications =
                _context.HostApplications.Count();

            var totalProperties =
                _context.Properties.Count();

            var totalActivityLogs =
                _context.ActivityLogs.Count();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalHostApplications = totalHostApplications;
            ViewBag.TotalProperties = totalProperties;
            ViewBag.TotalActivityLogs = totalActivityLogs;

            return View();
        }


        // ============================================================
        // MANAGE USERS
        // ============================================================

        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var userList = new List<UserManagementViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserManagementViewModel
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return View(userList);
        }
                [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "Invalid user.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // A SuperAdmin cannot be changed through this action.
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                TempData["Error"] =
                    "The SuperAdmin account cannot be changed.";

                return RedirectToAction(nameof(ManageUsers));
            }

            // Only Guests can be promoted using this action.
            if (!await _userManager.IsInRoleAsync(user, "Guest"))
            {
                TempData["Error"] =
                    "Only Guest users can be promoted to Admin.";

                return RedirectToAction(nameof(ManageUsers));
            }

            var removeGuestResult =
                await _userManager.RemoveFromRoleAsync(user, "Guest");

            if (!removeGuestResult.Succeeded)
            {
                TempData["Error"] =
                    "Could not remove the Guest role.";

                return RedirectToAction(nameof(ManageUsers));
            }

            var addAdminResult =
                await _userManager.AddToRoleAsync(user, "Admin");

            if (!addAdminResult.Succeeded)
            {
                // Restore Guest role if Admin assignment fails.
                await _userManager.AddToRoleAsync(user, "Guest");

                TempData["Error"] =
                    "Could not promote the user to Admin.";

                return RedirectToAction(nameof(ManageUsers));
            }

            TempData["Success"] =
                $"{user.FirstName} {user.LastName} has been promoted to Admin.";

            return RedirectToAction(nameof(ManageUsers));
        }
    }
}