using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class CreateDosagePatternRequestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void CreateDosagePatternRequest_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<CreateDosagePatternRequest>()
                .RuleFor(x => x.PatternSequence, f => new List<decimal> { 4.0m, 4.0m, 3.0m })
                .RuleFor(x => x.StartDate, f => f.Date.Past().ToUniversalTime());

            var dto = faker.Generate();
            Assert.NotNull(dto.PatternSequence);
            Assert.True(dto.PatternSequence.Count > 0);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<CreateDosagePatternRequest>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.PatternSequence.Count, dto2!.PatternSequence.Count);
        }
    }
}
