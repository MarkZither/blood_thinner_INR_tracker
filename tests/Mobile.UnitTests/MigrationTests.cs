using System;
using System.IO;
using System.Threading.Tasks;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mobile.UnitTests
{
    public class MigrationTests
    {
        [Fact]
        public async Task Migrate_AppliesMigrations_And_INRTestsTableExists()
        {
            // Use a temporary SQLite file so tests are isolated
            var tmp = Path.Combine(Path.GetTempPath(), $"bt_inr_tests_{Guid.NewGuid():N}.db");
            try
            {
                var cs = new SqliteConnectionStringBuilder { DataSource = tmp }.ToString();
                await using var connection = new SqliteConnection(cs);
                await connection.OpenAsync();

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;

                // ApplicationDbContext constructor expects an ILogger
                var logger = NullLogger<ApplicationDbContext>.Instance;

                await using var db = new ApplicationDbContext(options, logger);

                // Apply migrations (this should create the INRTests table)
                await db.Database.MigrateAsync();

                // Ensure there are no pending migrations
                var pending = await db.Database.GetPendingMigrationsAsync();
                Assert.Empty(pending);

                // Ensure the INRTests DbSet can be queried (table exists). If table is missing this will throw.
                var any = await db.Set<INRTest>().AnyAsync();
                Assert.False(any); // table exists; likely empty on a fresh DB
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }
}
