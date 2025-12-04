using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
        private readonly BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService? _telemetry;

        private const string AccessTokenKey = "inr_access_token";
        private const string IdTokenKey = "inr_id_token";
        private const string RefreshTokenKey = "inr_refresh_token";
        private const string AccessTokenExpiresAtKey = "inr_access_token_expires_at"; // ISO-8601 UTC

        // Configuration: set to true to use WAM broker, false for native browser (default)
        // Change this to true to enable WAM broker instead of native browser
        private static bool UseWamBroker => false;

        private IPublicClientApplication? _msal;
        private OAuthConfig? _currentConfig;
        private string _currentProvider = "azure";
        private readonly HttpClient _httpClient;

        public AuthService(
            ISecureStorageService secureStorage,
            IOAuthConfigService oauthConfigService,
            IOptions<FeaturesOptions> featuresOptions,
            ILogger<AuthService> logger,
            HttpClient? httpClient = null,
            BloodThinnerTracker.Mobile.Services.Telemetry.ITelemetryService? telemetry = null)
        {
            _secureStorage = secureStorage;
            _oauthConfigService = oauthConfigService;
            _featuresOptions = featuresOptions;
            _logger = logger;
            _telemetry = telemetry;
            _httpClient = httpClient ?? new HttpClient();
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

                var swExchange = System.Diagnostics.Stopwatch.StartNew();
                var httpResp = await _httpClient.PostAsJsonAsync(url, request);

                if (!httpResp.IsSuccessStatusCode)
                {
                    var errorContent = await httpResp.Content.ReadAsStringAsync();
                    _logger.LogError($"Token exchange failed with status {httpResp.StatusCode}: {errorContent}");
                    _telemetry?.TrackEvent("Auth.ExchangeFailed", new Dictionary<string, string> { { "Status", httpResp.StatusCode.ToString() } });
                    return false;
                }

                // Read raw JSON to capture accessToken + optional refreshToken and expiresIn
                var content = await httpResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.TryGetProperty("accessToken", out var accessTokenProp) && !root.TryGetProperty("access_token", out accessTokenProp))
                {
                    _logger.LogError("Token exchange response was missing accessToken");
                    return false;
                }

                var accessToken = accessTokenProp.GetString();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Token exchange response contained empty accessToken");
                    return false;
                }

                // Optional fields
                string? refreshToken = null;
                if (root.TryGetProperty("refreshToken", out var refreshProp) || root.TryGetProperty("refresh_token", out refreshProp))
                {
                    refreshToken = refreshProp.GetString();
                }

                // expiresIn can be seconds (int) or string
                DateTimeOffset? expiresAt = null;
                if (root.TryGetProperty("expiresIn", out var expiresProp) || root.TryGetProperty("expires_in", out expiresProp))
                {
                    if (expiresProp.ValueKind == JsonValueKind.Number && expiresProp.TryGetInt64(out var secs))
                    {
                        expiresAt = DateTimeOffset.UtcNow.AddSeconds(secs);
                    }
                    else if (expiresProp.ValueKind == JsonValueKind.String && long.TryParse(expiresProp.GetString(), out var secs2))
                    {
                        expiresAt = DateTimeOffset.UtcNow.AddSeconds(secs2);
                    }
                }

                // Store internal bearer token and related metadata securely
                await _secureStorage.SetAsync(AccessTokenKey, accessToken);
                await _secureStorage.SetAsync(IdTokenKey, idToken);
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _secureStorage.SetAsync(RefreshTokenKey, refreshToken);
                }
                if (expiresAt.HasValue)
                {
                    await _secureStorage.SetAsync(AccessTokenExpiresAtKey, expiresAt.Value.ToString("o"));
                }

                swExchange.Stop();
                _telemetry?.TrackHistogram("Auth.ExchangeMs", swExchange.Elapsed.TotalMilliseconds);
                _telemetry?.TrackEvent("Auth.ExchangeSucceeded", new Dictionary<string, string> { { "HasRefresh", (!string.IsNullOrEmpty(refreshToken)).ToString() } });
                _logger.LogInformation("Successfully exchanged id_token and stored bearer token (refresh available={HasRefresh})", !string.IsNullOrEmpty(refreshToken));
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
            // Check expiry and proactively refresh if close to expiration.
            var accessToken = await _secureStorage.GetAsync(AccessTokenKey);
            if (string.IsNullOrEmpty(accessToken)) return null;

            try
            {
                var expiresAtStr = await _secureStorage.GetAsync(AccessTokenExpiresAtKey);
                if (!string.IsNullOrEmpty(expiresAtStr) && DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
                {
                    var now = DateTimeOffset.UtcNow;
                    // If token expires within 5 minutes, attempt refresh
                    if (expiresAt - now < TimeSpan.FromMinutes(5))
                    {
                        _logger.LogInformation("Access token expiring soon (at {ExpiresAt}). Attempting refresh.", expiresAt);
                        _telemetry?.TrackEvent("Auth.ProactiveRefreshAttempt");
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var ok = await RefreshAccessTokenAsync();
                        sw.Stop();
                        _telemetry?.TrackHistogram("Auth.ProactiveRefreshMs", sw.Elapsed.TotalMilliseconds);
                        if (ok)
                        {
                            accessToken = await _secureStorage.GetAsync(AccessTokenKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check/refresh access token");
            }

            return accessToken;
        }

        /// <summary>
        /// Uses the stored refresh token to obtain a new access token from the backend.
        /// Stores rotated refresh token and new expiry if provided.
        /// </summary>
        public async Task<bool> RefreshAccessTokenAsync()
        {
            try
            {
                var refreshToken = await _secureStorage.GetAsync(RefreshTokenKey);
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogDebug("No refresh token available to refresh access token");
                    return false;
                }

                var apiRootUrl = _featuresOptions.Value.ApiRootUrl;
                if (string.IsNullOrEmpty(apiRootUrl) || apiRootUrl == "https://api.example.invalid")
                {
                    _logger.LogError("ApiRootUrl not configured or using placeholder value");
                    return false;
                }

                var url = $"{apiRootUrl.TrimEnd('/')}/api/auth/refresh";

                var request = new
                {
                    refreshToken = refreshToken,
                    deviceId = GetDeviceId(),
                    devicePlatform = GetDevicePlatform()
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var httpResp = await _httpClient.PostAsJsonAsync(url, request);

                if (!httpResp.IsSuccessStatusCode)
                {
                    var err = await httpResp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Refresh token endpoint returned {Status}: {Body}", httpResp.StatusCode, err);
                    _telemetry?.TrackEvent("Auth.RefreshFailed", new Dictionary<string, string?> { { "Status", httpResp.StatusCode.ToString() }, { "Error", err } } as IDictionary<string, string>);
                    return false;
                }

                var content = await httpResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.TryGetProperty("accessToken", out var accessTokenProp) && !root.TryGetProperty("access_token", out accessTokenProp))
                {
                    _logger.LogError("Refresh response missing accessToken");
                    return false;
                }

                var accessToken = accessTokenProp.GetString();
                if (string.IsNullOrEmpty(accessToken)) return false;

                string? newRefresh = null;
                if (root.TryGetProperty("refreshToken", out var refreshProp) || root.TryGetProperty("refresh_token", out refreshProp))
                {
                    newRefresh = refreshProp.GetString();
                }

                DateTimeOffset? expiresAt = null;
                if (root.TryGetProperty("expiresIn", out var expiresProp) || root.TryGetProperty("expires_in", out expiresProp))
                {
                    if (expiresProp.ValueKind == JsonValueKind.Number && expiresProp.TryGetInt64(out var secs))
                    {
                        expiresAt = DateTimeOffset.UtcNow.AddSeconds(secs);
                    }
                    else if (expiresProp.ValueKind == JsonValueKind.String && long.TryParse(expiresProp.GetString(), out var secs2))
                    {
                        expiresAt = DateTimeOffset.UtcNow.AddSeconds(secs2);
                    }
                }

                await _secureStorage.SetAsync(AccessTokenKey, accessToken);
                if (!string.IsNullOrEmpty(newRefresh)) await _secureStorage.SetAsync(RefreshTokenKey, newRefresh);
                if (expiresAt.HasValue) await _secureStorage.SetAsync(AccessTokenExpiresAtKey, expiresAt.Value.ToString("o"));

                sw.Stop();
                _telemetry?.TrackHistogram("Auth.RefreshMs", sw.Elapsed.TotalMilliseconds);
                _telemetry?.TrackEvent("Auth.RefreshSucceeded", new Dictionary<string, string> { { "Rotated", (!string.IsNullOrEmpty(newRefresh)).ToString() } });

                _logger.LogInformation("Refreshed access token successfully (rotated refresh={HasRotated})", !string.IsNullOrEmpty(newRefresh));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh access token");
                return false;
            }
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
        private IntPtr GetParentWindowHandle()
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
                    _logger?.LogWarning(ex, "Failed to get window handle for MSAL WAM integration");
                }
                return IntPtr.Zero;
            }
#endif
    }
}

