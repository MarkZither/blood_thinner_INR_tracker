// BloodThinnerTracker.Api - ID Token Validation Service for OAuth2
// Licensed under MIT License. See LICENSE file in the project root.

using Google.Apis.Auth;
using Microsoft.Identity.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BloodThinnerTracker.Api.Services.Authentication;

/// <summary>
/// Service for validating OAuth2 ID tokens from Azure AD and Google
/// </summary>
public interface IIdTokenValidationService
{
    /// <summary>
    /// Validate Google ID token
    /// </summary>
    /// <param name="idToken">ID token from Google</param>
    /// <returns>Validation result with user information</returns>
    Task<IdTokenValidationResult> ValidateGoogleTokenAsync(string idToken);

    /// <summary>
    /// Validate Azure AD ID token
    /// </summary>
    /// <param name="idToken">ID token from Azure AD</param>
    /// <returns>Validation result with user information</returns>
    Task<IdTokenValidationResult> ValidateAzureAdTokenAsync(string idToken);
}

/// <summary>
/// ID token validation result
/// </summary>
public class IdTokenValidationResult
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// External user ID from provider
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// User email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User display name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Provider name (AzureAD or Google)
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ID token validation service implementation
/// </summary>
public class IdTokenValidationService : IIdTokenValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdTokenValidationService> _logger;

    /// <summary>
    /// Initialize ID token validation service
    /// </summary>
    public IdTokenValidationService(
        IConfiguration configuration,
        ILogger<IdTokenValidationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate Google ID token
    /// </summary>
    public async Task<IdTokenValidationResult> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            _logger.LogInformation("Validating Google ID token");

            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Google ClientId not configured");
                return new IdTokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Google authentication not configured"
                };
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            _logger.LogInformation("Google ID token validated successfully for user {Email}", payload.Email);

            return new IdTokenValidationResult
            {
                IsValid = true,
                ExternalUserId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Provider = "Google"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google ID token validation failed");
            return new IdTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Google token validation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validate Azure AD ID token
    /// </summary>
    public async Task<IdTokenValidationResult> ValidateAzureAdTokenAsync(string idToken)
    {
        try
        {
            _logger.LogInformation("Validating Azure AD ID token");

            var tenantId = _configuration["Authentication:AzureAd:TenantId"];
            var clientId = _configuration["Authentication:AzureAd:ClientId"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Azure AD not configured");
                return new IdTokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure AD authentication not configured"
                };
            }

            _logger.LogDebug("Azure AD Config - TenantId: {TenantId}, ClientId: {ClientId}", tenantId, clientId);

            var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            var tokenHandler = new JwtSecurityTokenHandler();

            // Decode token to see what's in it (for debugging)
            var jwtToken = tokenHandler.ReadJwtToken(idToken);
            _logger.LogDebug("Token Issuer: {Issuer}, Audience: {Audience}",
                jwtToken.Issuer, string.Join(", ", jwtToken.Audiences));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    $"https://login.microsoftonline.com/{tenantId}/v2.0",
                    $"https://sts.windows.net/{tenantId}/",
                    $"https://login.microsoftonline.com/{tenantId}/"
                },
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    // Use Microsoft Identity Web's key resolver
                    var metadataAddress = $"{authority}/.well-known/openid-configuration";
                    var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress,
                        new OpenIdConnectConfigurationRetriever());

                    var config = configManager.GetConfigurationAsync().GetAwaiter().GetResult();
                    return config.SigningKeys;
                }
            };

            var principal = tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken);

            // Azure AD v2.0 uses short claim names by default
            // Look for claims with various possible names
            var emailClaim = principal.FindFirst("preferred_username")
                ?? principal.FindFirst("email")
                ?? principal.FindFirst("upn");

            var nameClaim = principal.FindFirst("name");

            // Object ID (oid) is the unique user identifier in Azure AD
            var oidClaim = principal.FindFirst("oid")
                ?? principal.FindFirst("sub")
                ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

            _logger.LogDebug("Azure AD Claims - oid: {Oid}, sub: {Sub}, email: {Email}, preferred_username: {PreferredUsername}, name: {Name}, upn: {Upn}",
                principal.FindFirst("oid")?.Value ?? "null",
                principal.FindFirst("sub")?.Value ?? "null",
                principal.FindFirst("email")?.Value ?? "null",
                principal.FindFirst("preferred_username")?.Value ?? "null",
                principal.FindFirst("name")?.Value ?? "null",
                principal.FindFirst("upn")?.Value ?? "null");

            if (oidClaim == null || string.IsNullOrEmpty(oidClaim.Value))
            {
                _logger.LogError("Azure AD token missing oid/sub claim - cannot identify user");
            }

            _logger.LogInformation("Azure AD ID token validated successfully for user {Email} with ExternalUserId: {ExternalUserId}",
                emailClaim?.Value, oidClaim?.Value ?? "MISSING");

            return new IdTokenValidationResult
            {
                IsValid = true,
                ExternalUserId = oidClaim?.Value ?? "",
                Email = emailClaim?.Value ?? "",
                Name = nameClaim?.Value ?? "",
                Provider = "AzureAD"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AD ID token validation failed");
            return new IdTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Azure AD token validation failed: {ex.Message}"
            };
        }
    }
}
