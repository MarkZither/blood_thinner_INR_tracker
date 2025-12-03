using System.Threading;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Lightweight interface exposing a single sync operation that can be
    /// invoked by background workers (JobService) or the hosted service.
    /// </summary>
    public interface ICacheSyncWorker
    {
        /// <summary>
        /// Perform a one-time sync. Implementations should honor the cancellation token
        /// to allow graceful shutdown of background work.
        /// </summary>
        Task SyncOnceAsync(CancellationToken cancellationToken = default);
    }
}
