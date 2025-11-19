using System;
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
    public class MedicationLogsController_BasicTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;

        public MedicationLogsController_BasicTests()
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
        public async Task LogMedicationDose_CreatesEntry_And_GetMedicationLogs_ReturnsList()
        {
            // seed user and medication
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "loguser@example.com", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var med = new Medication { PublicId = Guid.NewGuid(), Name = "Dabigatran", UserId = user.Id, Dosage = 150m, DosageUnit = "mg", MaxDailyDose = 1000m, CreatedAt = DateTime.UtcNow };
            _db.Medications.Add(med);
            await _db.SaveChangesAsync();

            // prepare controller
            var controllerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<BloodThinnerTracker.Api.Controllers.MedicationLogsController>.Instance;
            var controller = new BloodThinnerTracker.Api.Controllers.MedicationLogsController(_db, controllerLogger);

            // set user principal
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()) }, "test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

            // call LogMedicationDose
            var request = new BloodThinnerTracker.Api.Controllers.LogMedicationRequest
            {
                MedicationId = med.PublicId,
                ActualDosage = 150m
            };

            var result = await controller.LogMedicationDose(request);
            Assert.IsType<CreatedAtActionResult>(result.Result);

            // verify DB entry
            var logs = await _db.MedicationLogs.Where(l => l.MedicationId == med.Id).ToListAsync();
            Assert.Single(logs);

            // call GetMedicationLogs
            var getResult = await controller.GetMedicationLogs(med.PublicId);
            Assert.IsType<OkObjectResult>(getResult.Result);
            var ok = getResult.Result as OkObjectResult;
            Assert.NotNull(ok?.Value);
        }
    }
}
