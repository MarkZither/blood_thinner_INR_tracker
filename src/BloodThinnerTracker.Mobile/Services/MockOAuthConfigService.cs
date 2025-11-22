using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Mock OAuth configuration service for development/testing.
    /// Returns hardcoded OAuth provider configuration without API calls.
    /// </summary>
    public class MockOAuthConfigService : IOAuthConfigService
    {
        /// <summary>
        /// Returns mock OAuth configuration for Azure AD and Google.
        /// </summary>
        public Task<OAuthConfig?> GetConfigAsync(CancellationToken cancellationToken = default)
        {
            var config = new OAuthConfig
            {
                Providers = new List<OAuthProviderConfig>
                {
                    new OAuthProviderConfig
                    {
                        Provider = "azure",
                        ClientId = "00000000-0000-0000-0000-000000000000",
                        Authority = "https://login.microsoftonline.com/common/v2.0",
                        RedirectUri = "msal00000000-0000-0000-0000-000000000000://auth",
                        Scopes = "openid profile email"
                    },
                    new OAuthProviderConfig
                    {
                        Provider = "google",
                        ClientId = "000000000000-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx.apps.googleusercontent.com",
                        Authority = "https://accounts.google.com",
                        RedirectUri = "com.googleusercontent.apps.000000000000-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx://oauth2callback",
                        Scopes = "openid profile email"
                    }
                }
            };

            return Task.FromResult<OAuthConfig?>(config);
        }
    }
}
