namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Configuration options for mobile app features.
/// Binds to Features:* configuration keys from appsettings.json or environment variables.
/// Example: Features__UseMockServices=true, Features__ApiRootUrl=https://localhost:7235
/// </summary>
public class FeaturesOptions
{
    public const string SectionName = "Features";

    /// <summary>
    /// Use mock services (MockAuthService, MockOAuthConfigService, MockInrService).
    /// When false, uses real API services with OAuth.
    /// Default: false (production mode)
    /// </summary>
    public bool UseMockServices { get; set; } = false;

    /// <summary>
    /// Root URL for the API backend (e.g., https://localhost:7235 or https://api.bloodtracker.com).
    /// Used to construct OAuth config and token exchange endpoints.
    /// Default: https://api.example.invalid (fails fast if not configured)
    /// </summary>
    public string ApiRootUrl { get; set; } = "https://api.example.invalid";

    /// <summary>
    /// Enable fetching runtime remote configuration on startup.
    /// </summary>
    public bool RemoteConfigEnabled { get; set; } = false;

    /// <summary>
    /// URL to retrieve remote configuration JSON from (optional).
    /// </summary>
    public string? RemoteConfigUrl { get; set; }
}
