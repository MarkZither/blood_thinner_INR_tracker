namespace BloodThinnerTracker.Shared.Constants
{
    /// <summary>
    /// Common API path constants shared across projects.
    /// Use leading slash when concatenating with a base URL (e.g. apiBase.TrimEnd('/') + ApiPaths.OAuthConfig).
    /// </summary>
    public static class ApiPaths
    {
        /// <summary>
        /// Path for OAuth configuration endpoint (returns OAuthConfig).
        /// </summary>
        public const string OAuthConfig = "/api/auth/config";

        /// <summary>
        /// Path for mobile auth exchange endpoint.
        /// </summary>
        public const string MobileAuthExchange = "/api/auth/external/mobile";
    }
}
