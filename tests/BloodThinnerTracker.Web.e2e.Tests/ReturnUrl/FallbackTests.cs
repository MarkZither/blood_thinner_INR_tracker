using System.Threading.Tasks;
using Xunit;
using Microsoft.Playwright;
using BloodThinnerTracker.Web.e2e.Tests.TestHelpers;

namespace BloodThinnerTracker.Web.e2e.Tests.ReturnUrl
{
    public class FallbackTests
    {
        [Fact(Skip = "Requires Playwright browsers. Enable locally to run end-to-end tests")]
        public async Task MissingReturnUrlShouldLandOnDashboard()
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            // Scaffold: perform direct login and assert landing page is /dashboard

            await browser.CloseAsync();
        }
    }
}
