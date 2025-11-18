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
            // Run migrations synchronously during host startup so the application
            // does not accept requests that may write to schema elements which
            // are not yet created (prevents 'no such table' errors).
            _logger.LogInformation("Beginning database migration...");
            await _serviceProvider.EnsureDatabaseAsync();
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            // Log and rethrow to fail fast if migrations cannot be applied - this
            // avoids running the application with a partially-initialized schema.
            _logger.LogError(ex, "Database migration failed during startup");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database migration service stopping");
        return Task.CompletedTask;
    }
}
