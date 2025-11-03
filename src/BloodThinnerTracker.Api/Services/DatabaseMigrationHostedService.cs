using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Background service that runs database migrations after the application starts.
/// This allows the app to become healthy and respond to health checks while migrations run.
/// </summary>
public class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database migration service starting...");

        try
        {
            // Run migrations in a background task so the app can start accepting requests
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Beginning database migration...");
                    await _serviceProvider.EnsureDatabaseAsync();
                    _logger.LogInformation("Database migration completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database migration failed");
                    // Don't crash the app - let it stay running so we can diagnose the issue
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start database migration");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database migration service stopping");
        return Task.CompletedTask;
    }
}
