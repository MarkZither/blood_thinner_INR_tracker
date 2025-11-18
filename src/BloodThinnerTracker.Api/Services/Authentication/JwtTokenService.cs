// BloodThinnerTracker.Api - JWT Token Service for Medical Application
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BloodThinnerTracker.Shared.Models.Authentication;

namespace BloodThinnerTracker.Api.Services.Authentication;

/// <summary>
/// JWT token service for medical application security
/// Implements secure token generation and validation for healthcare data protection
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token for authenticated user
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="permissions">Medical data permissions</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(UserInfo user, IEnumerable<string> permissions);

    /// <summary>
    /// Generate refresh token for token renewal
    /// </summary>
    /// <returns>Secure refresh token</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate JWT access token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Extract user ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Check if token is expired
    /// </summary>
    /// <param name="token">JWT token to check</param>
    /// <returns>True if expired, false otherwise</returns>
    bool IsTokenExpired(string token);
}

/// <summary>
/// JWT token service implementation for medical application
/// Provides secure token generation and validation with medical security considerations
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtConfig _jwtConfig;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    /// <summary>
    /// Initialize JWT token service with medical security configuration
    /// </summary>
    /// <param name="jwtConfig">JWT configuration</param>
    /// <param name="logger">Service logger</param>
    public JwtTokenService(JwtConfig jwtConfig, ILogger<JwtTokenService> logger)
    {
        _jwtConfig = jwtConfig ?? throw new ArgumentNullException(nameof(jwtConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenHandler = new JwtSecurityTokenHandler();

        // Validate JWT configuration for medical security
        ValidateJwtConfiguration();

        // Configure token validation parameters
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtConfig.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1), // Reduced clock skew for medical security
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }

    /// <summary>
    /// Generate secure JWT access token for medical data access
    /// </summary>
    public string GenerateAccessToken(UserInfo user, IEnumerable<string> permissions)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Role, user.Role),
                new("provider", user.Provider),
                new("timezone", user.TimeZone),
                new(JwtRegisteredClaimNames.Sub, user.PublicId.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new("medical_data_access", "true") // Medical data access flag
            };

            // Add medical permissions as claims
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            // Add device security claims if available
            if (user.PublicId != Guid.Empty)
            {
                claims.Add(new Claim("device_verified", "true"));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenExpirationMinutes),
                SigningCredentials = credentials,
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                NotBefore = DateTime.UtcNow // Token not valid before current time
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            _logger.LogInformation("Generated access token for user {UserPublicId} with {PermissionCount} permissions",
                user.PublicId, permissions.Count());

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate access token for user {UserPublicId}", user.PublicId);
            throw new InvalidOperationException("Failed to generate access token", ex);
        }
    }

    /// <summary>
    /// Generate cryptographically secure refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        try
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64]; // 512-bit token for enhanced security
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            _logger.LogDebug("Generated new refresh token");
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate refresh token");
            throw new InvalidOperationException("Failed to generate refresh token", ex);
        }
    }

    /// <summary>
    /// Validate JWT access token with medical security checks
    /// </summary>
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: empty or null token");
            return null;
        }

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Additional medical security validations
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Token validation failed: invalid algorithm");
                return null;
            }

            // Verify medical data access claim
            var medicalAccessClaim = principal.FindFirst("medical_data_access");
            if (medicalAccessClaim?.Value != "true")
            {
                _logger.LogWarning("Token validation failed: missing medical data access claim");
                return null;
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug("Successfully validated token for user {UserId}", userId);

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("Token validation failed: token expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Extract user ID from JWT token
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = ValidateAccessToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract user ID from token");
            return null;
        }
    }

    /// <summary>
    /// Check if JWT token is expired
    /// </summary>
    public bool IsTokenExpired(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check token expiration");
            return true; // Assume expired if we can't validate
        }
    }

    /// <summary>
    /// Validate JWT configuration for medical security requirements
    /// </summary>
    private void ValidateJwtConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_jwtConfig.SecretKey))
            throw new InvalidOperationException("JWT secret key is required");

        if (_jwtConfig.SecretKey.Length < 32) // 256 bits minimum
            throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 characters) for medical security");

        if (string.IsNullOrWhiteSpace(_jwtConfig.Issuer))
            throw new InvalidOperationException("JWT issuer is required");

        if (string.IsNullOrWhiteSpace(_jwtConfig.Audience))
            throw new InvalidOperationException("JWT audience is required");

        if (_jwtConfig.AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("Access token expiration must be greater than 0");

        if (_jwtConfig.AccessTokenExpirationMinutes > 60)
            _logger.LogWarning("Access token expiration is longer than 60 minutes, consider shorter duration for medical security");

        _logger.LogInformation("JWT configuration validated successfully for medical application");
    }
}
