using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using System;
using System.IO;

namespace BloodThinnerTracker.Mobile
{
    public static partial class MauiProgram
    {
        static partial void RegisterPlatformServices(MauiAppBuilder builder)
        {
            // Register Windows-specific notification scheduler
            builder.Services.AddSingleton<Services.INotificationScheduler, Platforms.Windows.NotificationSchedulerWindows>();

            // Note: AUMID/COM activator registration is performed later once a
            // temporary IServiceProvider has been built so an ILogger can be used
            // for diagnostics. Avoid calling TryRegisterAumidAndComServer here
            // before DI/logging are available.

            // Perform best-effort runtime initialization that requires IServiceProvider by
            // building a temporary provider from the collection. This avoids putting
            // platform initialization into shared startup while also avoiding a
            // separate IPlatformInitializer type which caused multi-target compile issues.

            Microsoft.Extensions.Logging.ILogger? logger = null;

            try
            {
                using var sp = builder.Services.BuildServiceProvider();
                logger = sp.GetService<Microsoft.Extensions.Logging.ILogger>();

                try
                {
                    BloodThinnerTracker.Mobile.Platforms.Windows.WindowsServiceProvider.Initialize(sp);

                    try
                    {
                        var config = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                        var aumid = config?["Windows:Aumid"] ?? "MarkZither.BloodThinnerTracker.Mobile";

                        BloodThinnerTracker.Mobile.Platforms.Windows.WindowsNotificationRegistration.TryRegisterAumidAndComServer(aumid);
                        BloodThinnerTracker.Mobile.Platforms.Windows.WindowsNotificationRegistration.EnsureStartMenuShortcutWithAumid(aumid);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "PlatformServices(Windows): Failed to register AUMID or shortcut");
                    }

                    try
                    {
                        BloodThinnerTracker.Mobile.Platforms.Windows.Background.WindowsSchedulingHelper.RegisterBackgroundTask(
                            intervalMinutes: 15,
                            force: false,
                            store: new BloodThinnerTracker.Mobile.Platforms.Windows.Background.WindowsSchedulingFlagStore(),
                            logger: logger as Microsoft.Extensions.Logging.ILogger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "PlatformServices(Windows): Failed to register background task");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "PlatformServices(Windows): WindowsServiceProvider.Initialize failed");
                }
            }
            catch (Exception ex)
            {
                // Log init failures using the configured logger. Do not persist
                // separate platform trace files here — use the existing logging
                // pipeline so infrastructure controls where logs are written.
                System.Diagnostics.Debug.WriteLine($"PlatformServices(Windows): Best-effort init failed: {ex}");
                logger?.LogError(ex, "PlatformServices(Windows): WindowsServiceProvider Best-effort init failed");
            }
        }
    }
}
// No separate runtime initializer type needed; initialization attempted above.
