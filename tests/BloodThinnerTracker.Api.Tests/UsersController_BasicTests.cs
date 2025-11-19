using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class UsersController_BasicTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;

        public UsersController_BasicTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _db = TestHelpers.CreateSqliteContext(_connection);
            _db.Database.EnsureCreated();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            _db.Dispose();
            _connection.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetCurrentUserProfile_ReturnsProfile_WhenUserExists()
        {
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "profile@example.com", FirstName = "Sam", LastName = "Smith", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BloodThinnerTracker.Api.Controllers.UsersController>.Instance;
            var controller = new BloodThinnerTracker.Api.Controllers.UsersController(_db, logger);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()) }, "test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

            var result = await controller.GetCurrentUserProfile();
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = result.Result as OkObjectResult;
            Assert.NotNull(ok?.Value);
            var profile = ok.Value as BloodThinnerTracker.Api.Controllers.UserProfileResponse;
            Assert.NotNull(profile);
            Assert.Equal(user.Email, profile.Email);
            Assert.Equal("Sam", profile.FirstName);
        }

        [Fact]
        public async Task UpdateCurrentUserProfile_UpdatesFields()
        {
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "update@example.com", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BloodThinnerTracker.Api.Controllers.UsersController>.Instance;
            var controller = new BloodThinnerTracker.Api.Controllers.UsersController(_db, logger);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()) }, "test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

            var request = new BloodThinnerTracker.Api.Controllers.UpdateUserProfileRequest
            {
                FirstName = "Updated",
                LastName = "User",
                PhoneNumber = "+1234567890",
                PreferredLanguage = "en-GB",
                TimeZone = "Europe/London",
                ReminderAdvanceMinutes = 30
            };

            var result = await controller.UpdateCurrentUserProfile(request);
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = result.Result as OkObjectResult;
            Assert.NotNull(ok?.Value);

            var refreshed = await _db.Users.FirstOrDefaultAsync(u => u.PublicId == user.PublicId);
            Assert.NotNull(refreshed);
            Assert.Equal("Updated", refreshed.FirstName);
            Assert.Equal("User", refreshed.LastName);
            Assert.Equal(30, refreshed.ReminderAdvanceMinutes);
            Assert.Equal("Europe/London", refreshed.TimeZone);
        }
    }
}
