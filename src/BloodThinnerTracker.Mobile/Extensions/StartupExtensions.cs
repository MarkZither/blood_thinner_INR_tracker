using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Extensions;

/// <summary>
/// Extension methods for post-build startup operations (diagnostics, migrations).
/// Keeps MauiProgram.cs concise by encapsulating startup logic here.
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Delay before starting background services to allow UI to load first.
    /// This prevents "not responding" messages during splash screen.
    /// </summary>
    private const int StartupDelayMs = 2000;

    /// <summary>
    /// Runs startup diagnostics and logs registered services.
    /// Background services are deferred to allow the UI to become responsive first.
    /// </summary>
    public static MauiApp RunStartupDiagnostics(this MauiApp app, IConfiguration configuration)
    {
        try
        {
            var startupLogger = app.Services.GetService<ILogger<App>>();
            var dbgAttached = System.Diagnostics.Debugger.IsAttached;
            var enableInProcFlag = configuration["Features:EnableInProcBackground"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            var hostedSvcTypes = app.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
                .Select(s => s.GetType().FullName)
                .ToList();

            startupLogger?.LogInformation(
                "Startup diagnostic: Debugger.IsAttached={Attached} EnableInProcBackground={EnableInProc} RegisteredHostedServices={Services}",
                dbgAttached, enableInProcFlag, string.Join(",", hostedSvcTypes));

            // Defer background service startup to allow UI to load first
            if (dbgAttached || enableInProcFlag)
            {
                startupLogger?.LogInformation("Startup: Deferring DevSyncHostedService start by {Delay}ms to allow UI to load", StartupDelayMs);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(StartupDelayMs);
                    TriggerDevSyncHostedService(app, startupLogger);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunStartupDiagnostics: Startup diagnostic logging failed: {ex.Message}");
        }

        return app;
    }

    private static void TriggerDevSyncHostedService(MauiApp app, ILogger? logger)
    {
        try
        {
            var dev = app.Services.GetService<Services.DevSyncHostedService>();
            if (dev != null)
            {
                _ = Task.Run(() => dev.StartAsync(System.Threading.CancellationToken.None));
                logger?.LogInformation("Startup: Triggered DevSyncHostedService.StartAsync()");
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to start DevSyncHostedService proactively");
        }
    }

    /// <summary>
    /// Schedules EF Core migrations to run asynchronously after a short delay.
    /// This allows the UI to become responsive before heavy database work begins.
    /// The app can still function with API fallback if migrations haven't completed yet.
    /// </summary>
    public static MauiApp ApplyDatabaseMigrations(this MauiApp app)
    {
        // Schedule migrations to run asynchronously after UI loads
        _ = Task.Run(async () =>
        {
            // Wait for UI to become responsive first
            await Task.Delay(StartupDelayMs);
            await ApplyDatabaseMigrationsAsync(app);
        });

        return app;
    }

    /// <summary>
    /// Applies EF Core migrations to the local SQLite database asynchronously.
    /// </summary>
    private static async Task ApplyDatabaseMigrationsAsync(MauiApp app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>();
            if (db != null)
            {
                var startupLogger = scope.ServiceProvider.GetService<ILogger<App>>();
                startupLogger?.LogInformation("Startup: Applying EF Core migrations to local SQLite database (deferred)");

                // Use async migration to avoid blocking
                await db.Database.MigrateAsync();

                startupLogger?.LogInformation("Startup: Local SQLite database migrations applied successfully");

                var seeder = scope.ServiceProvider.GetService<Services.MobileUserSeeder>();
                seeder?.EnsureSeeded();
            }
        }
        catch (Exception ex)
        {
            var fallbackLogger = app.Services.GetService<ILogger<App>>();
            fallbackLogger?.LogError(ex, "Startup: Failed to apply database migrations");
        }
    }
}
