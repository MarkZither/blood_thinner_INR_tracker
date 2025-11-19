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
                .RuleFor(x => x.MedicationId, f => f.Random.Guid())
                .RuleFor(x => x.ActualDosage, f => (decimal?)f.Random.Decimal(0.5m, 10m))
                .RuleFor(x => x.ScheduledTime, f => f.Date.Recent().ToUniversalTime());

            var dto = faker.Generate();

            Assert.NotEqual(default, dto.MedicationId);
            Assert.InRange(dto.ActualDosage ?? 0m, 0.01m, 1000m);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<LogMedicationRequest>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.MedicationId, dto2!.MedicationId);
            Assert.Equal(dto.ActualDosage, dto2.ActualDosage);
        }
    }
}
