using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Xunit;
using BloodThinnerTracker.Shared.Models;

namespace BloodThinnerTracker.Api.Tests
{
    public class INRTestTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.IgnoreCycles };
        [Fact]
        public void IsInTargetRange_ReturnsTrue_WhenValueWithinRange()
        {
            var t = new INRTest
            {
                PublicId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow,
                INRValue = 2.5m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m
            };

            Assert.True(t.IsInTargetRange());
        }

        [Fact]
        public void GetDeviationFromTarget_ReturnsCenteredDifference()
        {
            var t = new INRTest
            {
                PublicId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow,
                INRValue = 3.0m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m
            };

            var deviation = t.GetDeviationFromTarget();
            // target center is (2.0+3.0)/2 = 2.5, so deviation should be 0.5
            Assert.Equal(0.5m, deviation);
        }

        [Fact]
        public void GetTherapeuticRangeCategory_DetectsCriticallyHigh()
        {
            var t = new INRTest
            {
                PublicId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow,
                INRValue = 5.0m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m
            };

            var cat = t.GetTherapeuticRangeCategory();
            Assert.Equal(TherapeuticRangeCategory.CriticallyHigh, cat);
            Assert.Contains("CRITICAL", t.GetRiskAssessment());
        }

        [Fact]
        public void ValidateForMedicalSafety_ReportsErrors_ForFutureDateAndCriticalINR()
        {
            var t = new INRTest
            {
                PublicId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow.AddDays(1), // future
                INRValue = 6.0m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m
            };

            var errors = t.ValidateForMedicalSafety();
            Assert.Contains(errors, e => e.Contains("future", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(errors, e => e.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetDisplayResult_IncludesTargetAndSymbol()
        {
            var t = new INRTest
            {
                PublicId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow,
                INRValue = 2.34m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m
            };

            var display = t.GetDisplayResult();
            Assert.Contains("INR: 2.34", display);
            Assert.Contains("Target: 2.0-3.0", display);
            Assert.Contains("✓", display);
        }

        [Fact]
        public void INRTestResponse_RoundTrip_SerializesAndDeserializes()
        {
            var resp = new INRTestResponse
            {
                PublicId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TestDate = DateTime.UtcNow,
                INRValue = 2.75m,
                TargetINRMin = 2.0m,
                TargetINRMax = 3.0m,
                Laboratory = "Acme Lab",
                Notes = "Sample",
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(resp, _opts);
            var round = JsonSerializer.Deserialize<INRTestResponse>(json, _opts);
            Assert.NotNull(round);
            Assert.Equal(resp.PublicId, round!.PublicId);
            Assert.Equal(resp.INRValue, round.INRValue);
            Assert.Equal(resp.TestDate.ToUniversalTime().Ticks, round.TestDate.ToUniversalTime().Ticks);
            Assert.Equal(resp.Laboratory, round.Laboratory);
        }
    }
}
