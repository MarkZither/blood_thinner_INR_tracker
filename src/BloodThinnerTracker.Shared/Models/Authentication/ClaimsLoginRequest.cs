// BloodThinnerTracker.Shared - Claims-Based Login Request Model
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Shared.Models.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Claims-based authentication request for Web SSO scenarios.
/// Used when the Web application handles OAuth and passes validated claims to API.
/// </summary>
public class ClaimsLoginRequest
{
    /// <summary>
    /// Gets or sets the OAuth2 provider name (AzureAD or Google).
    /// </summary>
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address from OAuth claims.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name from OAuth claims.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the external user ID (sub claim) from OAuth provider.
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// Gets or sets the device identifier for security tracking.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device platform (Web, iOS, Android).
    /// </summary>
    public string? DevicePlatform { get; set; }
}
