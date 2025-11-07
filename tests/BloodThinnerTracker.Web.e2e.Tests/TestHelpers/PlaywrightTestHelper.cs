using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Web.e2e.Tests.TestHelpers
{
    public static class PlaywrightTestHelper
    {
        public static string BaseUrl => Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "http://localhost:5000";

        public static HttpClient CreateClient() => new HttpClient { BaseAddress = new Uri(BaseUrl) };

    }
}
