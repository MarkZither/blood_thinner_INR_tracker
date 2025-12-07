using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace BloodThinnerTracker.Mobile.Services
{
    // Minimal no-op authentication provider for Kiota when running against non-authenticated APIs.
    internal class NoAuthProvider : IAuthenticationProvider
    {
        public Task AuthenticateRequestAsync(RequestInformation request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AuthenticateRequestAsync(RequestInformation request, System.Collections.Generic.Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
