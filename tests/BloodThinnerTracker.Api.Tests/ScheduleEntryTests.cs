using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class ScheduleEntryTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };

        [Fact]
        public void ScheduleEntry_RoundTrip_Valid()
        {
            var dto = new ScheduleEntry
            {
                Date = DateTime.UtcNow.Date,
                DayOfWeek = DateTime.UtcNow.DayOfWeek.ToString(),
                Dosage = 2.0m,
                PatternDay = 1,
                PatternLength = 6,
                IsPatternChange = true,
                PatternChangeNote = "Adjusted dose"
            };

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
