using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class ScheduleEntryTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void ScheduleEntry_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<ScheduleEntry>()
                .RuleFor(x => x.Date, f => f.Date.Recent().ToUniversalTime())
                .RuleFor(x => x.Note, f => f.Lorem.Sentence());

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.Date);
            Assert.False(string.IsNullOrWhiteSpace(dto.Note));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<ScheduleEntry>(json, _opts);
            Assert.Equal(dto.Date, dto2.Date);
            Assert.Equal(dto.Note, dto2.Note);
        }
    }
}
