// BloodThinnerTracker.Api - Medical Authentication Configuration Extensions
// Licensed under MIT License. See LICENSE file in the project root.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BloodThinnerTracker.Shared.Models.Authentication;
using BloodThinnerTracker.Api.Services.Authentication;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Authentication configuration extensions for medical application
/// Provides secure authentication setup with medical data protection
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configure medical authentication services including JWT, Azure AD, and Google OAuth
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection ConfigureMedicalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind authentication configuration
        var authConfig = new AuthenticationConfig();
        configuration.GetSection("Authentication").Bind(authConfig);
        services.AddSingleton(authConfig);
        services.AddSingleton(authConfig.Jwt);
        services.AddSingleton(authConfig.AzureAd);
        services.AddSingleton(authConfig.Google);
        services.AddSingleton(authConfig.MedicalSecurity);

        // Validate authentication configuration
        ValidateAuthenticationConfig(authConfig);

        // Add password hasher for local authentication
        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();

        // Add JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add authentication service
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = authConfig.Jwt.RequireHttpsMetadata;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.Jwt.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = authConfig.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = authConfig.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1), // Reduced for medical security
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Add custom JWT events for medical security
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
                    logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    logger.LogDebug("JWT token validated for user {UserId}", userId);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
                    logger.LogInformation("JWT authentication challenge: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        // Configure Google OAuth if enabled
        if (!string.IsNullOrEmpty(authConfig.Google.ClientId))
        {
            services.AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = authConfig.Google.ClientId;
                    options.ClientSecret = authConfig.Google.ClientSecret;
                    options.CallbackPath = authConfig.Google.CallbackPath;
                    
                    // Add medical data scopes
                    foreach (var scope in authConfig.Google.Scopes)
                    {
                        options.Scope.Add(scope);
                    }

                    // Configure events for medical security
                    options.Events.OnCreatingTicket = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<GoogleHandler>>();
                        logger.LogInformation("Google OAuth ticket created for user {Email}", 
                            context.Principal?.FindFirst("email")?.Value);
                    };
                });
        }

        // Configure Azure AD OAuth if enabled
        if (!string.IsNullOrEmpty(authConfig.AzureAd.ClientId))
        {
            services.AddAuthentication()
                .AddOpenIdConnect("AzureAD", options =>
                {
                    options.Authority = $"{authConfig.AzureAd.Instance}{authConfig.AzureAd.TenantId}";
                    options.ClientId = authConfig.AzureAd.ClientId;
                    options.ClientSecret = authConfig.AzureAd.ClientSecret;
                    options.CallbackPath = authConfig.AzureAd.CallbackPath;
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                    options.RequireHttpsMetadata = authConfig.Jwt.RequireHttpsMetadata;

                    // Add medical data scopes
                    foreach (var scope in authConfig.AzureAd.Scopes)
                    {
                        options.Scope.Add(scope);
                    }

                    // Configure events for medical security
                    options.Events.OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenIdConnectHandler>>();
                        logger.LogWarning("Azure AD authentication failed: {Exception}", context.Exception?.Message);
                        return Task.CompletedTask;
                    };
                });
        }

        // Add authorization policies for medical data
        services.AddAuthorization(options =>
        {
            // Medical data access policy
            options.AddPolicy("MedicalDataAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("medical_data_access", "true");
            });

            // Medication management policy
            options.AddPolicy("MedicationManagement", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("permission", "medication:write");
            });

            // INR data access policy
            options.AddPolicy("INRDataAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("permission", "inr:read");
            });

            // Admin access policy
            options.AddPolicy("AdminAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin", "HealthcareProvider");
            });
        });

        return services;
    }

    /// <summary>
    /// Validate authentication configuration for medical security requirements
    /// </summary>
    /// <param name="authConfig">Authentication configuration to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    private static void ValidateAuthenticationConfig(AuthenticationConfig authConfig)
    {
        // JWT validation
        if (string.IsNullOrWhiteSpace(authConfig.Jwt.SecretKey))
            throw new InvalidOperationException("JWT SecretKey is required for medical authentication");

        if (authConfig.Jwt.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 256 bits (32 characters) for medical security");

        if (string.IsNullOrWhiteSpace(authConfig.Jwt.Issuer))
            throw new InvalidOperationException("JWT Issuer is required");

        if (string.IsNullOrWhiteSpace(authConfig.Jwt.Audience))
            throw new InvalidOperationException("JWT Audience is required");

        // Medical security validation
        if (authConfig.MedicalSecurity.SessionTimeoutMinutes > 120)
            throw new InvalidOperationException("Session timeout cannot exceed 120 minutes for medical security");

        if (authConfig.MedicalSecurity.MaxFailedLoginAttempts > 10)
            throw new InvalidOperationException("Maximum failed login attempts cannot exceed 10 for security");

        if (authConfig.Jwt.AccessTokenExpirationMinutes > 60)
            throw new InvalidOperationException("Access token expiration cannot exceed 60 minutes for medical security");
    }
}