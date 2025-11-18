using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class LogMedicationRequestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void LogMedicationRequest_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<LogMedicationRequest>()
                .RuleFor(x => x.MedicationPublicId, f => f.Random.Guid())
                .RuleFor(x => x.Dosage, f => f.Random.Decimal(0.5m, 10m))
                .RuleFor(x => x.ScheduledTime, f => f.Date.Recent().ToUniversalTime());

            var dto = faker.Generate();

            Assert.NotEqual(default, dto.MedicationPublicId);
            Assert.InRange(dto.Dosage, 0.01m, 1000m);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<LogMedicationRequest>(json, _opts);
            Assert.Equal(dto.MedicationPublicId, dto2.MedicationPublicId);
            Assert.Equal(dto.Dosage, dto2.Dosage);
        }
    }
}
