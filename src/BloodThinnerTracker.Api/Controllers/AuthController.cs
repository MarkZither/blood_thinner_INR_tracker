// BloodThinnerTracker.Api - Authentication Controller for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Web;
using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Api.Services.Authentication;

namespace BloodThinnerTracker.Api.Controllers;

/// <summary>
/// Authentication controller for medical application security
/// Provides secure login, token management, and medical data access control
///
/// MEDICAL DISCLAIMER: This system is for medication tracking purposes only.
/// Always consult healthcare professionals for medical decisions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IIdTokenValidationService _idTokenValidationService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initialize authentication controller
    /// </summary>
    public AuthController(
        IAuthenticationService authenticationService,
        IIdTokenValidationService idTokenValidationService,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _idTokenValidationService = idTokenValidationService ?? throw new ArgumentNullException(nameof(idTokenValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Initiate OAuth2 authentication flow (redirect to provider)
    /// </summary>
    /// <param name="provider">OAuth provider (google or azuread)</param>
    /// <param name="redirectUri">Optional callback URI (defaults to /api/auth/callback/{provider})</param>
    /// <returns>Redirect to OAuth provider consent page</returns>
    /// <response code="302">Redirect to OAuth provider</response>
    /// <response code="400">Invalid provider</response>
    [HttpGet("external/{provider}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExternalLogin(
        [FromRoute] string provider,
        [FromQuery] string? redirectUri = null)
    {
        try
        {
            // Validate provider
            provider = provider.ToLowerInvariant();
            if (provider != "google" && provider != "azuread")
            {
                _logger.LogWarning("Invalid OAuth provider requested: {Provider}", provider);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Detail = $"Provider '{provider}' is not supported. Use 'google' or 'azuread'.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Generate CSRF state parameter
            var csrfToken = GenerateState();

            // Build state parameter: csrfToken|finalRedirectUri
            // This allows us to redirect user after OAuth completes
            var state = string.IsNullOrEmpty(redirectUri)
                ? csrfToken
                : $"{csrfToken}|{redirectUri}";

            // Store state in distributed cache with 5-minute expiration
            var cacheKey = $"oauth_state:{csrfToken}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, state, cacheOptions);

            // Build authorization URL (always uses /api/auth/callback/{provider})
            var authUrl = BuildAuthorizationUrl(provider, state);

            _logger.LogInformation("Initiating OAuth2 flow for provider {Provider} with state {State}", provider, state);

            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating OAuth2 flow for provider {Provider}", provider);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "OAuth Error",
                Detail = "Failed to initiate OAuth2 authentication",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// OAuth2 callback handler (receives authorization code from provider)
    /// </summary>
    /// <param name="provider">OAuth provider (google or azuread)</param>
    /// <param name="code">Authorization code from provider</param>
    /// <param name="state">CSRF state parameter</param>
    /// <param name="error">Error from provider (if any)</param>
    /// <returns>Authentication response with JWT tokens or error</returns>
    /// <response code="200">OAuth callback successful, returns JWT tokens</response>
    /// <response code="400">Invalid callback parameters</response>
    /// <response code="401">OAuth authentication failed</response>
    [HttpGet("callback/{provider}")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> OAuthCallback(
        [FromRoute] string provider,
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null)
    {
        try
        {
            // Handle provider error
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("OAuth provider returned error: {Error}", error);

                // Check if this is a test page callback
                if (!string.IsNullOrEmpty(state))
                {
                    var errorStateParts = state.Split('|');
                    if (errorStateParts.Length >= 2 && errorStateParts[1].EndsWith("/oauth-test.html", StringComparison.OrdinalIgnoreCase))
                    {
                        return Redirect($"{errorStateParts[1]}?error={Uri.EscapeDataString(error)}");
                    }
                }

                return Unauthorized(new ProblemDetails
                {
                    Title = "OAuth Error",
                    Detail = $"OAuth provider returned error: {error}",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Validate required parameters
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("OAuth callback missing required parameters");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Callback",
                    Detail = "Missing required parameters: code and state",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Extract CSRF token from state (format: csrfToken or csrfToken|redirectUri)
            var stateParts = state.Split('|');
            var csrfToken = stateParts[0];
            var finalRedirectUri = stateParts.Length > 1 ? stateParts[1] : null;

            // Validate CSRF state parameter
            var cacheKey = $"oauth_state:{csrfToken}";
            var cachedState = await _cache.GetStringAsync(cacheKey);
            if (cachedState != state)
            {
                _logger.LogWarning("OAuth state parameter mismatch - possible CSRF attack. Expected: {Expected}, Got: {Got}",
                    cachedState, state);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid State",
                    Detail = "State parameter validation failed (CSRF protection)",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Remove used state from cache
            await _cache.RemoveAsync(cacheKey);

            // Exchange authorization code for ID token
            var idToken = await ExchangeCodeForIdTokenAsync(provider, code);
            if (string.IsNullOrEmpty(idToken))
            {
                _logger.LogWarning("Failed to exchange authorization code for ID token");
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Exchange Failed",
                    Detail = "Could not exchange authorization code for ID token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Validate ID token and extract claims
            IdTokenValidationResult validationResult;
            if (provider == "google")
            {
                validationResult = await _idTokenValidationService.ValidateGoogleTokenAsync(idToken);
            }
            else // azuread
            {
                validationResult = await _idTokenValidationService.ValidateAzureAdTokenAsync(idToken);
            }

            if (!validationResult.IsValid || string.IsNullOrEmpty(validationResult.ExternalUserId))
            {
                _logger.LogWarning("ID token validation failed: {Error}", validationResult.ErrorMessage);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Validation Failed",
                    Detail = validationResult.ErrorMessage ?? "Invalid ID token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Authenticate using validated claims
            var deviceId = $"web-{Guid.NewGuid()}";

            // Ensure we have a valid external user ID
            if (string.IsNullOrEmpty(validationResult.ExternalUserId))
            {
                _logger.LogWarning("Token validation succeeded but ExternalUserId is missing for provider {Provider}", provider);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Validation Failed",
                    Detail = "User identifier (oid/sub claim) is missing from ID token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogDebug("Calling AuthenticateExternalAsync with Provider={Provider}, ExternalUserId={ExternalUserId}, Email={Email}, Name={Name}",
                validationResult.Provider,
                validationResult.ExternalUserId,
                validationResult.Email ?? "(null)",
                validationResult.Name ?? "(null)");

            var response = await _authenticationService.AuthenticateExternalAsync(
                validationResult.Provider!,
                validationResult.ExternalUserId!,
                validationResult.Email ?? string.Empty,
                validationResult.Name ?? string.Empty,
                deviceId);

            if (response == null)
            {
                _logger.LogWarning("OAuth authentication failed for provider {Provider} - AuthenticateExternalAsync returned null", provider);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Could not authenticate with provided OAuth token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("OAuth authentication successful for user {UserId} via {Provider}",
                response.User.PublicId, provider);

            // Check if there's a final redirect URI (e.g., /oauth-test.html)
            if (!string.IsNullOrEmpty(finalRedirectUri))
            {
                if (finalRedirectUri.EndsWith("/oauth-test.html", StringComparison.OrdinalIgnoreCase))
                {
                    // Return to test page with token in query string for easy copy/paste
                    var testPageUrl = $"{finalRedirectUri}?token={response.AccessToken}";
                    return Redirect(testPageUrl);
                }

                // For other redirect URIs, append token as query parameter
                var separator = finalRedirectUri.Contains('?') ? '&' : '?';
                return Redirect($"{finalRedirectUri}{separator}token={response.AccessToken}");
            }

            // Normal OAuth flow - return JSON response
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback for provider {Provider}", provider);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Callback Error",
                Detail = "An error occurred processing the OAuth callback",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Mobile OAuth2 endpoint (ID token exchange flow)
    /// </summary>
    /// <param name="request">External login request with ID token</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Authentication failed</response>
    [HttpPost("external/mobile")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> ExternalMobileLogin([FromBody] ExternalLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid external mobile login request");
                return BadRequest(ModelState);
            }

            // Validate ID token based on provider
            IdTokenValidationResult validationResult;
            if (request.Provider == "Google")
            {
                validationResult = await _idTokenValidationService.ValidateGoogleTokenAsync(request.IdToken);
            }
            else if (request.Provider == "AzureAD")
            {
                validationResult = await _idTokenValidationService.ValidateAzureAdTokenAsync(request.IdToken);
            }
            else
            {
                _logger.LogWarning("Unsupported provider: {Provider}", request.Provider);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Detail = $"Provider '{request.Provider}' is not supported",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (!validationResult.IsValid || string.IsNullOrEmpty(validationResult.ExternalUserId))
            {
                _logger.LogWarning("ID token validation failed: {Error}", validationResult.ErrorMessage);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Validation Failed",
                    Detail = validationResult.ErrorMessage ?? "Invalid ID token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("ID token validation successful - Provider: {Provider}, ExternalUserId: {ExternalUserId}, Email: {Email}",
                validationResult.Provider, validationResult.ExternalUserId, validationResult.Email);

            // Authenticate using validated claims
            var response = await _authenticationService.AuthenticateExternalAsync(
                validationResult.Provider!,
                validationResult.ExternalUserId,
                validationResult.Email ?? string.Empty,
                validationResult.Name ?? string.Empty,
                request.DeviceId);

            if (response == null)
            {
                _logger.LogWarning("External authentication failed for provider {Provider}", request.Provider);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Could not authenticate with provided ID token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("Mobile OAuth authentication successful for user {UserId} via {Provider}",
                response.User.PublicId, request.Provider);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during external mobile login");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during authentication",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Simplified token exchange for Web SSO scenarios where Web app handles OAuth
    /// </summary>
    /// <param name="request">Claims-based authentication request</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("exchange")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthenticationResponse>> ExchangeWebToken([FromBody] ExternalLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid external login request");
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                _logger.LogWarning("External login request missing id_token");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "id_token is required for token exchange"
                });
            }

            _logger.LogInformation("Processing token exchange for provider: {Provider}", request.Provider);

            // Validate the provider-signed ID token (do not trust client-supplied claims)
            IdTokenValidationResult validationResult;
            if (string.Equals(request.Provider, "Google", StringComparison.OrdinalIgnoreCase))
            {
                validationResult = await _idTokenValidationService.ValidateGoogleTokenAsync(request.IdToken);
            }
            else if (string.Equals(request.Provider, "AzureAD", StringComparison.OrdinalIgnoreCase))
            {
                validationResult = await _idTokenValidationService.ValidateAzureAdTokenAsync(request.IdToken);
            }
            else
            {
                _logger.LogWarning("Unsupported provider in external login request: {Provider}", request.Provider);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid provider",
                    Detail = "Unsupported authentication provider"
                });
            }

            if (!validationResult.IsValid || string.IsNullOrEmpty(validationResult.ExternalUserId))
            {
                _logger.LogWarning("ID token validation failed for provider {Provider}: {Error}", request.Provider, validationResult.ErrorMessage);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Invalid or untrusted identity token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Authenticate / provision user using validated token claims
            var response = await _authenticationService.AuthenticateExternalAsync(
                validationResult.Provider ?? request.Provider ?? "AzureAD",
                validationResult.ExternalUserId,
                validationResult.Email ?? string.Empty,
                validationResult.Name ?? string.Empty,
                request.DeviceId ?? "web-app");

            if (response == null)
            {
                _logger.LogWarning("External authentication failed for ExternalUserId {ExternalUserId}", validationResult.ExternalUserId);
                return Unauthorized(new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = "Unable to authenticate user",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("Token exchange successful for user: {UserId}", response.User?.PublicId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web token exchange for provider {Provider}", request?.Provider);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during token exchange",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    #region Helper Methods

    private string GenerateState()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string BuildAuthorizationUrl(string provider, string state)
    {
        var baseUrl = Request.Scheme + "://" + Request.Host;
        // OAuth callback always points to /api/auth/callback/{provider}
        // The final redirect URI is encoded in the state parameter
        var callback = $"{baseUrl}/api/auth/callback/{provider}";

        if (provider == "google")
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var scopes = string.Join(" ", _configuration.GetSection("Authentication:Google:Scopes").Get<string[]>()
                ?? new[] { "openid", "profile", "email" });

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["client_id"] = clientId;
            queryParams["redirect_uri"] = callback;
            queryParams["response_type"] = "code";
            queryParams["scope"] = scopes;
            queryParams["state"] = state;
            queryParams["access_type"] = "offline";
            queryParams["prompt"] = "consent";

            return $"https://accounts.google.com/o/oauth2/v2/auth?{queryParams}";
        }
        else if (provider == "azuread")
        {
            var tenantId = _configuration["Authentication:AzureAd:TenantId"] ?? "common";
            var clientId = _configuration["Authentication:AzureAd:ClientId"];
            var scopes = string.Join(" ", _configuration.GetSection("Authentication:AzureAd:Scopes").Get<string[]>()
                ?? new[] { "openid", "profile", "email" });

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["client_id"] = clientId;
            queryParams["redirect_uri"] = callback;
            queryParams["response_type"] = "code";
            queryParams["scope"] = scopes;
            queryParams["state"] = state;
            queryParams["prompt"] = "select_account";

            var instance = _configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
            return $"{instance}{tenantId}/oauth2/v2.0/authorize?{queryParams}";
        }

        throw new ArgumentException($"Unsupported provider: {provider}");
    }

    private async Task<string?> ExchangeCodeForIdTokenAsync(string provider, string code)
    {
        var baseUrl = Request.Scheme + "://" + Request.Host;
        var redirectUri = $"{baseUrl}/api/auth/callback/{provider}";

        using var httpClient = new HttpClient();

        if (provider == "google")
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];

            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Google token exchange failed: {Status} - {Error}",
                    tokenResponse.StatusCode, errorContent);
                return null;
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
            return tokenData?.IdToken;
        }
        else if (provider == "azuread")
        {
            var tenantId = _configuration["Authentication:AzureAd:TenantId"] ?? "common";
            var clientId = _configuration["Authentication:AzureAd:ClientId"];
            var clientSecret = _configuration["Authentication:AzureAd:ClientSecret"];

            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code",
                ["scope"] = "openid profile email"
            };

            var instance = _configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
            var tokenResponse = await httpClient.PostAsync(
                $"{instance}{tenantId}/oauth2/v2.0/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Azure AD token exchange failed: {Status} - {Error}",
                    tokenResponse.StatusCode, errorContent);
                return null;
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<AzureAdTokenResponse>();
            return tokenData?.IdToken;
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with refreshed tokens</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid refresh token request format");
                return BadRequest(ModelState);
            }

            var response = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
            if (response == null)
            {
                _logger.LogWarning("Token refresh failed for refresh token");
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Refresh Failed",
                    Detail = "Invalid or expired refresh token",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            _logger.LogInformation("Token refreshed successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during token refresh",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Revoke refresh token to logout user securely
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Token revoked successfully</response>
    /// <response code="400">Invalid request format</response>
    /// <response code="401">Unauthorized - must be authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout([FromBody] string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Logout attempt with empty refresh token");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Refresh token is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _authenticationService.RevokeTokenAsync(refreshToken);

            if (success)
            {
                _logger.LogInformation("User {UserId} logged out successfully", userId);
                return Ok(new { message = "Logged out successfully" });
            }

            _logger.LogWarning("Failed to revoke refresh token for user {UserId}", userId);
            return BadRequest(new ProblemDetails
            {
                Title = "Logout Failed",
                Detail = "Failed to revoke refresh token",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during logout",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get current user information and permissions
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Unauthorized - must be authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        try
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userPublicId))
            {
                _logger.LogWarning("Unable to extract user ID from token");
                return Unauthorized();
            }

            var userInfo = new UserInfo
            {
                PublicId = userPublicId,  // PublicId is a typed Guid
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                Name = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient",
                Provider = User.FindFirst("provider")?.Value ?? "Unknown",
                TimeZone = User.FindFirst("timezone")?.Value ?? "UTC"
            };

            var permissions = await _authenticationService.GetUserPermissionsAsync(userPublicId);

            _logger.LogDebug("Retrieved user information for {UserPublicId}", userPublicId);
            return Ok(new { user = userInfo, permissions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting current user");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred retrieving user information",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Validate current session for medical security
    /// </summary>
    /// <returns>Session validation result</returns>
    /// <response code="200">Session is valid</response>
    /// <response code="401">Session is invalid or expired</response>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ValidateSession()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Extract session ID from claims or generate one for validation
            var sessionId = User.FindFirst("session_id")?.Value ?? Guid.NewGuid().ToString();
            var isValid = await _authenticationService.ValidateSessionAsync(userId, sessionId);

            if (isValid)
            {
                _logger.LogDebug("Session validated for user {UserId}", userId);
                return Ok(new { valid = true, userId, timestamp = DateTime.UtcNow });
            }

            _logger.LogWarning("Invalid session for user {UserId}", userId);
            return Unauthorized(new ProblemDetails
            {
                Title = "Session Invalid",
                Detail = "Current session is invalid or expired",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during session validation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during session validation",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get OAuth configuration for mobile clients.
    /// Returns client IDs, redirect URIs, and scopes for all configured providers.
    /// This is a public endpoint (no authentication required).
    /// </summary>
    /// <returns>OAuth configuration for all providers</returns>
    /// <response code="200">OAuth configuration</response>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BloodThinnerTracker.Shared.Models.Authentication.OAuthConfig), StatusCodes.Status200OK)]
    public ActionResult<BloodThinnerTracker.Shared.Models.Authentication.OAuthConfig> GetOAuthConfig()
    {
        var config = new BloodThinnerTracker.Shared.Models.Authentication.OAuthConfig
        {
            FetchedAt = DateTimeOffset.UtcNow,
            Providers = new List<BloodThinnerTracker.Shared.Models.Authentication.OAuthProviderConfig>()
        };

        // Add Google OAuth configuration if enabled
        if (!string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]))
        {
            config.Providers.Add(new BloodThinnerTracker.Shared.Models.Authentication.OAuthProviderConfig
            {
                Provider = "google",
                ClientId = _configuration["Authentication:Google:ClientId"] ?? string.Empty,
                RedirectUri = _configuration["Authentication:Google:RedirectUri"] ?? "http://localhost/callback",
                Authority = "https://accounts.google.com",
                Scopes = string.Join(" ", _configuration.GetSection("Authentication:Google:Scopes").Get<string[]>() ?? new[] { "openid", "profile", "email" })
            });
        }

        // Add Azure AD OAuth configuration if enabled
        if (!string.IsNullOrEmpty(_configuration["Authentication:AzureAd:ClientId"]))
        {
            var tenantId = _configuration["Authentication:AzureAd:TenantId"] ?? "common";
            var instance = _configuration["Authentication:AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
            var appIdUri = _configuration["Authentication:AzureAd:AppIdUri"] ?? _configuration["Authentication:AzureAd:ClientId"];

            // For PKCE public client flow, request the API scope instead of 'openid'
            // Azure AD automatically includes openid profile email in id_token response
            var defaultScopes = new[] { $"{appIdUri}/.default" };

            config.Providers.Add(new BloodThinnerTracker.Shared.Models.Authentication.OAuthProviderConfig
            {
                Provider = "azure",
                ClientId = _configuration["Authentication:AzureAd:ClientId"] ?? string.Empty,
                RedirectUri = _configuration["Authentication:AzureAd:RedirectUri"] ?? "http://localhost/callback",
                Authority = $"{instance}{tenantId}",
                Scopes = string.Join(" ", _configuration.GetSection("Authentication:AzureAd:Scopes").Get<string[]>() ?? defaultScopes)
            });
        }

        _logger.LogInformation("OAuth configuration requested, returning {ProviderCount} configured providers", config.Providers.Count);
        return Ok(config);
    }

    /// <summary>
    /// Health check endpoint for authentication service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            service = "Authentication",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
