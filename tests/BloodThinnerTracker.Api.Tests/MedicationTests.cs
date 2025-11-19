using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void MedicationResponse_RoundTrip_Valid()
        {
            var dto = new MedicationResponse
            {
                PublicId = Guid.NewGuid(),
                Name = "TestMed",
                Dosage = 5.0m,
                StartDate = DateTime.UtcNow.AddDays(-10)
            };

            Assert.NotEqual(default, dto.PublicId);
            Assert.False(string.IsNullOrWhiteSpace(dto.Name));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<MedicationResponse>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.PublicId, dto2!.PublicId);
            Assert.Equal(dto.Name, dto2.Name);
        }
    }
}
