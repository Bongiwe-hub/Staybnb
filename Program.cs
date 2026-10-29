
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. DATABASE CONFIGURATION
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(30)
    )
);

// ============================================================
// 2. ASP.NET CORE IDENTITY AND PASSWORD SECURITY
// ============================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Keep the current registration behaviour.
    // Email confirmation is not required because a verification
    // process has not been configured in this project.
    options.SignIn.RequireConfirmedAccount = false;

    // Password requirements.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout protection against repeated failed login attempts.
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================================
// 3. AUTHENTICATION COOKIE CONFIGURATION
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".Staybnb.Auth";

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Prevent client-side JavaScript from reading the auth cookie.
    options.Cookie.HttpOnly = true;

    // Restrict when the browser sends the cookie cross-site.
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Use HTTPS for cookies when the request uses HTTPS.
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;

    // Authentication session expires after eight hours.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ============================================================
// 4. MVC CONFIGURATION
// ============================================================

builder.Services.AddControllersWithViews();

// ============================================================
// 5. REGISTER CUSTOM SERVICES
// ============================================================

// Used by controllers to record user and administrator activity.
builder.Services.AddScoped<ActivityLogger>();

// ============================================================
// 6. BUILD THE APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// 7. ERROR HANDLING
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ============================================================
// 8. HTTP REQUEST PIPELINE
// ============================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication identifies the user.
app.UseAuthentication();

// Authorization checks whether the user has permission.
app.UseAuthorization();

// ============================================================
// 9. DEFAULT MVC ROUTE
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ============================================================
// 10. SEED ROLES AND SUPERADMIN ACCOUNT
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await RoleSeeder.SeedRolesAndSuperAdminAsync(services);
}

// ============================================================
// 11. START THE APPLICATION
// ============================================================

app.Run();
