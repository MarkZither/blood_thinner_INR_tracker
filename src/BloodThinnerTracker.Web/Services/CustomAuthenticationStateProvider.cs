// BloodThinnerTracker.Web - Custom Authentication State Provider
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Web.Services;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Custom authentication state provider for managing JWT-based authentication in Blazor Server.
/// Handles token storage, validation, and authentication state management using server-side memory cache.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string UserInfoKey = "userInfo";
    private const string ClaimsKey = "userClaims";

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAuthenticationStateProvider"/> class.
    /// </summary>
    /// <param name="cache">Memory cache for token storage.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for session identification.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomAuthenticationStateProvider(
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string GetCacheKey(string key, string? userIdentifier = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // If user identifier is explicitly provided, use it (for storing during OAuth)
        if (!string.IsNullOrEmpty(userIdentifier))
        {
            // Use hash for privacy and consistent length
            var hashedId = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(userIdentifier))).Substring(0, 16);
            var cacheKey = $"{hashedId}:{key}";
            _logger.LogInformation("GetCacheKey: Using EXPLICIT user identifier - hashed to {HashedId}, full key: {CacheKey}",
                hashedId, cacheKey);
            return cacheKey;
        }

        // Try to get a stable identifier across HTTP and SignalR contexts
        // Priority 1: Use user's email/sub claim from authenticated user (most stable)
        // Priority 2: Use session ID if available
        // Priority 3: Use connection ID as last resort

        string? identifier = null;

        // Try to get from authenticated user claims first
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("GetCacheKey: Found authenticated user - email: {Email}, sub: {Sub}",
                email ?? "none", sub ?? "none");

            identifier = email ?? sub;
            if (!string.IsNullOrEmpty(identifier))
            {
                // Use hash for privacy and consistent length
                identifier = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(identifier))).Substring(0, 16);
                _logger.LogInformation("GetCacheKey: Using AUTHENTICATED USER identifier - hashed to {Identifier}", identifier);
            }
        }
        else
        {
            _logger.LogWarning("GetCacheKey: No authenticated user found in HttpContext");
        }

        // Fallback to session ID
        if (string.IsNullOrEmpty(identifier) && httpContext?.Session != null)
        {
            _ = httpContext.Session.IsAvailable; // Load session
            identifier = httpContext.Session.Id;
            _logger.LogInformation("GetCacheKey: Using SESSION ID as fallback: {SessionId}", identifier);
        }

        // Last resort: connection ID
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = httpContext?.Connection?.Id ?? "global";
            _logger.LogWarning("GetCacheKey: Using CONNECTION ID as last resort: {Identifier}", identifier);
        }

        var resultKey = $"{identifier}:{key}";
        _logger.LogInformation("GetCacheKey: Final cache key: {CacheKey}", resultKey);
        return resultKey;
    }

    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    /// <returns>The authentication state with user claims if authenticated.</returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return CreateAnonymousState();
            }

            // Try to get stored claims first (from OAuth flow)
            var claimsJson = await GetItemAsync(ClaimsKey);
            IEnumerable<Claim> claims;

            if (!string.IsNullOrEmpty(claimsJson))
            {
                // Use stored claims from OAuth
                var claimData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(claimsJson);
                claims = claimData?.Select(c => new Claim(c["Type"], c["Value"])) ?? Enumerable.Empty<Claim>();
                _logger.LogInformation("Retrieved {Count} claims from stored OAuth data", claims.Count());
            }
            else
            {
                // Fall back to parsing JWT token
                claims = ParseClaimsFromJwt(token);
                _logger.LogInformation("Parsed {Count} claims from JWT token", claims.Count());
            }

            if (!claims.Any())
            {
                _logger.LogWarning("No claims available, user not authenticated");
                return CreateAnonymousState();
            }

            // Check if token is expired
            var expiryClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expiryClaim != null)
            {
                var expiryTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiryClaim.Value));
                if (expiryTime <= DateTimeOffset.UtcNow)
                {
                    _logger.LogInformation("Token has expired, clearing authentication state");
                    await ClearAuthenticationAsync();
                    return CreateAnonymousState();
                }
            }

            var identity = new ClaimsIdentity(claims, "oauth");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return CreateAnonymousState();
        }
    }

    /// <summary>
    /// Marks the user as authenticated and stores the access token.
    /// </summary>
    /// <param name="token">The access token (can be JWT or opaque token).</param>
    /// <param name="refreshToken">The refresh token (optional).</param>
    /// <param name="principal">The authenticated claims principal (optional, for OAuth scenarios).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MarkUserAsAuthenticatedAsync(string token, string? refreshToken = null, ClaimsPrincipal? principal = null)
    {
        try
        {
            IEnumerable<Claim> claims;

            // If a principal is provided (e.g., from OAuth), use its claims
            if (principal?.Identity?.IsAuthenticated == true)
            {
                claims = principal.Claims;
                _logger.LogInformation("Using claims from provided principal (OAuth flow)");
            }
            else
            {
                // Try to parse as JWT (for API-generated tokens)
                claims = ParseClaimsFromJwt(token);
                if (!claims.Any())
                {
                    _logger.LogWarning("No claims found - token might not be a JWT or parsing failed");
                    // Create minimal claims from token storage
                    claims = new List<Claim>();
                }
            }

            // Extract user identifier for cache key
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var sub = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var userIdentifier = email ?? sub ?? "anonymous";

            // Store with user-specific key
            await SetItemAsync(TokenKey, token, userIdentifier);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await SetRefreshTokenAsync(refreshToken, userIdentifier);
            }

            // Extract user info from claims and store it
            var userInfo = new
            {
                Email = email,
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Id = sub
            };

            await SetItemAsync(UserInfoKey, JsonSerializer.Serialize(userInfo), userIdentifier);

            // Also store claims for retrieval
            await SetItemAsync(ClaimsKey, JsonSerializer.Serialize(claims.Select(c => new { c.Type, c.Value })), userIdentifier);

            var identity = new ClaimsIdentity(claims, principal != null ? "oauth" : "jwt");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));

            _logger.LogInformation("User authenticated successfully: {Email} (Claims count: {Count})",
                userInfo.Email ?? "Unknown", claims.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as authenticated");
            throw;
        }
    }

    /// <summary>
    /// Marks the user as logged out and clears all stored tokens.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MarkUserAsLoggedOutAsync()
    {
        await ClearAuthenticationAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAnonymousState()));
        _logger.LogInformation("User logged out successfully");
    }

    /// <summary>
    /// Gets the current JWT access token.
    /// </summary>
    /// <returns>The access token or null if not authenticated.</returns>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await GetItemAsync(TokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token");
            return null;
        }
    }

    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    /// <returns>The refresh token or null if not available.</returns>
    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await GetItemAsync(RefreshTokenKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token");
            return null;
        }
    }

    /// <summary>
    /// Checks if the user is currently authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Gets the current user's email from stored claims.
    /// </summary>
    /// <returns>The user's email or null if not authenticated.</returns>
    public async Task<string?> GetUserEmailAsync()
    {
        try
        {
            var userInfoJson = await GetItemAsync(UserInfoKey);
            if (string.IsNullOrEmpty(userInfoJson))
            {
                return null;
            }

            var userInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(userInfoJson);
            return userInfo?.GetValueOrDefault("Email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user email");
            return null;
        }
    }

    private async Task SetTokenAsync(string token, string? userIdentifier = null)
    {
        await SetItemAsync(TokenKey, token, userIdentifier);
    }

    private async Task SetRefreshTokenAsync(string refreshToken, string? userIdentifier = null)
    {
        await SetItemAsync(RefreshTokenKey, refreshToken, userIdentifier);
    }

    private async Task ClearAuthenticationAsync()
    {
        await RemoveItemAsync(TokenKey);
        await RemoveItemAsync(RefreshTokenKey);
        await RemoveItemAsync(UserInfoKey);
        await RemoveItemAsync(ClaimsKey);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        try
        {
            // Validate JWT format (must have 3 parts separated by dots)
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                _logger.LogError("Invalid JWT format: Expected 3 parts (header.payload.signature), got {Count} parts", parts.Length);
                return claims;
            }

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs == null)
            {
                _logger.LogWarning("JWT payload deserialized to null");
                return claims;
            }

            // Map standard JWT claims to ClaimTypes
            MapClaim(keyValuePairs, claims, "sub", ClaimTypes.NameIdentifier);
            MapClaim(keyValuePairs, claims, "email", ClaimTypes.Email);
            MapClaim(keyValuePairs, claims, "name", ClaimTypes.Name);
            MapClaim(keyValuePairs, claims, "given_name", ClaimTypes.GivenName);
            MapClaim(keyValuePairs, claims, "family_name", ClaimTypes.Surname);
            MapClaim(keyValuePairs, claims, "role", ClaimTypes.Role);

            // Add all other claims as-is
            foreach (var kvp in keyValuePairs)
            {
                if (!claims.Any(c => c.Type == kvp.Key))
                {
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    claims.Add(new Claim(kvp.Key, value));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} claims from JWT", claims.Count);
            return claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JWT claims from token. Token might not be a valid JWT.");
            return claims;
        }
    }

    private void MapClaim(Dictionary<string, object> source, List<Claim> destination, string sourceKey, string destinationType)
    {
        if (source.TryGetValue(sourceKey, out var value) && value != null)
        {
            var stringValue = value.ToString() ?? string.Empty;
            destination.Add(new Claim(destinationType, stringValue));
            destination.Add(new Claim(sourceKey, stringValue)); // Keep original claim too
        }
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    private AuthenticationState CreateAnonymousState()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }

    // Server-side memory cache storage methods
    private Task<string?> GetItemAsync(string key, string? userIdentifier = null)
    {
        try
        {
            var cacheKey = GetCacheKey(key, userIdentifier);
            var sessionId = _httpContextAccessor.HttpContext?.Session?.Id ?? "no-session";
            var value = _cache.Get<string>(cacheKey);
            _logger.LogInformation("Retrieving {Key} from cache with session ID {SessionId}, full key: {CacheKey}, found: {Found}",
                key, sessionId, cacheKey, value != null);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Key} from cache", key);
            return Task.FromResult<string?>(null);
        }
    }

    private Task SetItemAsync(string key, string value, string? userIdentifier = null)
    {
        try
        {
            var cacheKey = GetCacheKey(key, userIdentifier);
            var sessionId = _httpContextAccessor.HttpContext?.Session?.Id ?? "no-session";
            _logger.LogInformation("Storing {Key} in memory cache with session ID {SessionId}, user: {UserIdentifier} (length: {Length})",
                key, sessionId, userIdentifier ?? "auto", value?.Length ?? 0);

            // Store with sliding expiration (30 days of inactivity)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromDays(30));

            _cache.Set(cacheKey, value ?? string.Empty, cacheOptions);
            _logger.LogInformation("Successfully stored {Key} in memory cache with full key: {CacheKey}", key, cacheKey);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing {Key} in cache", key);
            throw;
        }
    }

    private Task RemoveItemAsync(string key, string? userIdentifier = null)
    {
        try
        {
            var cacheKey = GetCacheKey(key, userIdentifier);
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing {Key} from cache", key);
            return Task.CompletedTask;
        }
    }
}
