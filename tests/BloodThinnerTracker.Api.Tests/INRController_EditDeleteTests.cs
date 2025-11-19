using System;
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
    public class INRController_EditDeleteTests
    {
        [Fact]
        public async Task UpdateINRTest_Succeeds_AndSetsUpdatedBy()
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

            var update = new UpdateINRTestRequest { INRValue = 3.33m };

            var result = await controller.UpdateINRTest(test.PublicId, update);
            var ok = Assert.IsType<ActionResult<INRTestResponse>>(result);
            var actionResult = Assert.IsType<OkObjectResult>(ok.Result);
            var response = Assert.IsType<INRTestResponse>(actionResult.Value);

            Assert.Equal(3.33m, response.INRValue);

            var refreshed = await db.INRTests.FirstAsync(t => t.PublicId == test.PublicId);
            Assert.Equal(user.PublicId, refreshed.UpdatedBy);
        }

        [Fact]
        public async Task UpdateINRTest_NotFound_Returns404()
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

            var logger = provider.GetRequiredService<ILogger<INRController>>();
            var controller = new INRController(db, logger);

            var httpContext = new DefaultHttpContext();
            httpContext.User = TestPrincipal.WithPublicId(user.PublicId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.UpdateINRTest(Guid.NewGuid(), new UpdateINRTestRequest { INRValue = 2.5m });
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteINRTest_NotFound_Returns404()
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

            var logger = provider.GetRequiredService<ILogger<INRController>>();
            var controller = new INRController(db, logger);

            var httpContext = new DefaultHttpContext();
            httpContext.User = TestPrincipal.WithPublicId(user.PublicId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.DeleteINRTest(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
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
