using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MedicationScheduleController_BasicTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;

        public MedicationScheduleController_BasicTests()
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
        public async Task GetSchedule_WithActivePattern_ReturnsOkAndEntries()
        {
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "sched@example.com", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var med = new Medication { PublicId = Guid.NewGuid(), Name = "Rivaroxaban", UserId = user.Id, Dosage = 10m, DosageUnit = "mg", CreatedAt = DateTime.UtcNow };
            _db.Medications.Add(med);
            await _db.SaveChangesAsync();

            var pattern = new MedicationDosagePattern
            {
                MedicationId = med.Id,
                PatternSequence = new List<decimal> { 10m },
                StartDate = DateTime.UtcNow.Date
            };
            _db.MedicationDosagePatterns.Add(pattern);
            await _db.SaveChangesAsync();

            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BloodThinnerTracker.Api.Controllers.MedicationScheduleController>.Instance;
            var controller = new BloodThinnerTracker.Api.Controllers.MedicationScheduleController(_db, logger);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()) }, "test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

            var result = await controller.GetSchedule(med.PublicId, days: 3);
            Assert.IsType<OkObjectResult>(result.Result);

            var ok = result.Result as OkObjectResult;
            Assert.NotNull(ok?.Value);

            var response = ok.Value as BloodThinnerTracker.Shared.Models.MedicationScheduleResponse;
            Assert.NotNull(response);
            Assert.Equal(3, response.TotalDays);
            Assert.Equal(3, response.Schedule.Count);
        }
    }
}
