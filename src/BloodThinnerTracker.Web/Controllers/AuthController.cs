using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using BloodThinnerTracker.Web.Services;

namespace BloodThinnerTracker.Web.Controllers;

/// <summary>
/// Handles OAuth authentication challenges and logout
/// </summary>
[Route("[controller]/[action]")]
public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthController(
        ILogger<AuthController> logger,
        CustomAuthenticationStateProvider authStateProvider)
    {
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Initiates Microsoft OAuth authentication challenge
    /// GET: /Auth/LoginMicrosoft?returnUrl=/dashboard
    /// </summary>
    [HttpGet]
    public IActionResult LoginMicrosoft(string? returnUrl = null)
    {
        // Validate the returnUrl using ReturnUrlValidator to prevent malicious URLs
        var validationResult = ReturnUrlValidator.Validate(returnUrl);
        var safeReturn = validationResult.IsValid ? validationResult.Normalized! : "/dashboard";

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("LoginMicrosoft: Invalid returnUrl rejected. Reason: {Reason}, Raw: {RawUrl}",
                validationResult.ValidationResultCode, returnUrl ?? "(null)");
        }

        // Encode once for inclusion in the local RedirectUri query string
        var encodedReturn = Uri.EscapeDataString(safeReturn);

        // The OAuth middleware will round-trip AuthenticationProperties via the provider 'state'.
        // Use a local callback that includes the encoded returnUrl so the app can redirect there
        // after the external provider completes.
        var redirectAfterExternal = Url.Content($"~/oauth-complete?returnUrl={encodedReturn}");

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectAfterExternal,
            Items =
            {
                { "scheme", "AzureAD" },
                // Store the raw/safe return URL as an item so it can also be retrieved from properties.Items
                { "returnUrl", safeReturn }
            }
        };

        _logger.LogInformation("Initiating Microsoft OAuth challenge. ReturnUrl: {ReturnUrl} RedirectUri: {RedirectUri}", safeReturn, redirectAfterExternal);

        return Challenge(properties, "AzureAD");
    }

    /// <summary>
    /// Initiates Google OAuth authentication challenge
    /// GET: /Auth/LoginGoogle?returnUrl=/dashboard
    /// </summary>
    [HttpGet]
    public IActionResult LoginGoogle(string? returnUrl = null)
    {
        // Validate the returnUrl using ReturnUrlValidator to prevent malicious URLs
        var validationResult = ReturnUrlValidator.Validate(returnUrl);
        var safeReturn = validationResult.IsValid ? validationResult.Normalized! : "/dashboard";

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("LoginGoogle: Invalid returnUrl rejected. Reason: {Reason}, Raw: {RawUrl}",
                validationResult.ValidationResultCode, returnUrl ?? "(null)");
        }

        // Encode once for inclusion in the local RedirectUri query string
        var encodedReturn = Uri.EscapeDataString(safeReturn);

        // The OAuth middleware will round-trip AuthenticationProperties via the provider 'state'.
        // Use a local callback that includes the encoded returnUrl so the app can redirect there
        // after the external provider completes.
        var redirectAfterExternal = Url.Content($"~/oauth-complete?returnUrl={encodedReturn}");

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectAfterExternal,
            Items =
            {
                { "scheme", GoogleDefaults.AuthenticationScheme },
                // Store the raw/safe return URL as an item so it can also be retrieved from properties.Items
                { "returnUrl", safeReturn }
            }
        };

        _logger.LogInformation("Initiating Google OAuth challenge. ReturnUrl: {ReturnUrl} RedirectUri: {RedirectUri}", safeReturn, redirectAfterExternal);

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

        // STEP 1: Clear Blazor authentication state and cached tokens
        await _authStateProvider.MarkUserAsLoggedOutAsync();

        // STEP 2: Sign out from local cookie authentication
        // NOTE: We DON'T sign out from OAuth schemes (Microsoft/Google) here because:
        // - Those are just for authentication challenges, not session storage
        // - Signing out from them prevents the next login from working
        // - The user's OAuth provider session (Microsoft/Google) stays active, which is expected
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User logged out successfully - local session cleared");

        // Redirect to login page with forceLoad to clear Blazor circuit
        return Redirect("/login");
    }
}
