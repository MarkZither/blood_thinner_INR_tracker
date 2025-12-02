using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Collections;
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

            var logger = NullLogger<ApplicationDbContext>.Instance;

            return new ApplicationDbContext(options, logger);
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

        public static void ClearNavigationProperties<T>(T obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite) continue;

                var pt = prop.PropertyType;
                if (pt == typeof(string) || pt.IsValueType || pt.IsEnum) continue;

                // Clear collections and complex navigation properties to avoid deep graphs in tests
                if (typeof(IEnumerable).IsAssignableFrom(pt))
                {
                    prop.SetValue(obj, null);
                    continue;
                }

                prop.SetValue(obj, null);
            }
        }
    }
}
