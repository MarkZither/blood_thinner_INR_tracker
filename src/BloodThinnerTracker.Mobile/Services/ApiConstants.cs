namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// API endpoint constants for the Blood Thinner Tracker backend.
/// Uses Flurl for clean URL composition in services.
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// OAuth configuration endpoint (OIDC provider discovery for mobile clients).
    /// Returns client IDs, redirect URIs, and scopes for all configured providers.
    /// Example: https://localhost:7235/api/auth/config
    /// </summary>
    public const string OAuthConfigPath = "api/auth/config";

    /// <summary>
    /// Mobile OAuth token exchange endpoint to convert OAuth id_token to internal bearer token.
    /// Validates id_token server-side and returns internal JWT for API access.
    /// Example: https://localhost:7235/api/auth/external/mobile
    /// </summary>
    public const string MobileAuthExchangePath = "api/auth/external/mobile";

    /// <summary>
    /// INR logs endpoint to fetch user's recent INR test results.
    /// Example: https://localhost:7235/api/inr-logs
    /// </summary>
    public const string InrLogsPath = "api/inr-logs";

    /// <summary>
    /// Medication logs endpoint to fetch user's medication history.
    /// Example: https://localhost:7235/api/medication-logs
    /// </summary>
    public const string MedicationLogsPath = "api/medication-logs";

    /// <summary>
    /// Health check endpoint for connectivity testing.
    /// Example: https://localhost:7235/health
    /// </summary>
    public const string HealthPath = "health";
}
