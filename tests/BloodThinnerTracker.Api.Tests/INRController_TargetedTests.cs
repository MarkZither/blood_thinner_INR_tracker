using System;
using System.Collections.Generic;
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
    public class INRController_TargetedTests
    {
        [Fact]
        public async Task CreateINRTest_ReturnsCreatedAndPersists()
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

            var request = new CreateINRTestRequest
            {
                TestDate = DateTime.UtcNow,
                INRValue = 2.5m
            };

            var action = await controller.CreateINRTest(request);
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            var response = Assert.IsType<INRTestResponse>(created.Value);
            Assert.Equal(2.5m, response.INRValue);

            // Verify persisted
            var saved = await db.INRTests.FirstOrDefaultAsync(t => t.PublicId == response.PublicId);
            Assert.NotNull(saved);
            Assert.Equal(2.5m, saved!.INRValue);
        }

        [Fact]
        public async Task CreateINRTest_InvalidINR_ReturnsBadRequest()
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

            var request = new CreateINRTestRequest
            {
                TestDate = DateTime.UtcNow,
                INRValue = 0.1m // invalid, below 0.5
            };

            var action = await controller.CreateINRTest(request);
            Assert.IsType<BadRequestObjectResult>(action.Result);
        }

        [Fact]
        public async Task GetINRTest_NotFound_Returns404()
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

            var randomId = Guid.NewGuid();
            var result = await controller.GetINRTest(randomId);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetINRTests_ReturnsList()
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

            var t1 = new INRTest { PublicId = Guid.NewGuid(), UserId = user.Id, TestDate = DateTime.UtcNow.AddDays(-1), INRValue = 2.1m, CreatedAt = DateTime.UtcNow };
            var t2 = new INRTest { PublicId = Guid.NewGuid(), UserId = user.Id, TestDate = DateTime.UtcNow, INRValue = 2.6m, CreatedAt = DateTime.UtcNow };
            db.INRTests.AddRange(t1, t2);
            await db.SaveChangesAsync();

            var logger = provider.GetRequiredService<ILogger<INRController>>();
            var controller = new INRController(db, logger);

            var httpContext = new DefaultHttpContext();
            httpContext.User = TestPrincipal.WithPublicId(user.PublicId);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var action = await controller.GetINRTests();
            var ok = Assert.IsType<ActionResult<List<INRTestResponse>>>(action);
            var actionResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsType<List<INRTestResponse>>(actionResult.Value);
            Assert.Equal(2, list.Count);
        }

        // Reuse lightweight test helpers similar to other tests
        private class TestCurrentUserService : ICurrentUserService
        {
            public int? GetCurrentUserId() => null;
        }

        // No data protection helper required for these lightweight controller tests

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
