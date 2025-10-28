// BloodThinnerTracker.Api - Authentication Service for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Api.Data;

namespace BloodThinnerTracker.Api.Services.Authentication;

/// <summary>
/// Authentication service interface for medical application
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with external provider (Azure AD, Google)
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="externalId">External user ID</param>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <param name="deviceId">Optional device ID for token tracking</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateExternalAsync(string provider, string externalId, string email, string name, string? deviceId = null);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token string</param>
    /// <returns>New authentication response</returns>
    Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <returns>True if revoked successfully</returns>
    Task<bool> RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Get user permissions for medical data access
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of permissions</returns>
    Task<List<string>> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Validate user session for medical security
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>True if session is valid</returns>
    Task<bool> ValidateSessionAsync(string userId, string sessionId);
}

/// <summary>
/// Authentication service implementation for medical application
/// Handles secure authentication with medical data protection
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IIdTokenValidationService _idTokenValidationService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationConfig _authConfig;

    /// <summary>
    /// Initialize authentication service with medical security configuration
    /// </summary>
    public AuthenticationService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IIdTokenValidationService idTokenValidationService,
        ILogger<AuthenticationService> logger,
        AuthenticationConfig authConfig)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _idTokenValidationService = idTokenValidationService ?? throw new ArgumentNullException(nameof(idTokenValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authConfig = authConfig ?? throw new ArgumentNullException(nameof(authConfig));
    }

    /// <summary>
    /// Authenticate user with external provider (Azure AD, Google)
    /// Uses ID token validation and auto-creates users on first login
    /// </summary>
    public async Task<AuthenticationResponse?> AuthenticateExternalAsync(string provider, string externalId, string email, string name, string? deviceId = null)
    {
        try
        {
            _logger.LogInformation("External authentication attempt for {Email} via {Provider}", email, provider);

            // Look up existing user by ExternalUserId
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ExternalUserId == externalId && u.AuthProvider == provider);

            User user;
            bool isNewUser = false;

            if (existingUser == null)
            {
                // Auto-create user on first OAuth login (per T015e requirement)
                _logger.LogInformation("Creating new user account for {Email} via {Provider}", email, provider);
                
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = email,
                    Name = name,
                    AuthProvider = provider,
                    ExternalUserId = externalId,
                    Role = UserRole.Patient,
                    TimeZone = "UTC", // TODO: Detect from device in T019a
                    LastLoginAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                _context.Users.Add(user);
                isNewUser = true;
            }
            else
            {
                // Update existing user's last login timestamp
                user = existingUser;
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            // Create UserInfo for JWT token
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                Provider = user.AuthProvider,
                TimeZone = user.TimeZone,
                LastLogin = user.LastLoginAt ?? DateTime.UtcNow
            };

            var permissions = await GetUserPermissionsAsync(user.Id);
            var accessToken = _jwtTokenService.GenerateAccessToken(userInfo, permissions);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token in database (T011d)
            await StoreRefreshTokenAsync(user.Id, refreshToken, deviceId);
            await LogAuthenticationEventAsync(user.Id, isNewUser ? "NewUserCreated" : "ExternalLogin", "Success", null);

            _logger.LogInformation("User {UserId} authenticated via {Provider} (New: {IsNew})", user.Id, provider, isNewUser);

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _authConfig.Jwt.AccessTokenExpirationMinutes * 60,
                User = userInfo,
                Permissions = permissions,
                RequiresMfa = _authConfig.MedicalSecurity.RequireMfa
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External authentication failed for {Email} via {Provider}", email, provider);
            await LogAuthenticationEventAsync(email, "ExternalLogin", "Failed", null);
            return null;
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    public async Task<AuthenticationResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogDebug("Token refresh attempt");

            // Hash the refresh token to find it in the database
            var tokenHash = HashToken(refreshToken);

            // Find the refresh token in the database
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found in database");
                return null;
            }

            // Validate token is still active (not expired or revoked)
            if (!storedToken.IsActive)
            {
                _logger.LogWarning("Refresh token is inactive (expired or revoked) for user {UserId}", storedToken.UserId);
                return null;
            }

            var user = storedToken.User;
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                _logger.LogWarning("User {UserId} is inactive or deleted", storedToken.UserId);
                return null;
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);

            // Create UserInfo for new JWT token
            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                Provider = user.AuthProvider,
                TimeZone = user.TimeZone,
                LastLogin = user.LastLoginAt ?? DateTime.UtcNow
            };

            var permissions = await GetUserPermissionsAsync(user.Id);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(userInfo, permissions);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Revoke old refresh token
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevocationReason = "Replaced with new refresh token";
            _context.RefreshTokens.Update(storedToken);

            // Store new refresh token
            await StoreRefreshTokenAsync(user.Id, newRefreshToken, storedToken.DeviceId);
            await _context.SaveChangesAsync();

            await LogAuthenticationEventAsync(user.Id, "TokenRefresh", "Success", storedToken.DeviceId);
            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return new AuthenticationResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = _authConfig.Jwt.AccessTokenExpirationMinutes * 60,
                User = userInfo,
                Permissions = permissions,
                RequiresMfa = _authConfig.MedicalSecurity.RequireMfa
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return null;
        }
    }

    /// <summary>
    /// Revoke refresh token for security
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("Revoking refresh token");

            // Hash the refresh token to find it in the database
            var tokenHash = HashToken(refreshToken);

            // Find the refresh token in the database
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found for revocation");
                return false;
            }

            // Mark token as revoked
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevocationReason = "User logout";
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            await LogAuthenticationEventAsync(storedToken.UserId, "TokenRevoke", "Success", storedToken.DeviceId);
            _logger.LogInformation("Refresh token revoked for user {UserId}", storedToken.UserId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke refresh token");
            return false;
        }
    }

    /// <summary>
    /// Get user permissions for medical data access
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            // Default medical permissions for patients
            // TODO: Implement role-based permissions when User entity is created
            var permissions = new List<string>
            {
                "medication:read",
                "medication:write",
                "inr:read",
                "inr:write",
                "reminders:read",
                "reminders:write",
                "profile:read",
                "profile:write"
            };

            _logger.LogDebug("Retrieved {PermissionCount} permissions for user {UserId}", permissions.Count, userId);
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Validate user session for medical security
    /// </summary>
    public async Task<bool> ValidateSessionAsync(string userId, string sessionId)
    {
        try
        {
            // TODO: Implement session validation against database
            // This is a placeholder implementation until Session entity is created
            _logger.LogDebug("Session validation for user {UserId}, session {SessionId}", userId, sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session validation failed for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Store refresh token in database with secure hashing
    /// </summary>
    private async Task StoreRefreshTokenAsync(string userId, string refreshToken, string? deviceId)
    {
        var tokenHash = HashToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(7); // NFR-002: 7-day refresh token lifetime

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            TokenHash = tokenHash,
            DeviceId = deviceId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        _logger.LogDebug("Stored refresh token for user {UserId}, device {DeviceId}", userId, deviceId);
    }

    /// <summary>
    /// Hash refresh token for secure storage (SHA-256)
    /// </summary>
    private string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Log authentication events for medical compliance and security
    /// </summary>
    private async Task LogAuthenticationEventAsync(string userId, string eventType, string result, string? deviceId)
    {
        try
        {
            // Log to audit system for medical compliance
            _logger.LogInformation("Authentication event: {EventType} for user {UserId} - {Result}", 
                eventType, userId, result);

            // TODO: Store in audit log table when implemented
            // This ensures medical compliance and security tracking
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication event");
        }
    }
}