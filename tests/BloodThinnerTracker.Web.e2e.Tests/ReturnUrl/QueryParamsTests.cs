using System.Threading.Tasks;
using Xunit;
using Microsoft.Playwright;
using BloodThinnerTracker.Web.e2e.Tests.TestHelpers;

namespace BloodThinnerTracker.Web.e2e.Tests.ReturnUrl
{
    public class QueryParamsTests
    {
        [Fact(Skip = "Requires Playwright browsers. Enable locally to run end-to-end tests")]
        public async Task QueryParametersShouldBePreservedAfterLogin()
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            // Scaffold: navigate to /inr-tests?filter=recent, follow login flow, assert final URL includes ?filter=recent

            await browser.CloseAsync();
        }
    }
}
