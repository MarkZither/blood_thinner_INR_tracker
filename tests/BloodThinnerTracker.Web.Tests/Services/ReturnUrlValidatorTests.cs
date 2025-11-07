using Xunit;
using BloodThinnerTracker.Web.Services;

namespace BloodThinnerTracker.Web.Tests.Services
{
    public class ReturnUrlValidatorTests
    {
        [Fact]
        public void Validate_Returns_Normalized_For_Relative_Path()
        {
            var res = ReturnUrlValidator.Validate("/medications/123");
            Assert.True(res.IsValid);
            Assert.Equal("/medications/123", res.Normalized);
        }

        [Fact]
        public void Validate_Detects_DoubleEncoded()
        {
            // %252F -> %2F after first decode, so double-encoded leading slash
            var res = ReturnUrlValidator.Validate("%252F%252Fmalicious");
            Assert.False(res.IsValid);
            Assert.Equal("double-encoded", res.ValidationResultCode);
        }
    }
}
