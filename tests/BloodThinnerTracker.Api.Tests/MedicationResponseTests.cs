using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class MedicationResponseTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void MedicationResponse_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<MedicationResponse>()
                .RuleFor(x => x.PublicId, f => f.Random.Guid())
                .RuleFor(x => x.Name, f => f.Lorem.Word())
                .RuleFor(x => x.Dosage, f => f.Random.Decimal(0.5m, 10m));

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.PublicId);
            Assert.False(string.IsNullOrWhiteSpace(dto.Name));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<MedicationResponse>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.PublicId, dto2!.PublicId);
            Assert.Equal(dto.Dosage, dto2.Dosage);
        }
    }
}
