using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{
    public class AuthExchangeRequest
    {
        public string Id_Token { get; set; } = string.Empty;
        public string Provider { get; set; } = "";
    }

    public class AuthExchangeResponse
    {
        public string Access_Token { get; set; } = string.Empty;
        public string Token_Type { get; set; } = "Bearer";
        public int Expires_In { get; set; }
        public DateTimeOffset Issued_At { get; set; }
    }

    /// <summary>
    /// Authentication service implementing OAuth 2.0 PKCE flow with external providers (Azure AD, Google).
    /// Exchanges OAuth id_token for an internal bearer token via the backend auth/exchange endpoint.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly IOAuthConfigService _oauthConfigService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;

        private const string AccessTokenKey = "inr_access_token";
        private const string IdTokenKey = "inr_id_token";

        private IPublicClientApplication? _msal;
        private OAuthConfig? _currentConfig;
        private string _currentProvider = "azure";

        public AuthService(
            ISecureStorageService secureStorage,
            IOAuthConfigService oauthConfigService,
            HttpClient httpClient,
            ILogger<AuthService> logger)
        {
            _secureStorage = secureStorage;
            _oauthConfigService = oauthConfigService;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Initiates OAuth PKCE flow to obtain an id_token from the configured external provider.
        /// Launches the system browser and captures the callback.
        /// </summary>
        /// <returns>The id_token if successful; empty string if flow was cancelled or failed.</returns>
        public async Task<string> SignInAsync()
        {
            try
            {
                _logger.LogInformation("Starting OAuth sign-in flow");

                // Fetch OAuth config from API
                _currentConfig = await _oauthConfigService.GetConfigAsync();
                if (_currentConfig?.Providers == null || _currentConfig.Providers.Count == 0)
                {
                    _logger.LogError("Failed to fetch OAuth config from API");
                    return string.Empty;
                }

                // For MVP, default to Azure AD; UI can allow provider selection later
                var providerConfig = _currentConfig.Providers.FirstOrDefault(p => p.Provider == "azure");
                if (providerConfig == null)
                {
                    _logger.LogError("Azure AD provider not configured");
                    return string.Empty;
                }

                _currentProvider = providerConfig.Provider;

                // Initialize MSAL public client application
                _msal = PublicClientApplicationBuilder
                    .Create(providerConfig.ClientId)
                    .WithAuthority(providerConfig.Authority)
                    .WithRedirectUri(providerConfig.RedirectUri)
                    .WithLogging((level, message, isPii) =>
                    {
                        if (!isPii)
                        {
                            _logger.LogDebug($"MSAL: {message}");
                        }
                    }, Microsoft.Identity.Client.LogLevel.Verbose, enablePiiLogging: false)
                    .Build();

                // Perform interactive authentication with PKCE (automatic in MSAL)
                var scopes = providerConfig.Scopes
                    .Split(' ')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                var authResult = await _msal.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();

                if (string.IsNullOrEmpty(authResult.IdToken))
                {
                    _logger.LogWarning("MSAL returned success but id_token was empty");
                    return string.Empty;
                }

                _logger.LogInformation("Successfully obtained id_token from external provider");
                return authResult.IdToken;
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
            {
                _logger.LogInformation("User cancelled OAuth sign-in");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth sign-in flow failed");
                return string.Empty;
            }
        }

        /// <summary>
        /// Exchanges an OAuth id_token for an internal bearer token via the backend.
        /// Stores the returned bearer token securely for API authentication.
        /// </summary>
        public async Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure")
        {
            if (string.IsNullOrEmpty(idToken))
            {
                _logger.LogWarning("ExchangeIdTokenAsync called with empty idToken");
                return false;
            }

            var req = new { id_token = idToken, provider };
            try
            {
                _logger.LogInformation("Exchanging id_token for internal bearer token");
                var resp = await _httpClient.PostAsJsonAsync("api/auth/exchange", req);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError($"Token exchange failed with status {resp.StatusCode}");
                    return false;
                }

                var body = await resp.Content.ReadFromJsonAsync<AuthExchangeResponse>();
                if (body == null)
                {
                    _logger.LogError("Token exchange response was null");
                    return false;
                }

                // Store internal bearer token securely
                await _secureStorage.SetAsync(AccessTokenKey, body.Access_Token);
                await _secureStorage.SetAsync(IdTokenKey, idToken);

                _logger.LogInformation("Successfully exchanged id_token and stored bearer token");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token exchange failed");
                return false;
            }
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await _secureStorage.GetAsync(AccessTokenKey);
        }

        public async Task SignOutAsync()
        {
            try
            {
                // Clear stored tokens
                await _secureStorage.RemoveAsync(AccessTokenKey);
                await _secureStorage.RemoveAsync(IdTokenKey);

                // Sign out from MSAL if initialized
                if (_msal != null)
                {
                    var accounts = await _msal.GetAccountsAsync();
                    foreach (var account in accounts)
                    {
                        await _msal.RemoveAsync(account);
                    }
                }

                _logger.LogInformation("User signed out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign out failed");
            }
        }
    }
}

