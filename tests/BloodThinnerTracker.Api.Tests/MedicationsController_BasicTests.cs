using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BloodThinnerTracker.Data.SQLite;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationsController_BasicTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _db;

        public MedicationsController_BasicTests()
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
        public async Task GetMedications_ReturnsSeededMedication()
        {
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "test@example.com", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            var med = new Medication { PublicId = Guid.NewGuid(), Name = "Warfarin", User = user, Dosage = 5m, DosageUnit = "mg", CreatedAt = DateTime.UtcNow };
            _db.Medications.Add(med);
            await _db.SaveChangesAsync();

            var fetched = await _db.Medications.FirstOrDefaultAsync(m => m.PublicId == med.PublicId);
            Assert.NotNull(fetched);
            Assert.Equal("Warfarin", fetched.Name);
        }

        [Fact]
        public async Task GetMedication_NotFound_ReturnsNullInDb()
        {
            var rnd = Guid.NewGuid();
            var notFound = await _db.Medications.FirstOrDefaultAsync(m => m.PublicId == rnd);
            Assert.Null(notFound);
        }

        [Fact]
        public async Task CreateMedication_PersistsToDatabase()
        {
            var user = new User { Id = 1, PublicId = Guid.NewGuid(), Email = "creator@example.com", CreatedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var med = new Medication { PublicId = Guid.NewGuid(), Name = "Apixaban", UserId = user.Id, Dosage = 2.5m, DosageUnit = "mg", CreatedAt = DateTime.UtcNow };
            _db.Medications.Add(med);
            await _db.SaveChangesAsync();

            var persisted = await _db.Medications.FirstOrDefaultAsync(m => m.PublicId == med.PublicId);
            Assert.NotNull(persisted);
            Assert.Equal("Apixaban", persisted.Name);
            Assert.Equal(user.Id, persisted.UserId);
        }
    }
}
