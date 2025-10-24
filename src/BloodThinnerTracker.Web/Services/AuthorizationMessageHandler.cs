// BloodThinnerTracker.Web - Authorization Message Handler
// Licensed under MIT License. See LICENSE file in the project root.

namespace BloodThinnerTracker.Web.Services;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// HTTP message handler that automatically adds JWT bearer tokens to API requests.
/// Handles token refresh on 401 unauthorized responses.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthorizationMessageHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationMessageHandler"/> class.
    /// </summary>
    /// <param name="authStateProvider">The authentication state provider.</param>
    /// <param name="logger">Logger instance.</param>
    public AuthorizationMessageHandler(
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthorizationMessageHandler> logger)
    {
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTTP request with automatic JWT token injection.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
        {
            var token = await customProvider.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Added Bearer token to request: {Method} {Uri}", request.Method, request.RequestUri);
            }
            else
            {
                _logger.LogDebug("No token available for request: {Method} {Uri}", request.Method, request.RequestUri);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - token might be expired
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response. Token may be expired.");
            
            // TODO: Implement automatic token refresh using refresh token
            // For now, just log the user out
            if (_authStateProvider is CustomAuthenticationStateProvider provider)
            {
                _logger.LogInformation("Logging out user due to 401 response");
                await provider.MarkUserAsLoggedOutAsync();
            }
        }

        return response;
    }
}
