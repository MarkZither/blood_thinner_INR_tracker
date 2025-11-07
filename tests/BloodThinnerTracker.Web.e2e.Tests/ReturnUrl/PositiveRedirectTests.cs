using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Xunit;
using Microsoft.Playwright;
using BloodThinnerTracker.Web.e2e.Tests.TestHelpers;

namespace BloodThinnerTracker.Web.e2e.Tests.ReturnUrl
{
    public class PositiveRedirectTests
    {
        [Fact(Skip = "Playwright e2e deferred: run only in e2e feature branch or locally when enabled")]
        public async Task UnauthenticatedRequestShouldReturnToOriginalPageAfterLogin()
        {
            // Arrange: start Playwright and open a browser
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var baseUrl = PlaywrightTestHelper.BaseUrl.TrimEnd('/');

            // Act: Navigate to a protected page which should redirect to login
            await page.GotoAsync(baseUrl + "/medications/123");

            // Expect we are redirected to /login with returnUrl query param
            await page.WaitForURLAsync(new Regex("/login\\?returnUrl=.*", RegexOptions.Compiled));

            // Click the Sign in with Microsoft button (link rendered by MudButton with text)
            await page.ClickAsync("text=Sign in with Microsoft");

            // Simulate provider returning successfully: navigate to the oauth-complete callback URL
            var oauthComplete = $"{baseUrl}/oauth-complete?provider=microsoft&returnUrl=%2Fmedications%2F123";
            await page.GotoAsync(oauthComplete);

            // Wait for the app to redirect back to the medications page
            await page.WaitForURLAsync(new Regex("/medications/123$", RegexOptions.Compiled));

            // Assert: the Medications page shows the expected header
            var header = page.Locator("text=My Medications");
            Assert.True(await header.IsVisibleAsync(), "Expected Medications header to be visible after login redirect");

            // Cleanup
            await browser.CloseAsync();
        }
    }
}
