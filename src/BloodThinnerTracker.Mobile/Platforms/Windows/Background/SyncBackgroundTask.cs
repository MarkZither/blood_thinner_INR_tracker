using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using BloodThinnerTracker.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Windows.Background
{
    /// <summary>
    /// Windows background task that performs periodic sync of INR data.
    /// Mirrors the Android <c>ForegroundSyncJob</c> implementation with feature parity:
    /// <list type="bullet">
    ///   <item>Uses <see cref="ICacheSyncWorker.SyncOnceAsync"/> for sync work</item>
    ///   <item>Computes outstanding actions via <see cref="IInrRepository.GetRecentAsync"/></item>
    ///   <item>Supports cooperative cancellation via <see cref="CancellationToken"/></item>
    ///   <item>Performance logging with <see cref="Stopwatch"/></item>
    ///   <item>Updates badge via <see cref="WindowsBadgeHelper"/></item>
    /// </list>
    /// </summary>
    public sealed class SyncBackgroundTask : IBackgroundTask
    {
        private CancellationTokenSource? _cts;

        /// <summary>
        /// Entry point for the background task.
        /// </summary>
        /// <param name="taskInstance">The background task instance.</param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get deferral for async work
            var deferral = taskInstance.GetDeferral();

            // Set up cancellation handling
            _cts = new CancellationTokenSource();
            taskInstance.Canceled += OnCanceled;

            Task.Run(async () =>
            {
                ILogger? logger = null;

                try
                {
                    // Check if service provider is initialized
                    if (!WindowsServiceProvider.IsInitialized)
                    {
                        // Background task ran before app initialized the provider
                        // This can happen if the task runs on system boot before app startup
                        Debug.WriteLine("SyncBackgroundTask: WindowsServiceProvider not initialized");
                        return;
                    }

                    using var scope = WindowsServiceProvider.CreateScope();
                    logger = scope.ServiceProvider.GetService<ILogger<SyncBackgroundTask>>();

                    int outstandingCount = 0;

                    // Perform sync via ICacheSyncWorker
                    try
                    {
                        var worker = scope.ServiceProvider.GetService<ICacheSyncWorker>();
                        if (worker != null)
                        {
                            logger?.LogInformation("SyncBackgroundTask starting sync");
                            var sw = Stopwatch.StartNew();

                            try
                            {
                                await worker.SyncOnceAsync(_cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                logger?.LogInformation("SyncBackgroundTask cancelled during sync");
                                return;
                            }
                            catch (ApiAuthenticationException aex)
                            {
                                // Token expired or unauthorized - log and stop gracefully
                                logger?.LogInformation(aex, "SyncBackgroundTask: authentication required during sync");
                            }

                            sw.Stop();
                            logger?.LogInformation("SyncBackgroundTask completed sync in {ElapsedMs}ms",
                                sw.ElapsedMilliseconds);
                        }
                        else
                        {
                            logger?.LogWarning("SyncBackgroundTask: ICacheSyncWorker not registered");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "SyncBackgroundTask failed to run sync");
                    }

                    // Compute outstanding actions using repository. If no items are present
                    // in the DB (worker may not have persisted), attempt to fetch from API
                    // and persist them into the canonical DB so the badge reflects persisted state.
                    try
                    {
                        var repo = scope.ServiceProvider.GetService<IInrRepository>();
                        if (repo != null)
                        {
                            var items = await repo.GetRecentAsync(50).ConfigureAwait(false);
                            if (items == null || !items.Any())
                            {
                                // Try to fetch directly from API and persist
                                try
                                {
                                    var api = scope.ServiceProvider.GetService<IInrService>();
                                    if (api != null)
                                    {
                                        var fetched = await api.GetRecentAsync(50).ConfigureAwait(false);
                                        if (fetched != null && fetched.Any())
                                        {
                                            try
                                            {
                                                await repo.SaveRangeAsync(fetched).ConfigureAwait(false);
                                            }
                                            catch (ApiAuthenticationException aex)
                                            {
                                                logger?.LogInformation(aex, "SyncBackgroundTask: authentication required when persisting API results");
                                            }
                                            catch (Exception perEx)
                                            {
                                                logger?.LogWarning(perEx, "SyncBackgroundTask: failed to persist fetched API results");
                                            }
                                        }
                                    }
                                }
                                catch (ApiAuthenticationException aex)
                                {
                                    logger?.LogInformation(aex, "SyncBackgroundTask: authentication required when fetching API results");
                                }
                                catch (Exception ex)
                                {
                                    logger?.LogWarning(ex, "SyncBackgroundTask: failed to fetch API results for persistence");
                                }

                                // Re-read from repo after attempted persistence
                                items = await repo.GetRecentAsync(50).ConfigureAwait(false);
                            }

                            outstandingCount = items?.Count(i => !i.ReviewedByProvider) ?? 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "SyncBackgroundTask failed to compute outstanding count");
                    }

                    // Update Windows badge
                    try
                    {
                        WindowsBadgeHelper.UpdateBadge(outstandingCount, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "SyncBackgroundTask badge update failed");
                    }

                    logger?.LogInformation("SyncBackgroundTask completed. Outstanding: {Count}", outstandingCount);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "SyncBackgroundTask failed with unhandled exception");
                }
                finally
                {
                    taskInstance.Canceled -= OnCanceled;
                    _cts?.Dispose();
                    _cts = null;
                    deferral.Complete();
                }
            });
        }

        /// <summary>
        /// Handle task cancellation.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Debug.WriteLine($"SyncBackgroundTask cancelled. Reason: {reason}");
            _cts?.Cancel();
        }
    }
}
