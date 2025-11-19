using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class LogMedicationRequestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void LogMedicationRequest_RoundTrip_Valid()
        {
            var dto = new LogMedicationRequest
            {
                MedicationId = Guid.NewGuid(),
                ActualDosage = 2.0m,
                ScheduledTime = DateTime.UtcNow.AddHours(-2)
            };

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
