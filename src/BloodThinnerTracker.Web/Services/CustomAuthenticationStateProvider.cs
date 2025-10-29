// BloodThinnerTracker.Web - Custom Authentication State Provider
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Web.Services;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

/// <summary>
/// Custom authentication state provider for managing JWT-based authentication in Blazor Web.
/// Handles token storage, validation, and authentication state management.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string UserInfoKey = "userInfo";

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomAuthenticationStateProvider"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime for browser storage access.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomAuthenticationStateProvider(
        IJSRuntime jsRuntime,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
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

            var claims = ParseClaimsFromJwt(token);
            
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

            var identity = new ClaimsIdentity(claims, "jwt");
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
            await SetTokenAsync(token);
            
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await SetRefreshTokenAsync(refreshToken);
            }

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

            // Extract user info from claims and store it
            var userInfo = new
            {
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            };

            await SetItemAsync(UserInfoKey, JsonSerializer.Serialize(userInfo));

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

    private async Task SetTokenAsync(string token)
    {
        await SetItemAsync(TokenKey, token);
    }

    private async Task SetRefreshTokenAsync(string refreshToken)
    {
        await SetItemAsync(RefreshTokenKey, refreshToken);
    }

    private async Task ClearAuthenticationAsync()
    {
        await RemoveItemAsync(TokenKey);
        await RemoveItemAsync(RefreshTokenKey);
        await RemoveItemAsync(UserInfoKey);
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

    // Browser localStorage access methods
    private async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (InvalidOperationException)
        {
            // JSRuntime not available during prerendering
            return null;
        }
    }

    private async Task SetItemAsync(string key, string value)
    {
        try
        {
            _logger.LogInformation("Attempting to store {Key} in localStorage (length: {Length})", key, value?.Length ?? 0);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            _logger.LogInformation("Successfully stored {Key} in localStorage", key);
        }
        catch (InvalidOperationException ex)
        {
            // JSRuntime not available during prerendering
            _logger.LogError(ex, "JSRuntime not available - cannot store {Key} in localStorage", key);
            throw new InvalidOperationException($"Cannot store {key} - JSRuntime not available. This should only be called after OnAfterRenderAsync.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error storing {Key} in localStorage", key);
            throw;
        }
    }

    private async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (InvalidOperationException)
        {
            // JSRuntime not available during prerendering - ignore
        }
    }
}
