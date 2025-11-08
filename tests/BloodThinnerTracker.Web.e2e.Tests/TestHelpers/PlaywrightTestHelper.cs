using System;

namespace BloodThinnerTracker.Web.e2e.Tests.TestHelpers
{
    public static class PlaywrightTestHelper
    {
        public static string BaseUrl => Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "http://localhost:5000";
    }
}
