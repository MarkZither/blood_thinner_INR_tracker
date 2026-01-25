using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Mock implementation of IForegroundSyncService for offline development and testing.
/// Does not perform any actual sync operations - just logs that sync was requested.
/// </summary>
public class MockForegroundSyncService : IForegroundSyncService
{
    private readonly ILogger<MockForegroundSyncService>? _logger;

    public MockForegroundSyncService(ILogger<MockForegroundSyncService>? logger = null)
    {
        _logger = logger;
    }

    public Task EnsureForegroundSyncAsync()
    {
        _logger?.LogDebug("MockForegroundSyncService: EnsureForegroundSyncAsync called (no-op in mock mode)");
        return Task.CompletedTask;
    }
}
