// BloodThinnerTracker.Api - Authentication Service for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Api.Data;

namespace BloodThinnerTracker.Api.Services.Authentication;

/// <summary>
/// Authentication service interface for medical application
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateAsync(LoginRequest request);

    /// <summary>
    /// Authenticate user with external provider (Azure AD, Google)
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="externalId">External user ID</param>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateExternalAsync(string provider, string externalId, string email, string name);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response</returns>
    Task<AuthenticationResponse?> RefreshTokenAsync(RefreshTokenRequest request);

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
    private readonly IPasswordHasher<object> _passwordHasher;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationConfig _authConfig;

    /// <summary>
    /// Initialize authentication service with medical security configuration
    /// </summary>
    public AuthenticationService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher<object> passwordHasher,
        ILogger<AuthenticationService> logger,
        AuthenticationConfig authConfig)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authConfig = authConfig ?? throw new ArgumentNullException(nameof(authConfig));
    }

    /// <summary>
    /// Authenticate user with email and password for medical data access
    /// </summary>
    public async Task<AuthenticationResponse?> AuthenticateAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Authentication attempt for user {Email}", request.Email);

            // TODO: Implement user lookup and password verification
            // This is a placeholder implementation until User entity is created
            var user = new UserInfo
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                Name = request.Email.Split('@')[0],
                Role = "Patient",
                Provider = "Local",
                TimeZone = "UTC",
                LastLogin = DateTime.UtcNow
            };

            var permissions = await GetUserPermissionsAsync(user.Id);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // TODO: Store refresh token in database
            await LogAuthenticationEventAsync(user.Id, "Login", "Success", request.DeviceId);

            _logger.LogInformation("User {UserId} authenticated successfully", user.Id);

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _authConfig.Jwt.AccessTokenExpirationMinutes * 60,
                User = user,
                Permissions = permissions,
                RequiresMfa = _authConfig.MedicalSecurity.RequireMfa
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Email}", request.Email);
            await LogAuthenticationEventAsync(request.Email, "Login", "Failed", request.DeviceId);
            return null;
        }
    }

    /// <summary>
    /// Authenticate user with external provider (Azure AD, Google)
    /// </summary>
    public async Task<AuthenticationResponse?> AuthenticateExternalAsync(string provider, string externalId, string email, string name)
    {
        try
        {
            _logger.LogInformation("External authentication attempt for {Email} via {Provider}", email, provider);

            // TODO: Implement external user lookup or creation
            // This is a placeholder implementation until User entity is created
            var user = new UserInfo
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = name,
                Role = "Patient",
                Provider = provider,
                TimeZone = "UTC",
                LastLogin = DateTime.UtcNow
            };

            var permissions = await GetUserPermissionsAsync(user.Id);
            var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // TODO: Store refresh token in database
            await LogAuthenticationEventAsync(user.Id, "ExternalLogin", "Success", null);

            _logger.LogInformation("User {UserId} authenticated via {Provider}", user.Id, provider);

            return new AuthenticationResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _authConfig.Jwt.AccessTokenExpirationMinutes * 60,
                User = user,
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
    public async Task<AuthenticationResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            _logger.LogDebug("Token refresh attempt for refresh token");

            // TODO: Implement refresh token validation against database
            // This is a placeholder implementation until RefreshToken entity is created

            // For now, return null to indicate refresh token is invalid
            _logger.LogWarning("Refresh token validation not implemented yet");
            return null;
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

            // TODO: Implement refresh token revocation in database
            // This is a placeholder implementation until RefreshToken entity is created

            await LogAuthenticationEventAsync("unknown", "TokenRevoke", "Success", null);
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