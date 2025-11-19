using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationLogTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void MedicationLogResponse_RoundTrip_Valid()
        {
            var dto = new MedicationLogResponse
            {
                PublicId = Guid.NewGuid(),
                MedicationPublicId = Guid.NewGuid(),
                MedicationName = "TestMed",
                ScheduledTime = DateTime.UtcNow.AddHours(-1),
                ActualTime = DateTime.UtcNow.AddMinutes(-55),
                ActualDosage = 4.5m,
                CreatedAt = DateTime.UtcNow
            };

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
