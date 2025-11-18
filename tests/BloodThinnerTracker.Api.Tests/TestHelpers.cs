using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Data.Shared;

namespace BloodThinnerTracker.Api.Tests
{
    internal static class TestHelpers
    {
        public static ApplicationDbContext CreateSqliteContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var dataProtection = new EphemeralDataProtectionProvider();
            var currentUser = new TestCurrentUserService();
            var logger = NullLogger<ApplicationDbContext>.Instance;

            return new ApplicationDbContext(options, dataProtection, currentUser, logger);
        }

        private class EphemeralDataProtectionProvider : IDataProtectionProvider
        {
            public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => new SimpleDataProtector();
        }

        private class SimpleDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtector
        {
            public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => plaintext ?? System.Array.Empty<byte>();
            public byte[] Unprotect(byte[] protectedData) => protectedData ?? System.Array.Empty<byte>();
        }

        private class TestCurrentUserService : ICurrentUserService
        {
            // Return a stable test user id so ApplicationDbContextBase validation passes for medical entities
            public int? GetCurrentUserId() => 1;
        }
    }
}
