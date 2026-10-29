using Microsoft.AspNetCore.Identity;
using Staybnb.Models;

namespace Staybnb.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAndSuperAdminAsync(
            IServiceProvider serviceProvider)
        {
            // Get the RoleManager from the application's
            // dependency injection container.
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Get the UserManager from the application's
            // dependency injection container.
            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();


            // ========================================================
            // CREATE REQUIRED ROLES
            // ========================================================

            string[] roles =
            {
                "Guest",
                "Host",
                "Admin",
                "SuperAdmin"
            };

            foreach (var role in roles)
            {
                // Check whether the role already exists.
                if (!await roleManager.RoleExistsAsync(role))
                {
                    // Create the role if it doesn't exist.
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }


            // ========================================================
            // CREATE SUPERADMIN
            // ========================================================

            const string superAdminEmail = "superadmin@staybnb.com";
            const string superAdminPassword = "SuperAdmin123!";

            // Check whether the SuperAdmin account already exists.
            var superAdmin =
                await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                // Create the SuperAdmin user.
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Administrator",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(
                    superAdmin,
                    superAdminPassword);

                if (result.Succeeded)
                {
                    // Give the user the SuperAdmin role.
                    await userManager.AddToRoleAsync(
                        superAdmin,
                        "SuperAdmin");
                }
            }
            else
            {
                // Make sure an existing SuperAdmin account
                // has the correct role.
                if (!await userManager.IsInRoleAsync(
                    superAdmin,
                    "SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(
                        superAdmin,
                        "SuperAdmin");
                }
            }
        }
    }
}
