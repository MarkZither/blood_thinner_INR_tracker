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
            var hasToken = !string.IsNullOrEmpty(token);

            if (hasToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Token retrieved: {HasToken}, added Bearer token to request: {Method} {Uri}",
                    hasToken, request.Method, request.RequestUri);
            }
            else
            {
                _logger.LogDebug("Token retrieved: {HasToken}, no token available for request: {Method} {Uri}",
                    hasToken, request.Method, request.RequestUri);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - token might be expired
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response from {Uri}, attempting token refresh",
                request.RequestUri);

            if (_authStateProvider is CustomAuthenticationStateProvider provider)
            {
                // Check if we can refresh the authentication state
                var authState = await provider.GetAuthenticationStateAsync();
                
                // If still authenticated after state check (which triggers refresh), retry the request
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("Authentication state refreshed, retrying request to {Uri}", request.RequestUri);
                    
                    // Clone the request for retry
                    var retryRequest = await CloneHttpRequestAsync(request);
                    
                    // Get the new token
                    var newToken = await provider.GetTokenAsync();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        var retryResponse = await base.SendAsync(retryRequest, cancellationToken);
                        
                        if (retryResponse.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Request retry successful after token refresh");
                            return retryResponse;
                        }
                    }
                }
                
                // If refresh failed or user is not authenticated, log them out
                _logger.LogWarning("Token refresh failed or user not authenticated, logging out");
                await provider.MarkUserAsLoggedOutAsync();
            }
        }

        return response;
    }

    /// <summary>
    /// Clones an HTTP request message for retry scenarios.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // Copy content headers
            if (request.Content.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return clone;
    }
}
