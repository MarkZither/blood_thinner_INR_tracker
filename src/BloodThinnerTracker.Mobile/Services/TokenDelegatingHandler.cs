using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// DelegatingHandler that attaches a bearer token from IAuthService to outgoing requests
    /// and retries once after refreshing the token when a 401 is returned.
    /// </summary>
    public class TokenDelegatingHandler : DelegatingHandler
    {
        private readonly IAuthService _auth;
        private readonly ILogger<TokenDelegatingHandler> _logger;

        public TokenDelegatingHandler(IAuthService auth, ILogger<TokenDelegatingHandler> logger)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Attach token if available
            var token = await _auth.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("Request to {Uri} returned 401 - attempting token refresh and retry", request.RequestUri);
                try
                {
                    var refreshed = await _auth.RefreshAccessTokenAsync();
                    if (refreshed)
                    {
                        var newToken = await _auth.GetAccessTokenAsync();
                        if (!string.IsNullOrEmpty(newToken))
                        {
                            // clone request for retry since HttpRequestMessage can be consumed
                            using var retry = CloneHttpRequestMessage(request);
                            retry.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                            response.Dispose();
                            response = await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Token refresh attempt failed");
                }
            }

            return response;
        }

        private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy the request content (if any)
            if (request.Content != null)
            {
                var ms = new System.IO.MemoryStream();
                request.Content.CopyToAsync(ms).GetAwaiter().GetResult();
                ms.Position = 0;
                clone.Content = new StreamContent(ms);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Copy headers
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
