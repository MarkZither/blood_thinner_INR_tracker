using System;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mobile.IntegrationTests
{
    public class ForegroundSyncE2ETests
    {
        [Fact]
        public async Task EnsureForegroundSyncAsync_TriggersFetchAndPersistsData()
        {
            var ct = System.Threading.CancellationToken.None;

            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BloodThinnerTracker_AppHost>(ct);
            var app = await appHost.BuildAsync(ct);
            await app.StartAsync(ct);

            await app.ResourceNotifications.WaitForResourceHealthyAsync("api", ct).WaitAsync(TimeSpan.FromSeconds(30), ct);

            using (var scope = app.Services.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var db = sp.GetRequiredService<BloodThinnerTracker.Data.Shared.IApplicationDbContext>();

                var user = new BloodThinnerTracker.Shared.Models.User
                {
                    PublicId = Guid.NewGuid(),
                    Email = "fg-e2e@local",
                    Name = "FG E2E User",
                    AuthProvider = "Test",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var med = new BloodThinnerTracker.Shared.Models.Medication
                {
                    PublicId = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = "FG-Med",
                    Dosage = 2.0m,
                    DosageUnit = "mg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                db.Medications.Add(med);

                var inr = new BloodThinnerTracker.Shared.Models.INRTest
                {
                    PublicId = Guid.NewGuid(),
                    UserId = user.Id,
                    TestDate = DateTime.UtcNow,
                    InrValue = 2.1m,
                    CreatedAt = DateTime.UtcNow
                };
                db.INRTests.Add(inr);

                await db.SaveChangesAsync();

                var jwtService = sp.GetRequiredService<BloodThinnerTracker.Api.Services.Authentication.IJwtTokenService>();
                var userInfo = new BloodThinnerTracker.Shared.Models.Authentication.UserInfo
                {
                    PublicId = user.PublicId,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role ?? "User",
                    Provider = "Test",
                    TimeZone = "UTC"
                };
                var token = jwtService.GenerateAccessToken(userInfo, new[] { "inr:read", "medication:read" });

                using var http = app.CreateHttpClient("api");
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var auth = new FakeAuthService(token);
                var apiClient = new BloodThinnerTracker.Mobile.Services.ApiHttpClientFacade(http, auth, NullLogger<BloodThinnerTracker.Mobile.Services.ApiHttpClientFacade>.Instance);

                // Local DB
                var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bt_fg_e2e_{Guid.NewGuid():N}.db");
                try
                {
                    var cs = new SqliteConnectionStringBuilder { DataSource = tmp }.ToString();
                    await using var connection = new SqliteConnection(cs);
                    await connection.OpenAsync();

                    var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>()
                        .UseSqlite(connection)
                        .Options;

                    await using var localDb = new BloodThinnerTracker.Data.SQLite.ApplicationDbContext(options, NullLogger<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>.Instance);
                    await localDb.Database.MigrateAsync();

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
                    localDb.Users.Add(localUser);
                    await localDb.SaveChangesAsync();
                    localDb.CurrentUserId = localUser.Id;
                    await localDb.SaveChangesAsync();

                    var medRepo = new BloodThinnerTracker.Mobile.Services.MedicationRepository(localDb, null, NullLogger<BloodThinnerTracker.Mobile.Services.MedicationRepository>.Instance);
                    var medLogRepo = new BloodThinnerTracker.Mobile.Services.MedicationLogRepository(localDb, null);
                    var inrRepo = new BloodThinnerTracker.Mobile.Services.InrRepository(localDb, null);
                    var cache = new TestCacheService();

                    // Set last sync to be old so ForegroundSync triggers a sync
                    await cache.SetAsync("last_sync_utc", DateTime.UtcNow.AddHours(-5).ToString("o"));

                    var cacheSync = new BloodThinnerTracker.Mobile.Services.CacheSyncService(apiClient, inrRepo, cache, medRepo, medLogRepo, Microsoft.Extensions.Options.Options.Create(new BloodThinnerTracker.Mobile.Config.SyncOptions()), NullLogger<BloodThinnerTracker.Mobile.Services.CacheSyncService>.Instance);

                    var fg = new BloodThinnerTracker.Mobile.Services.ForegroundSyncService(cache, cacheSync, Microsoft.Extensions.Options.Options.Create(new BloodThinnerTracker.Mobile.Config.SyncOptions()), NullLogger<BloodThinnerTracker.Mobile.Services.ForegroundSyncService>.Instance);

                    await fg.EnsureForegroundSyncAsync();

                    var medCount = await localDb.Medications.CountAsync();
                    var inrCount = await localDb.INRTests.CountAsync();

                    Assert.True(medCount >= 1, "Expected medications from API to be persisted locally via foreground sync");
                    Assert.True(inrCount >= 1, "Expected INR tests from API to be persisted locally via foreground sync");
                }
                finally
                {
                    try { System.IO.File.Delete(tmp); } catch { }
                }
            }

            await app.StopAsync();
            await app.DisposeAsync();
        }

        class FakeAuthService : BloodThinnerTracker.Mobile.Services.IAuthService
        {
            private readonly string _token;
            public FakeAuthService(string token) => _token = token;
            public Task<string> SignInAsync() => Task.FromResult(_token);
            public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure") => Task.FromResult(true);
            public Task<bool> RefreshAccessTokenAsync() => Task.FromResult(true);
            public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(_token);
            public Task SignOutAsync() => Task.CompletedTask;
        }
    }
}
