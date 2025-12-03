using global::Windows.Storage;
using BloodThinnerTracker.Mobile.Services;

namespace BloodThinnerTracker.Mobile.Platforms.Windows.Background
{
    /// <summary>
    /// Windows implementation of <see cref="ISchedulingFlagStore"/> using
    /// <see cref="ApplicationData.LocalSettings"/> for persistence.
    /// This mirrors the Android SharedPreferences-based approach.
    ///
    /// Note: This only works in MSIX-packaged apps. In unpackaged debug builds,
    /// ApplicationData.Current is not available and methods will return defaults.
    /// </summary>
    public class WindowsSchedulingFlagStore : ISchedulingFlagStore
    {
        private const string ScheduledKey = "BackgroundTaskScheduled";

        // Fallback for unpackaged apps where ApplicationData is not available
        private static bool _inMemoryScheduled = false;

        /// <summary>
        /// Check if ApplicationData is available (only in packaged apps).
        /// </summary>
        private static bool TryGetLocalSettings(out ApplicationDataContainer? settings)
        {
            try
            {
                settings = ApplicationData.Current.LocalSettings;
                return true;
            }
            catch
            {
                settings = null;
                return false;
            }
        }

        /// <summary>
        /// Check if a background task has been scheduled.
        /// </summary>
        public bool IsScheduled()
        {
            if (TryGetLocalSettings(out var settings) && settings != null)
            {
                if (settings.Values.TryGetValue(ScheduledKey, out var value) && value is bool scheduled)
                {
                    return scheduled;
                }
                return false;
            }

            // Fallback for unpackaged apps
            return _inMemoryScheduled;
        }

        /// <summary>
        /// Set the scheduled flag.
        /// </summary>
        /// <param name="scheduled">Whether the task is scheduled.</param>
        public void SetScheduled(bool scheduled)
        {
            if (TryGetLocalSettings(out var settings) && settings != null)
            {
                settings.Values[ScheduledKey] = scheduled;
            }
            else
            {
                // Fallback for unpackaged apps
                _inMemoryScheduled = scheduled;
            }
        }
    }
}
