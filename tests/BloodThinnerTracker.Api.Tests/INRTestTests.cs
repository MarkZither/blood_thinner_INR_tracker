using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class INRTestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void INRTest_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<INRTest>()
                .RuleFor(x => x.PublicId, f => f.Random.Guid())
                .RuleFor(x => x.TestDate, f => f.Date.Recent().ToUniversalTime())
                .RuleFor(x => x.INRValue, f => f.Random.Decimal(0.5m, 8.0m));

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.PublicId);
            Assert.InRange(dto.INRValue, 0.1m, 100m);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<INRTest>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.PublicId, dto2!.PublicId);
            Assert.Equal(dto.INRValue, dto2.INRValue);
        }
    }
}
