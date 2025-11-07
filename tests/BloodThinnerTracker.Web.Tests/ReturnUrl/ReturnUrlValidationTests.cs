using BloodThinnerTracker.Web.Services;
using Xunit;

namespace BloodThinnerTracker.Web.Tests.ReturnUrl;

public class ReturnUrlValidationTests
{
    [Theory]
    [InlineData(null, false, "missing")]
    [InlineData("", false, "missing")]
    [InlineData("https://evil.com", false, "invalid-scheme")]
    [InlineData("//evil.com", false, "protocol-relative")]
    [InlineData("%252F%2Fevil.com", false, "double-encoded")]
    [InlineData("/settings", true, null)]
    [InlineData("%2Fsettings%3Ftab%3Dnotifications", true, null)]
    public void Validate_ReturnUrlVariousCases(string? raw, bool expectedValid, string? expectedCode)
    {
        var result = ReturnUrlValidator.Validate(raw);
        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedCode, result.ValidationResultCode);
        if (expectedValid)
        {
            Assert.NotNull(result.Normalized);
            Assert.StartsWith("/", result.Normalized);
        }
    }
}
