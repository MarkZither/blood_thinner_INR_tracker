using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using BloodThinnerTracker.Mobile.Config;
// using BloodThinnerTracker.Mobile.Services; // already imported
using Xunit;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Mobile.Services;

namespace Mobile.IntegrationTests
{
    public class CacheSyncIntegrationTests
    {
        [Fact]
        public async Task FetchAndUpsertAsync_PopulatesDatabase()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"bt_integration_{Guid.NewGuid():N}.db");
            try
            {
                var cs = new SqliteConnectionStringBuilder { DataSource = tmp }.ToString();
                await using var connection = new SqliteConnection(cs);
                await connection.OpenAsync();

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;

                var logger = NullLogger<ApplicationDbContext>.Instance;

                await using var db = new ApplicationDbContext(options, logger);
                await db.Database.MigrateAsync();

                // Ensure a local device user exists so medical entity saves do not fail FK checks
                var localUser = new BloodThinnerTracker.Shared.Models.User
                {
                    PublicId = Guid.NewGuid(),
                    Email = "mobile@local",
                    Name = "Local Device User",
                    AuthProvider = "Local",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                db.Users.Add(localUser);
                await db.SaveChangesAsync();
                db.CurrentUserId = localUser.Id;
                await db.SaveChangesAsync();

                // Setup repositories against the DB
                var medRepo = new MedicationRepository(db, null, NullLogger<MedicationRepository>.Instance);
                var medLogRepo = new MedicationLogRepository(db, null);
                var inrRepo = new InrRepository(db, null);

                // Simple in-memory cache implementation for integration test
                ICacheService cache = new TestCacheService();

                // Mock API client
                var api = new Mock<IApiClientFacade>();
                var medPub = Guid.NewGuid();
                api.Setup(a => a.GetMedicationsAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<global::ApiSdk.Models.MedicationResponse>
                {
                    new global::ApiSdk.Models.MedicationResponse{ PublicId = medPub, Name = "TestMed", Dosage = 1m, DosageUnit = "mg" }
                });

                api.Setup(a => a.GetRecentInrTestsAsync(It.IsAny<int>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<global::ApiSdk.Models.INRTestResponse>
                {
                    new global::ApiSdk.Models.INRTestResponse{ PublicId = Guid.NewGuid(), TestDate = DateTimeOffset.UtcNow, InrValue = 2.5m }
                });

                api.Setup(a => a.GetMedicationLogsAsync(medPub, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<global::ApiSdk.Models.MedicationLogResponse>
                {
                    new global::ApiSdk.Models.MedicationLogResponse{ PublicId = Guid.NewGuid(), MedicationPublicId = medPub, ScheduledTime = DateTimeOffset.UtcNow }
                });

                var svc = new CacheSyncService(api.Object, inrRepo, cache, medRepo, medLogRepo, Microsoft.Extensions.Options.Options.Create(new SyncOptions()), NullLogger<CacheSyncService>.Instance);

                await svc.FetchAndUpsertAsync(System.Threading.CancellationToken.None);

                // Assert DB has data
                var medCount = await db.Medications.CountAsync();
                var inrCount = await db.INRTests.CountAsync();
                var logCount = await db.MedicationLogs.CountAsync();

                Assert.True(medCount >= 1, "Medication should be persisted");
                Assert.True(inrCount >= 1, "INR test should be persisted");
                Assert.True(logCount >= 1, "Medication log should be persisted");
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }

    // Very small in-memory cache suitable for integration tests
    class TestCacheService : ICacheService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _store = new();

        public Task ClearAsync(string key)
        {
            _store.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> HasValidCacheAsync(string key)
        {
            return Task.FromResult(_store.ContainsKey(key));
        }

        public Task<string?> GetAsync(string key)
        {
            _store.TryGetValue(key, out var v);
            return Task.FromResult(v as string);
        }

        public Task<long?> GetCacheAgeMillisecondsAsync(string key)
        {
            return Task.FromResult<long?>(null);
        }

        public Task<DateTime?> GetExpirationTimeAsync(string key)
        {
            return Task.FromResult<DateTime?>(null);
        }

        public Task SetAsync(string key, string jsonPayload, TimeSpan? expiresIn = null)
        {
            _store[key] = jsonPayload;
            return Task.CompletedTask;
        }
    }
}
