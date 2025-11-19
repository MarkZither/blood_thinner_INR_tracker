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
                .RuleFor(x => x.DayOfWeek, f => f.Date.Weekday())
                .RuleFor(x => x.Dosage, f => f.Random.Decimal(0.5m, 10m))
                .RuleFor(x => x.PatternDay, f => f.Random.Int(1, 6))
                .RuleFor(x => x.PatternLength, f => f.Random.Int(1, 6))
                .RuleFor(x => x.IsPatternChange, f => true)
                .RuleFor(x => x.PatternChangeNote, f => f.Lorem.Sentence());

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.Date);
            Assert.False(string.IsNullOrWhiteSpace(dto.DayOfWeek));
            Assert.True(dto.IsPatternChange);
            Assert.False(string.IsNullOrWhiteSpace(dto.PatternChangeNote));

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<ScheduleEntry>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.Date, dto2!.Date);
            Assert.Equal(dto.DayOfWeek, dto2.DayOfWeek);
            Assert.Equal(dto.PatternChangeNote, dto2.PatternChangeNote);
        }
    }
}
