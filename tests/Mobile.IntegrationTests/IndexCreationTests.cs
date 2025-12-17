using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BloodThinnerTracker.Data.SQLite;

namespace Mobile.IntegrationTests
{
    public class IndexCreationTests
    {
        [Fact]
        public async Task MedicationIsActiveIndex_AppliedByMigrations()
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"bt_idx_{Guid.NewGuid():N}.db");
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

                // Apply migrations (should include our AddMedicationIsActiveIndex)
                await db.Database.MigrateAsync();

                // Query sqlite PRAGMA to list indexes for the Medications table
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA index_list('Medications');";
                await using var reader = await cmd.ExecuteReaderAsync();

                bool found = false;
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(1); // name column
                    if (string.Equals(name, "IX_Medications_IsActive", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found, "Expected IX_Medications_IsActive to exist after applying migrations");
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }
    }
}
