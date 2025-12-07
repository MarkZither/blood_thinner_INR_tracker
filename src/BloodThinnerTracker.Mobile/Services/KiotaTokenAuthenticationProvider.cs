using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Kiota IAuthenticationProvider that attaches the bearer token from IAuthService
    /// to outgoing Kiota RequestInformation objects.
    /// </summary>
    internal class KiotaTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly IAuthService _authService;
        private readonly ILogger<KiotaTokenAuthenticationProvider> _logger;

        public KiotaTokenAuthenticationProvider(IAuthService authService, ILogger<KiotaTokenAuthenticationProvider>? logger = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? NullLogger<KiotaTokenAuthenticationProvider>.Instance;
        }

        public async Task AuthenticateRequestAsync(RequestInformation request, CancellationToken cancellationToken)
        {
            await AuthenticateRequestAsync(request, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null) throw new ArgumentNullException(nameof(request));

                var token = await _authService.GetAccessTokenAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token) && request.Headers != null)
                {
                    request.Headers["Authorization"] = new[] { $"Bearer {token}" };
                    _logger.LogDebug("KiotaTokenAuthenticationProvider attached Authorization header.");
                }
                else
                {
                    _logger.LogWarning("KiotaTokenAuthenticationProvider did not find an access token to attach.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("KiotaTokenAuthenticationProvider authentication cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                // Don't log sensitive token contents. Log the exception so callers can diagnose auth pipeline issues.
                _logger.LogError(ex, "Unexpected error while attempting to attach authentication to Kiota request.");
                // Do not rethrow here: leaving the request unauthenticated will produce a 401 which the caller can handle.
            }
        }
    }
}
