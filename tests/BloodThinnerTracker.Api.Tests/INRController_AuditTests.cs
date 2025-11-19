using System;
using System.Linq;
using System.Threading.Tasks;
using BloodThinnerTracker.Api.Controllers;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BloodThinnerTracker.Api.Tests
{
    public class INRController_AuditTests
    {
        [Fact]
        public async Task UpdateINRTest_Creates_AuditRecord()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUserService, TestCurrentUserService>();
            services.AddSingleton<AuditInterceptor>();
            services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
                Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("tests"));

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite("Data Source=:memory:");
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var provider = scope.ServiceProvider;
            var db = provider.GetRequiredService<ApplicationDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var user = new User { PublicId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var test = new INRTest
            {
                PublicId = Guid.NewGuid(),
                UserId = user.Id,
                TestDate = DateTime.UtcNow,
                INRValue = 2.2m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.INRTests.Add(test);
            await db.SaveChangesAsync();

            var logger = provider.GetRequiredService<ILogger<INRController>>();
            var controller = new INRController(db, logger);

            var httpContext = new DefaultHttpContext();
            httpContext.User = TestPrincipal.WithPublicId(user.PublicId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Perform update via controller
            var update = new UpdateINRTestRequest { INRValue = 3.14m };
            var result = await controller.UpdateINRTest(test.PublicId, update);
            var ok = Assert.IsType<ActionResult<INRTestResponse>>(result);
            Assert.IsType<OkObjectResult>(ok.Result);

            // Verify an AuditRecord exists for the update
            var audits = await db.Set<AuditRecord>().Where(a => a.EntityType == nameof(INRTest) && a.EntityPublicId == test.PublicId).ToListAsync();
            Assert.True(audits.Count >= 1, "Expected an audit record for the update");
            Assert.Contains(audits, a => a.PerformedBy == user.PublicId);
        }

        [Fact]
        public async Task DeleteINRTest_Creates_AuditRecord()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUserService, TestCurrentUserService>();
            services.AddSingleton<AuditInterceptor>();
            services.AddSingleton<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(
                Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("tests"));

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlite("Data Source=:memory:");
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var provider = scope.ServiceProvider;
            var db = provider.GetRequiredService<ApplicationDbContext>();
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();

            var user = new User { PublicId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var test = new INRTest
            {
                PublicId = Guid.NewGuid(),
                UserId = user.Id,
                TestDate = DateTime.UtcNow,
                INRValue = 2.2m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.INRTests.Add(test);
            await db.SaveChangesAsync();

            var logger = provider.GetRequiredService<ILogger<INRController>>();
            var controller = new INRController(db, logger);

            var httpContext = new DefaultHttpContext();
            httpContext.User = TestPrincipal.WithPublicId(user.PublicId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var deleteResult = await controller.DeleteINRTest(test.PublicId);
            Assert.IsType<NoContentResult>(deleteResult);

            var audits = await db.Set<AuditRecord>().Where(a => a.EntityType == nameof(INRTest) && a.EntityPublicId == test.PublicId).ToListAsync();
            Assert.True(audits.Count >= 1, "Expected an audit record for the delete");
            Assert.Contains(audits, a => a.PerformedBy == user.PublicId && a.AfterJson != null);
        }

        private class TestCurrentUserService : ICurrentUserService
        {
            public int? GetCurrentUserId() => null;
        }

        private static class TestPrincipal
        {
            public static System.Security.Claims.ClaimsPrincipal WithPublicId(Guid publicId)
            {
                var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, publicId.ToString()) };
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
                return new System.Security.Claims.ClaimsPrincipal(identity);
            }
        }
    }
}
