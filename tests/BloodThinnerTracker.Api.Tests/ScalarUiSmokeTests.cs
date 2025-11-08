using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BloodThinnerTracker.Api.Tests
{
    public class ScalarUiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ScalarUiSmokeTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task ScalarEndpoints_DoNotThrowAndReturnContent(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var resp = await client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body), "Response body should not be empty");

            // Ensure Developer Exception page / NRE traces are not present
            Assert.DoesNotContain("DeveloperExceptionPageMiddleware", body);
            Assert.DoesNotContain("NullReferenceException", body);
            Assert.DoesNotContain("System.NullReferenceException", body);

            // If this is the Scalar UI page, ensure the Quick Start header is present
            if (url == "/scalar/v1")
            {
                Assert.Contains("<h2 id=\"description/quick-start-get-your-token-30-seconds\">Quick Start: Get Your Token (30 seconds)</h2>", body);
            }
        }
    }
}
