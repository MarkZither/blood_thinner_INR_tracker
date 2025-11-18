using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationLogTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void MedicationLog_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<MedicationLog>()
                .RuleFor(x => x.PublicId, f => f.Random.Guid())
                .RuleFor(x => x.Dosage, f => f.Random.Decimal(0.5m, 50m))
                .RuleFor(x => x.ScheduledTime, f => f.Date.Recent().ToUniversalTime())
                .RuleFor(x => x.ActualTime, (f, m) => m.ScheduledTime.AddMinutes(f.Random.Int(-30, 30)));

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.PublicId);
            Assert.InRange(dto.Dosage, 0.01m, 1000m);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<MedicationLog>(json, _opts);
            Assert.Equal(dto.PublicId, dto2.PublicId);
            Assert.Equal(dto.Dosage, dto2.Dosage);
        }
    }
}
