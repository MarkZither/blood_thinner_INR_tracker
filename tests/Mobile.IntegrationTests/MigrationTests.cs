using System;
using System.IO;
using System.Threading.Tasks;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mobile.IntegrationTests
{
    public class MigrationTests
    {
        [Fact]
        public async Task Migrate_AppliesMigrations_And_INRTestsTableExists()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"bt_inr_integration_{Guid.NewGuid():N}.db");
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

                // Attempt to apply migrations with a short timeout pattern
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                await db.Database.MigrateAsync(cts.Token);

                var pending = await db.Database.GetPendingMigrationsAsync();
                Assert.Empty(pending);

                // Query the INRTests table to ensure it exists
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
