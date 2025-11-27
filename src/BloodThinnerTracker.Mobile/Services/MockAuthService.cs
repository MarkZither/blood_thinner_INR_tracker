using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Mock authentication service for development/testing.
    /// Returns hardcoded tokens without OAuth flow or backend calls.
    /// Allows rapid iteration on UI and data flows without real OAuth setup.
    /// </summary>
    public class MockAuthService : IAuthService
    {
        private readonly ILogger<MockAuthService> _logger;
        private string? _bearerToken;

        private const string AccessTokenKey = "inr_access_token_mock";
        private const string IdTokenKey = "inr_id_token_mock";

        public MockAuthService(ILogger<MockAuthService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simulates OAuth sign-in by returning a mock id_token immediately.
        /// No actual OAuth flow or browser interaction.
        /// </summary>
        public Task<string> SignInAsync()
        {
            _logger.LogInformation("Mock SignInAsync: Returning mock id_token");

            // Return a mock JWT-like id_token (not a real JWT, just for testing)
            var mockIdToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtb2NrLXVzZXItMTIzIiwibmFtZSI6Ik1vY2sgVXNlciIsImVtYWlsIjoibW9ja0BleGFtcGxlLmNvbSIsImlhdCI6MTcwMDY0NDYwMCwiZXhwIjoxODAwNjQ0NjAwfQ.mock-signature";

            return Task.FromResult(mockIdToken);
        }

        /// <summary>
        /// Simulates token exchange by returning a mock bearer token immediately.
        /// Stores token in memory (not securely, but fine for mock).
        /// </summary>
        public Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure")
        {
            if (string.IsNullOrEmpty(idToken))
            {
                _logger.LogWarning("Mock ExchangeIdTokenAsync: idToken was empty");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Mock ExchangeIdTokenAsync: Exchanging id_token for mock bearer token (provider={Provider})", provider);

            // Generate a mock bearer token
            _bearerToken = $"mock-bearer-token-{Guid.NewGuid():N}";

            _logger.LogInformation("Mock ExchangeIdTokenAsync: Successfully exchanged and stored mock bearer token");
            return Task.FromResult(true);
        }

        public Task<string?> GetAccessTokenAsync()
        {
            _logger.LogDebug("Mock GetAccessTokenAsync: Returning {Token}", _bearerToken ?? "(null)");
            return Task.FromResult(_bearerToken);
        }

        public Task<bool> RefreshAccessTokenAsync()
        {
            _logger.LogInformation("Mock RefreshAccessTokenAsync: Rotating mock bearer token");
            // Simulate rotation by generating a new mock token
            _bearerToken = $"mock-bearer-token-{Guid.NewGuid():N}";
            return Task.FromResult(true);
        }

        public Task SignOutAsync()
        {
            _logger.LogInformation("Mock SignOutAsync: Clearing mock tokens");
            _bearerToken = null;
            return Task.CompletedTask;
        }
    }
}
