// BloodThinnerTracker.Web - Blazor Authentication Handler
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Web.Services;

using System.Security.Claims;
using System.Text.Encodings.Web;
// KeyVaultService usage removed; configuration-based Key Vault is used instead.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

/// <summary>
/// Authentication handler for Blazor Server that bridges HTTP authentication with AuthenticationStateProvider.
/// This handler is required for [Authorize] attributes to work properly in Blazor Server components.
/// </summary>
public class BlazorAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public BlazorAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthenticationStateProvider authStateProvider)
        : base(options, logger, encoder)
    {
        _authStateProvider = authStateProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Get authentication state from the Blazor AuthenticationStateProvider
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.NoResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating request");
            return AuthenticateResult.Fail(ex);
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Redirect to login page when authentication is required
        Response.Redirect("/login");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // Redirect to access denied page when user is authenticated but not authorized
        Response.Redirect("/access-denied");
        return Task.CompletedTask;
    }
}

// KeyVaultService is intentionally not used; secrets are provided via IConfiguration (AddAzureKeyVault) when enabled.
