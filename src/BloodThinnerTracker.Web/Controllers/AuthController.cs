using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodThinnerTracker.Web.Controllers;

/// <summary>
/// Handles OAuth authentication challenges and logout
/// </summary>
[Route("[controller]/[action]")]
public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initiates Microsoft OAuth authentication challenge
    /// GET: /Auth/LoginMicrosoft?returnUrl=/dashboard
    /// </summary>
    [HttpGet]
    public IActionResult LoginMicrosoft(string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/dashboard",
            Items =
            {
                { "scheme", MicrosoftAccountDefaults.AuthenticationScheme }
            }
        };

        _logger.LogInformation("Initiating Microsoft OAuth challenge. ReturnUrl: {ReturnUrl}", returnUrl);

        return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Initiates Google OAuth authentication challenge
    /// GET: /Auth/LoginGoogle?returnUrl=/dashboard
    /// </summary>
    [HttpGet]
    public IActionResult LoginGoogle(string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/dashboard",
            Items =
            {
                { "scheme", GoogleDefaults.AuthenticationScheme }
            }
        };

        _logger.LogInformation("Initiating Google OAuth challenge. ReturnUrl: {ReturnUrl}", returnUrl);

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles user logout
    /// GET: /Auth/Logout
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logging out");

        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect to login page
        return Redirect("/login");
    }
}
