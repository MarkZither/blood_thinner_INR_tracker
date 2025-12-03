using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mobile.UnitTests
{
    /// <summary>
    /// Migration tests verify EF Core migrations work correctly.
    /// Note: If model changes are made without adding a migration, these tests will fail.
    /// Run 'dotnet ef migrations add [MigrationName] --project src/BloodThinnerTracker.Data.SQLite --startup-project src/BloodThinnerTracker.Api' to fix.
    /// </summary>
    public class MigrationTests
    {
        /// <summary>
        /// Ensures the EF Core model has no pending changes that require a new migration.
        /// This prevents PendingModelChangesWarning at runtime.
        /// </summary>
        [Fact]
        public void Model_HasNoPendingChanges()
        {
            // Use in-memory SQLite for fast model validation
            var cs = new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ToString();
            using var connection = new SqliteConnection(cs);
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var logger = NullLogger<ApplicationDbContext>.Instance;
            using var db = new ApplicationDbContext(options, logger);

            // Get services needed for model comparison
            var modelDiffer = db.GetService<IMigrationsModelDiffer>();
            var migrationsAssembly = db.GetService<IMigrationsAssembly>();
            var designTimeModel = db.GetService<IDesignTimeModel>();

            // Get the snapshot model from the last migration
            var snapshot = migrationsAssembly.ModelSnapshot;
            Assert.NotNull(snapshot); // Ensure we have at least one migration

            // Finalize the snapshot model so it can be compared
            var modelInitializer = db.GetService<IModelRuntimeInitializer>();
            var snapshotModel = modelInitializer.Initialize(snapshot.Model);

            // Compare snapshot to current model using design-time model (required for migrations comparison)
            var differences = modelDiffer.GetDifferences(
                snapshotModel.GetRelationalModel(),
                designTimeModel.Model.GetRelationalModel());

            Assert.True(
                differences.Count == 0,
                $"Model has {differences.Count} pending change(s) requiring a migration. " +
                $"Run: dotnet ef migrations add <Name> --project src/BloodThinnerTracker.Data.SQLite --startup-project src/BloodThinnerTracker.Api");
        }

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
