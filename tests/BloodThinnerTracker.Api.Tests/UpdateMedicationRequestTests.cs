using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class UpdateMedicationRequestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void UpdateMedicationRequest_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<UpdateMedicationRequest>()
                .RuleFor(x => x.Name, f => f.Commerce.ProductName())
                .RuleFor(x => x.Dosage, f => (decimal?)f.Random.Decimal(0.5m, 100m));

            var dto = faker.Generate();
            Assert.False(string.IsNullOrWhiteSpace(dto.Name));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<UpdateMedicationRequest>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.Name, dto2!.Name);
            Assert.Equal(dto.Dosage, dto2.Dosage);
        }
    }
}
