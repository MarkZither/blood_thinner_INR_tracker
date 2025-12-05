namespace BloodThinnerTracker.Mobile.Config
{
    /// <summary>
    /// Configuration options for background sync intervals used by the mobile app.
    /// Binds to top-level configuration section defined in appsettings.json: "SyncOptions".
    /// </summary>
    public class SyncOptions
    {
        public const string SectionName = "SyncOptions";

        /// <summary>
        /// Default sync interval in minutes for periodic background syncs.
        /// Default: 60 minutes
        /// </summary>
        public int DefaultIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Fast-path interval (e.g., when charging + Wi‑Fi) in minutes.
        /// Default: 15 minutes
        /// </summary>
        public int FastPathIntervalMinutes { get; set; } = 15;
    }
}
