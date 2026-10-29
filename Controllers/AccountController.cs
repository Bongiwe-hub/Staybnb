
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Models;
using Staybnb.ViewModels;
using Staybnb.Services;
using System.Security.Claims;

namespace Staybnb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ActivityLogger _activityLogger;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ActivityLogger activityLogger,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        // ========================================================
        // REGISTRATION - DISPLAY FORM
        // ========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ========================================================
        // REGISTRATION - CREATE ACCOUNT
        // ========================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,

                // Retained from the existing application.
                // This is not actual email verification.
                EmailConfirmed = true,

                CreatedAt = DateTime.UtcNow
            };

            // Create the user using ASP.NET Core Identity.
            // Identity applies the configured password rules here.
            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (result.Succeeded)
            {
                // Every newly registered user starts as a Guest.
                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Guest");

                if (!roleResult.Succeeded)
                {
                    // Log the role assignment failure.
                    _logger.LogError(
                        "Could not assign the Guest role to new user {UserId}.",
                        user.Id);

                    // Avoid leaving an account that could not receive
                    // its required initial role.
                    var deleteResult =
                        await _userManager.DeleteAsync(user);

                    if (!deleteResult.Succeeded)
                    {
                        _logger.LogError(
                            "Could not remove incompletely registered user {UserId}.",
                            user.Id);
                    }

                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return View(model);
                }

                // Record successful registration in the audit log.
                var registeredUser = new ClaimsPrincipal(
                    new ClaimsIdentity(new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            user.Id)
                    }));

                try
                {
                    await _activityLogger.LogAsync(
                        registeredUser,
                        "Registration",
                        $"User {user.Email} registered successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not audit registration for user {UserId}.",
                        user.Id);
                }

                // Ask the new user to log in.
                return RedirectToAction(nameof(Login));
            }

            // Display password or account creation validation errors.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // ========================================================
        // LOGIN - DISPLAY FORM
        // ========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ========================================================
        // LOGIN - AUTHENTICATE USER
        // ========================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            bool rememberMe = false,
            string? returnUrl = null)
        {
            // Validate required login fields.
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email and password are required.");

                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // Authenticate the user.
            // IMPORTANT: failed attempts can now trigger lockout.
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                rememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    // Record successful login.
                    try
                    {
                        await _activityLogger.LogAsync(
                            new ClaimsPrincipal(
                                new ClaimsIdentity(new[]
                                {
                                    new Claim(
                                        ClaimTypes.NameIdentifier,
                                        user.Id)
                                })),
                            "Login",
                            $"User {user.Email} logged in successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Could not audit login for user {UserId}.",
                            user.Id);
                    }
                }

                // Redirect only to a local URL to prevent open redirects.
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            // Give a clear message when Identity reports a lockout.
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account is temporarily locked because of " +
                    "too many failed login attempts. Please try again later.");
            }
            else
            {
                // Do not reveal whether the email or password was incorrect.
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ========================================================
        // ACCESS DENIED
        // ========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ========================================================
        // LOGOUT
        // ========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            // Record logout before clearing the authentication session.
            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    await _activityLogger.LogAsync(
                        User,
                        "Logout",
                        "User logged out successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not audit logout for user {UserId}.",
                        userId);
                }
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
