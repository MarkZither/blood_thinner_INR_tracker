using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Mobile;
using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mobile.IntegrationTests
{
    public class StartupTests
    {
        [Fact]
        public async Task Startup_WiresOptions_RegistersHostedServicesAndCacheSync()
        {
            // Ensure MAUI platform-specific registrations are skipped in tests
            Environment.SetEnvironmentVariable("Test__SkipPlatformServices", "true");
            // Prevent LoggingConfigExtensions from attempting to use FileSystem.AppDataDirectory
            Environment.SetEnvironmentVariable("Diagnostics__Serilog__UseAppDataDirectory", "false");

            try
            {
                var app = MauiProgram.CreateMauiApp();
                using var scope = app.Services.CreateScope();

                // SyncOptions should be bound with defaults (appsettings provides defaults)
                var syncOptions = scope.ServiceProvider.GetService<Microsoft.Extensions.Options.IOptions<BloodThinnerTracker.Mobile.Config.SyncOptions>>();
                Assert.NotNull(syncOptions);
                Assert.True(syncOptions.Value.DefaultIntervalMinutes > 0);

                // CacheSyncService should be registered as concrete service
                var cacheSync = scope.ServiceProvider.GetService<CacheSyncService>();
                Assert.NotNull(cacheSync);

                // And it should also be available as an IHostedService registration
                var hosted = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
                Assert.Contains(hosted, h => h.GetType() == typeof(CacheSyncService));
            }
            finally
            {
                // Clean up env vars to avoid leaking into other tests
                Environment.SetEnvironmentVariable("Test__SkipPlatformServices", null);
                Environment.SetEnvironmentVariable("Diagnostics__Serilog__UseAppDataDirectory", null);
            }
        }

        [Fact]
        public async Task ColdStart_MigrationsApplied_NoPendingMigrations()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"bt_startup_integration_{Guid.NewGuid():N}.db");
            try
            {
                var cs = new SqliteConnectionStringBuilder { DataSource = tmp }.ToString();
                await using var connection = new SqliteConnection(cs);
                await connection.OpenAsync();

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                    .Options;

                var logger = NullLogger<ApplicationDbContext>.Instance;

                await using var db = new ApplicationDbContext(options, logger);

                // Apply migrations as a cold-start would
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                await db.Database.MigrateAsync(cts.Token);

                var pending = await db.Database.GetPendingMigrationsAsync();
                Assert.Empty(pending);

                // Ensure the INRTests table exists (no records expected)
                var any = await db.Set<INRTest>().AnyAsync();
                Assert.False(any);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }
}

