using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Windows.Background
{
    /// <summary>
    /// Helper class for registering and unregistering Windows background tasks.
    /// Mirrors the Android <c>ForegroundServiceHelper</c> pattern with idempotent
    /// scheduling using <see cref="ISchedulingFlagStore"/> and <see cref="SchedulingManager"/>.
    ///
    /// Note: Background tasks only work in MSIX-packaged apps. In unpackaged debug builds,
    /// registration will fail gracefully and log a warning.
    /// </summary>
    public static class WindowsSchedulingHelper
    {
        /// <summary>
        /// The name used to identify the sync background task.
        /// </summary>
        public const string TaskName = "BloodThinnerTrackerSyncTask";

        /// <summary>
        /// The entry point for the background task (fully qualified type name).
        /// </summary>
        public const string TaskEntryPoint = "BloodThinnerTracker.Mobile.Platforms.Windows.Background.SyncBackgroundTask";

        /// <summary>
        /// Check if the app is running as a packaged (MSIX) app.
        /// Background tasks only work in packaged apps.
        /// </summary>
        public static bool IsPackagedApp()
        {
            try
            {
                // This will throw if not packaged
                var package = global::Windows.ApplicationModel.Package.Current;
                return package != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register a periodic background task. Uses <see cref="SchedulingManager"/>
        /// for idempotent scheduling (won't register if already scheduled unless force=true).
        /// </summary>
        /// <param name="intervalMinutes">The interval in minutes (minimum 15 for TimeTrigger).</param>
        /// <param name="force">If true, re-register even if already scheduled.</param>
        /// <param name="store">Optional scheduling flag store for idempotency tracking.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <returns>True if registration succeeded or was already done; false on failure.</returns>
        public static bool RegisterBackgroundTask(
            uint intervalMinutes = 15,
            bool force = false,
            ISchedulingFlagStore? store = null,
            ILogger? logger = null)
        {
            try
            {
                // Background tasks only work in packaged (MSIX) apps
                if (!IsPackagedApp())
                {
                    logger?.LogDebug("Skipping background task registration - app is not packaged (MSIX). Background sync will not run when app is closed.");
                    return false;
                }

                // Check if we should schedule using the platform-agnostic manager
                store ??= new WindowsSchedulingFlagStore();
                if (!SchedulingManager.ShouldSchedule(store, force))
                {
                    logger?.LogDebug("Background task already scheduled; skipping registration");
                    return true;
                }

                // Ensure the task isn't already registered
                var existingTask = BackgroundTaskRegistration.AllTasks
                    .FirstOrDefault(t => t.Value.Name == TaskName);
                if (existingTask.Value != null)
                {
                    if (!force)
                    {
                        logger?.LogDebug("Background task '{TaskName}' already registered", TaskName);
                        SchedulingManager.MarkScheduled(store);
                        return true;
                    }

                    // Unregister existing if forcing
                    existingTask.Value.Unregister(cancelTask: false);
                    logger?.LogDebug("Unregistered existing background task for re-registration");
                }

                // Check background task access
                var requestStatus = BackgroundExecutionManager.RequestAccessAsync().AsTask().Result;
                if (requestStatus == BackgroundAccessStatus.DeniedBySystemPolicy ||
                    requestStatus == BackgroundAccessStatus.DeniedByUser)
                {
                    logger?.LogWarning("Background task access denied: {Status}", requestStatus);
                    return false;
                }

                // TimeTrigger requires minimum 15 minutes
                uint actualInterval = Math.Max(intervalMinutes, 15);
                var trigger = new TimeTrigger(actualInterval, oneShot: false);

                // Build and register the task
                var builder = new BackgroundTaskBuilder
                {
                    Name = TaskName,
                    TaskEntryPoint = TaskEntryPoint
                };
                builder.SetTrigger(trigger);

                // Add condition to only run when internet is available
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                var registration = builder.Register();
                logger?.LogInformation("Registered background task '{TaskName}' with {Interval} minute interval",
                    TaskName, actualInterval);

                SchedulingManager.MarkScheduled(store);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to register background task '{TaskName}'", TaskName);
                return false;
            }
        }

        /// <summary>
        /// Unregister the background task.
        /// </summary>
        /// <param name="store">Optional scheduling flag store to clear.</param>
        /// <param name="logger">Optional logger for diagnostics.</param>
        /// <returns>True if the task was unregistered or didn't exist; false on failure.</returns>
        public static bool UnregisterBackgroundTask(ISchedulingFlagStore? store = null, ILogger? logger = null)
        {
            try
            {
                var existingTask = BackgroundTaskRegistration.AllTasks
                    .FirstOrDefault(t => t.Value.Name == TaskName);

                if (existingTask.Value != null)
                {
                    existingTask.Value.Unregister(cancelTask: true);
                    logger?.LogInformation("Unregistered background task '{TaskName}'", TaskName);
                }
                else
                {
                    logger?.LogDebug("Background task '{TaskName}' was not registered", TaskName);
                }

                store ??= new WindowsSchedulingFlagStore();
                SchedulingManager.ClearScheduled(store);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to unregister background task '{TaskName}'", TaskName);
                return false;
            }
        }

        /// <summary>
        /// Check if the background task is currently registered.
        /// </summary>
        public static bool IsRegistered()
        {
            return BackgroundTaskRegistration.AllTasks.Any(t => t.Value.Name == TaskName);
        }
    }
}
