using System;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BloodThinnerTracker.Api.Tests
{
    public class AuditInterceptorTests
    {
        [Fact]
        public async Task Update_And_SoftDelete_Create_AuditRecords()
        {
            var services = new ServiceCollection();

            // Minimal services for ApplicationDbContext
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.AddScoped<ICurrentUserService, TestCurrentUserService>();
            services.AddScoped<AuditInterceptor>();

            // Provide logging so ApplicationDbContext can be activated (it requires ILogger<T>)
            services.AddLogging();

            // Attach the interceptor by using the overload that provides IServiceProvider
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite("Data Source=:memory:");
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            var sp = services.BuildServiceProvider();

            // Create DB and apply migrations
            using (var scope = sp.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var db = provider.GetRequiredService<ApplicationDbContext>();
                await db.Database.OpenConnectionAsync();
                await db.Database.EnsureCreatedAsync();

                // Add a user and an INRTest
                var user = new User { PublicId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
                provider.GetRequiredService<ApplicationDbContext>().Users.Add(user);
                await db.SaveChangesAsync();

                var test = new INRTest
                {
                    PublicId = Guid.NewGuid(),
                    UserId = user.Id,
                    TestDate = DateTime.UtcNow,
                    INRValue = 2.5m
                };

                db.INRTests.Add(test);
                await db.SaveChangesAsync();

                // Update the INRTest and set UpdatedBy
                var updated = await db.INRTests.FirstAsync(t => t.Id == test.Id);
                updated.INRValue = 3.1m;
                updated.UpdatedBy = user.PublicId;
                await db.SaveChangesAsync();

                // Soft-delete path: set IsDeleted and DeletedBy
                var toDelete = await db.INRTests.FirstAsync(t => t.Id == test.Id);
                toDelete.IsDeleted = true;
                toDelete.DeletedBy = user.PublicId;
                await db.SaveChangesAsync();

                // Verify audit records
                var audits = await db.Set<AuditRecord>().Where(a => a.EntityType == nameof(INRTest) && a.EntityPublicId == test.PublicId).ToListAsync();
                Assert.True(audits.Count >= 2, "Expected at least two audit records (update + delete)");
                Assert.Contains(audits, a => a.BeforeJson != null && a.AfterJson != null && a.PerformedBy == user.PublicId);
            }
        }

        // Minimal test helpers
        // Lightweight IDataProtectionProvider for tests - performs no encryption, just returns bytes as-is.
        private class EphemeralDataProtectionProvider : IDataProtectionProvider
        {
            public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => new SimpleDataProtector();
        }

        private class SimpleDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtector
        {
            public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => plaintext ?? Array.Empty<byte>();
            public byte[] Unprotect(byte[] protectedData) => protectedData ?? Array.Empty<byte>();
        }

        private class TestCurrentUserService : ICurrentUserService
        {
            public int? GetCurrentUserId() => null;
        }
    }
}
