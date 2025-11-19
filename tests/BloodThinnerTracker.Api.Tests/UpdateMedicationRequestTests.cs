using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class UpdateMedicationRequestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void UpdateMedicationRequest_RoundTrip_Valid()
        {
            var dto = new UpdateMedicationRequest
            {
                Name = "UpdatedName",
                Dosage = 7.5m
            };

            Assert.False(string.IsNullOrWhiteSpace(dto.Name));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<UpdateMedicationRequest>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.Name, dto2!.Name);
            Assert.Equal(dto.Dosage, dto2.Dosage);
        }
    }
}
