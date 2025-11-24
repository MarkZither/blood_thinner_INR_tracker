using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
#if WINDOWS
using Microsoft.Identity.Client.Broker;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using WinRT.Interop;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{

    /// <summary>
    /// Authentication service implementing OAuth 2.0 PKCE flow with external providers (Azure AD, Google).
    /// Exchanges OAuth id_token for an internal bearer token via the backend auth/exchange endpoint.
    /// Supports both native browser and Windows Account Manager (WAM) broker authentication.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly IOAuthConfigService _oauthConfigService;
        private readonly IOptions<FeaturesOptions> _featuresOptions;
        private readonly ILogger<AuthService> _logger;

        private const string AccessTokenKey = "inr_access_token";
        private const string IdTokenKey = "inr_id_token";

        // Configuration: set to true to use WAM broker, false for native browser (default)
        // Change this to true to enable WAM broker instead of native browser
        private static bool UseWamBroker => false;

        private IPublicClientApplication? _msal;
        private OAuthConfig? _currentConfig;
        private string _currentProvider = "azure";

        public AuthService(
            ISecureStorageService secureStorage,
            IOAuthConfigService oauthConfigService,
            IOptions<FeaturesOptions> featuresOptions,
            ILogger<AuthService> logger)
        {
            _secureStorage = secureStorage;
            _oauthConfigService = oauthConfigService;
            _featuresOptions = featuresOptions;
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
                // UseWamBroker config determines authentication method:
                // - true: Windows Account Manager broker (in-app, more secure, requires broker config)
                // - false: Native system browser with localhost callback (default, simpler UX)
#if WINDOWS
                PublicClientApplicationBuilder builder;

                if (UseWamBroker)
                {
                    // Windows: Use WAM (Windows Account Manager) broker for better security and UX
                    // WAM handles the msal{client-id}://auth protocol automatically
                    // Must be registered in Azure AD: Authentication → Mobile and desktop applications
                    builder = PublicClientApplicationBuilder
                        .Create(providerConfig.ClientId)
                        .WithAuthority(providerConfig.Authority)
                        .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
                }
                else
                {
                    // Windows: Use native system browser with localhost callback (recommended for better UX)
                    // Redirect URI: http://localhost:7777 (must be registered in Azure AD)
                    builder = PublicClientApplicationBuilder
                        .Create(providerConfig.ClientId)
                        .WithAuthority(providerConfig.Authority)
                        .WithRedirectUri("http://localhost:7777");
                }
#else
                // Android/Other: Use localhost loopback (no WAM available)
                var builder = PublicClientApplicationBuilder
                    .Create(providerConfig.ClientId)
                    .WithAuthority(providerConfig.Authority)
                    .WithRedirectUri("http://localhost:7777");
#endif

                _msal = builder
                    .WithLogging((level, message, isPii) =>
                    {
                        if (!isPii)
                        {
                            _logger.LogDebug($"MSAL: {message}");
                        }
                    }, Microsoft.Identity.Client.LogLevel.Verbose, enablePiiLogging: false)
                    .Build();

#if WINDOWS
                if (UseWamBroker)
                {
                    _logger.LogInformation("MSAL initialized with WAM broker - ClientId: {ClientId}, Authority: {Authority}",
                        providerConfig.ClientId, providerConfig.Authority);
                }
                else
                {
                    _logger.LogInformation("MSAL initialized with native browser - ClientId: {ClientId}, Authority: {Authority}",
                        providerConfig.ClientId, providerConfig.Authority);
                }
#else
                _logger.LogInformation("MSAL initialized with localhost loopback - ClientId: {ClientId}, Authority: {Authority}",
                    providerConfig.ClientId, providerConfig.Authority);
#endif

                // Perform interactive authentication with PKCE (automatic in MSAL)
                var scopes = providerConfig.Scopes
                    .Split(' ')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                var authResult = await _msal.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
#if WINDOWS
                    .WithParentActivityOrWindow(UseWamBroker ? GetParentWindowHandle() : IntPtr.Zero)  // Window handle only needed for WAM
#endif
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
                _logger.LogInformation(ex, "User cancelled OAuth sign-in");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth sign-in flow failed");
                return string.Empty;
            }
        }

        /// <summary>
        /// Exchanges an OAuth id_token for an internal bearer token via the backend mobile endpoint.
        /// Server-side validates the id_token and returns internal JWT for API authentication.
        /// </summary>
        public async Task<bool> ExchangeIdTokenAsync(string idToken, string provider = "azure")
        {
            if (string.IsNullOrEmpty(idToken))
            {
                _logger.LogWarning("ExchangeIdTokenAsync called with empty idToken");
                return false;
            }

            try
            {
                _logger.LogInformation("Exchanging id_token for internal bearer token via mobile auth endpoint");

                // Get API root URL from strongly-typed options
                var apiRootUrl = _featuresOptions.Value.ApiRootUrl;
                if (string.IsNullOrEmpty(apiRootUrl) || apiRootUrl == "https://api.example.invalid")
                {
                    _logger.LogError("ApiRootUrl not configured or using placeholder value");
                    return false;
                }

                // Build URL to mobile auth endpoint: https://localhost:7235/api/auth/external/mobile
                var url = $"{apiRootUrl.TrimEnd('/')}/{ApiConstants.MobileAuthExchangePath}";

                // Build request matching ExternalLoginRequest model from API
                // Provider should be PascalCase (AzureAD, not azure)
                var request = new
                {
                    provider = provider == "azure" ? "AzureAD" : provider,
                    idToken = idToken,
                    deviceId = GetDeviceId(),
                    devicePlatform = GetDevicePlatform()
                };

                var httpClient = new HttpClient();
                var httpResp = await httpClient.PostAsJsonAsync(url, request);

                if (!httpResp.IsSuccessStatusCode)
                {
                    var errorContent = await httpResp.Content.ReadAsStringAsync();
                    _logger.LogError($"Token exchange failed with status {httpResp.StatusCode}: {errorContent}");
                    return false;
                }

                var body = await httpResp.Content.ReadFromJsonAsync<AuthenticationResponse>();
                if (body?.AccessToken == null)
                {
                    _logger.LogError("Token exchange response was null or missing AccessToken");
                    return false;
                }

                // Store internal bearer token securely
                await _secureStorage.SetAsync(AccessTokenKey, body.AccessToken);
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

        /// <summary>
        /// Gets a unique device identifier for security tracking.
        /// Uses device model or generates a random ID if not available.
        /// </summary>
        private string GetDeviceId()
        {
            try
            {
                // Use device model as identifier (Windows, iOS, Android model name)
                return DeviceInfo.Current.Model ?? Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Gets the current device platform name.
        /// </summary>
        private string GetDevicePlatform()
        {
            return DeviceInfo.Current.Platform.ToString();
        }

#if WINDOWS
        /// <summary>
        /// Gets the parent window handle for WAM (Windows Account Manager) integration.
        /// Required for MSAL on Windows when using the broker.
        /// </summary>
        private static IntPtr GetParentWindowHandle()
        {
            try
            {
                // Get the active window from the MAUI application
                var windows = Microsoft.Maui.Controls.Application.Current?.Windows;
                if (windows?.Count > 0)
                {
                    var window = windows[0];
                    // For MAUI on Windows, get the native window via the handler
                    if (window?.Handler is Microsoft.Maui.Handlers.WindowHandler windowHandler)
                    {
                        // WindowHandler.PlatformView gives us access to the native window
                        if (windowHandler.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                        {
                            return WindowNative.GetWindowHandle(nativeWindow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash if we can't get window handle
                System.Diagnostics.Debug.WriteLine($"Failed to get window handle: {ex.Message}");
            }
            return IntPtr.Zero;
        }
#endif
    }
}

