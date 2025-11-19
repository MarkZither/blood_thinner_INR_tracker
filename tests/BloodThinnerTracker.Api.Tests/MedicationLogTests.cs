using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationLogTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void MedicationLogResponse_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<MedicationLogResponse>()
                .RuleFor(x => x.PublicId, f => f.Random.Guid())
                .RuleFor(x => x.ActualDosage, f => (decimal?)f.Random.Decimal(0.5m, 50m))
                .RuleFor(x => x.ScheduledTime, f => f.Date.Recent().ToUniversalTime())
                .RuleFor(x => x.ActualTime, (f, m) => m.ScheduledTime.AddMinutes(f.Random.Int(-30, 30)));

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.PublicId);
            Assert.InRange(dto.ActualDosage ?? 0m, 0.01m, 1000m);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<MedicationLogResponse>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.PublicId, dto2!.PublicId);
            Assert.Equal(dto.ActualDosage, dto2.ActualDosage);
        }
    }
}
