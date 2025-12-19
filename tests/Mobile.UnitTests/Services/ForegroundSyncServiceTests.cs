using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using BloodThinnerTracker.Mobile.Services;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests.Services
{
    public class ForegroundSyncServiceTests
    {
        [Fact]
        public async Task EnsureForegroundSyncAsync_WhenNoLastSync_TriggersSync()
        {
            var cache = new Mock<ICacheService>();
            cache.Setup(c => c.GetAsync("last_sync_utc")).Returns(Task.FromResult<string?>(null));

            var worker = new Mock<ICacheSyncWorker>();
            worker.Setup(w => w.SyncOnceAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

            var options = Options.Create(new Config.SyncOptions { DefaultIntervalMinutes = 60 });

            var svc = new ForegroundSyncService(cache.Object, worker.Object, options, NullLogger<ForegroundSyncService>.Instance);

            await svc.EnsureForegroundSyncAsync();

            worker.Verify(w => w.SyncOnceAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EnsureForegroundSyncAsync_WhenRecentSkip_TriggersNoSync()
        {
            var cache = new Mock<ICacheService>();
            var recent = DateTime.UtcNow.ToString("o");
            cache.Setup(c => c.GetAsync("last_sync_utc")).Returns(Task.FromResult<string?>(recent));

            var worker = new Mock<ICacheSyncWorker>();

            var options = Options.Create(new Config.SyncOptions { DefaultIntervalMinutes = 60 });

            var svc = new ForegroundSyncService(cache.Object, worker.Object, options, NullLogger<ForegroundSyncService>.Instance);

            await svc.EnsureForegroundSyncAsync();

            worker.Verify(w => w.SyncOnceAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EnsureForegroundSyncAsync_WhenOld_TriggersSync()
        {
            var cache = new Mock<ICacheService>();
            var old = DateTime.UtcNow.AddHours(-5).ToString("o");
            cache.Setup(c => c.GetAsync("last_sync_utc")).Returns(Task.FromResult<string?>(old));

            var worker = new Mock<ICacheSyncWorker>();
            worker.Setup(w => w.SyncOnceAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

            var options = Options.Create(new Config.SyncOptions { DefaultIntervalMinutes = 60 });

            var svc = new ForegroundSyncService(cache.Object, worker.Object, options, NullLogger<ForegroundSyncService>.Instance);

            await svc.EnsureForegroundSyncAsync();

            worker.Verify(w => w.SyncOnceAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}
