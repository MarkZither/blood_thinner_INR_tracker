using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;

namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Authentication service for managing user login/logout and tokens
/// </summary>
public interface IAuthenticationService
{
    Task<bool> LoginAsync(string email, string password);
    Task<bool> LoginWithOAuthAsync(string provider);
    Task<bool> RegisterAsync(string email, string password, string firstName, string lastName);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetAuthTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<bool> RefreshTokenAsync();
    event EventHandler<bool> AuthenticationStateChanged;
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _apiService;
    private readonly ISecureStorageService _secureStorage;
    private const string AUTH_TOKEN_KEY = "auth_token";
    private const string REFRESH_TOKEN_KEY = "refresh_token";
    private const string USER_ID_KEY = "user_id";

    public event EventHandler<bool> AuthenticationStateChanged = delegate { };

    public AuthenticationService(IApiService apiService, ISecureStorageService secureStorage)
    {
        _apiService = apiService;
        _secureStorage = secureStorage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var loginRequest = new LoginRequest { Email = email, Password = password };
            var response = await _apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", loginRequest);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SetAsync(AUTH_TOKEN_KEY, response.Token);
                await _secureStorage.SetAsync(REFRESH_TOKEN_KEY, response.RefreshToken ?? "");
                await _secureStorage.SetAsync(USER_ID_KEY, response.UserId?.ToString() ?? "");

                AuthenticationStateChanged?.Invoke(this, true);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginWithOAuthAsync(string provider)
    {
        try
        {
            // This would typically open a web view for OAuth authentication
            // For now, return false as OAuth requires platform-specific implementation
            await Task.Delay(100);
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OAuth login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password, string firstName, string lastName)
    {
        try
        {
            var registerRequest = new RegisterRequest 
            { 
                Email = email, 
                Password = password, 
                FirstName = firstName, 
                LastName = lastName 
            };
            
            var response = await _apiService.PostAsync<RegisterRequest, RegisterResponse>("auth/register", registerRequest);

            if (response?.Success == true)
            {
                // Auto-login after successful registration
                return await LoginAsync(email, password);
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Call logout endpoint if available
            await _apiService.PostAsync<object, object>("auth/logout", new { });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout API error: {ex.Message}");
        }
        finally
        {
            // Clear stored tokens regardless of API call result
            await _secureStorage.RemoveAsync(AUTH_TOKEN_KEY);
            await _secureStorage.RemoveAsync(REFRESH_TOKEN_KEY);
            await _secureStorage.RemoveAsync(USER_ID_KEY);

            AuthenticationStateChanged?.Invoke(this, false);
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _secureStorage.GetAsync(AUTH_TOKEN_KEY);
            if (string.IsNullOrEmpty(token))
                return false;

            // Check if token is expired
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jsonToken = handler.ReadJwtToken(token);
                if (jsonToken.ValidTo < DateTime.UtcNow)
                {
                    // Try to refresh the token
                    return await RefreshTokenAsync();
                }
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication check error: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetAuthTokenAsync()
    {
        return await _secureStorage.GetAsync(AUTH_TOKEN_KEY);
    }

    public async Task<string?> GetUserIdAsync()
    {
        return await _secureStorage.GetAsync(USER_ID_KEY);
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _secureStorage.GetAsync(REFRESH_TOKEN_KEY);
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var refreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
            var response = await _apiService.PostAsync<RefreshTokenRequest, LoginResponse>("auth/refresh", refreshRequest);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                await _secureStorage.SetAsync(AUTH_TOKEN_KEY, response.Token);
                await _secureStorage.SetAsync(REFRESH_TOKEN_KEY, response.RefreshToken ?? "");
                return true;
            }

            // Refresh failed, logout user
            await LogoutAsync();
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Token refresh error: {ex.Message}");
            await LogoutAsync();
            return false;
        }
    }
}

// Request/Response models for authentication
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public int? UserId { get; set; }
    public string? Message { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}