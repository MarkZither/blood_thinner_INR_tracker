using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Android.Services
{
    // Android JobService that performs a periodic sync and posts a notification
    // showing the number of outstanding actions as a badge. It uses the
    // AndroidServiceProvider bridge to obtain a scoped ICacheSyncWorker and
    // IInrRepository. Jobs support cooperative cancellation via JobService
    // lifecycle events.

    [Service(Name = "com.markzither.bloodthinnertracker.ForegroundSyncJob", Permission = "android.permission.BIND_JOB_SERVICE")]
    public class ForegroundSyncJob : JobService
    {
        const string ChannelId = "BTTforeground_sync_channel";
        const int NotificationId = 0xB100; // arbitrary id
        const string PrefsName = "com.markzither.bloodthinnertracker.prefs";
        const string BadgeKey = "outstanding_actions_count";

        // Track running jobs so we can cancel them on OnStopJob
        static readonly ConcurrentDictionary<int, CancellationTokenSource> _runningJobs = new();

        public override bool OnStartJob(JobParameters @params)
        {
            var jobId = @params.JobId;
            var cts = new CancellationTokenSource();
            _runningJobs[jobId] = cts;

            Task.Run(async () =>
            {
                var ctx = Application.Context;
                int outstandingCount = 0;

                ILogger? logger = null;

                try
                {
                    using var scope = BloodThinnerTracker.Mobile.Platforms.Android.AndroidServiceProvider.CreateScope();
                    logger = scope.ServiceProvider.GetService(typeof(ILogger<ForegroundSyncJob>)) as ILogger<ForegroundSyncJob>;

                    // Attempt to run a full sync via the shared worker
                    try
                    {
                        var worker = scope.ServiceProvider.GetService(typeof(BloodThinnerTracker.Mobile.Services.ICacheSyncWorker)) as BloodThinnerTracker.Mobile.Services.ICacheSyncWorker;
                        if (worker != null)
                        {
                            logger?.LogInformation("ForegroundSyncJob #{JobId} starting worker sync", jobId);
                            var sw = Stopwatch.StartNew();
                            try
                            {
                                await worker.SyncOnceAsync(cts.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                logger?.LogInformation("ForegroundSyncJob #{JobId} cancelled during sync", jobId);
                                return;
                            }
                            catch (ApiAuthenticationException aex)
                            {
                                logger?.LogInformation(aex, "ForegroundSyncJob #{JobId}: authentication required during sync", jobId);
                            }

                            sw.Stop();
                            logger?.LogInformation("ForegroundSyncJob #{JobId} completed worker sync in {ElapsedMs}ms", jobId, sw.ElapsedMilliseconds);
                        }
                        else
                        {
                            logger?.LogWarning("ForegroundSyncJob #{JobId} no ICacheSyncWorker registered", jobId);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger != null) logger.LogWarning(ex, "ForegroundSyncJob #{JobId} failed to run sync", jobId);
                        else Log.Warn("ForegroundSyncJob", $"Sync failed: {ex}");
                    }

                    // Compute outstanding actions using repository. If the local DB is empty (worker didn't
                    // persist anything), attempt to fetch from the API and persist so badge counts
                    // reflect canonical DB state.
                    try
                    {
                        var repo = scope.ServiceProvider.GetService(typeof(BloodThinnerTracker.Mobile.Services.IInrRepository)) as BloodThinnerTracker.Mobile.Services.IInrRepository;
                        if (repo != null)
                        {
                            var items = await repo.GetRecentAsync(50).ConfigureAwait(false);
                            if (items == null || !items.Any())
                            {
                                try
                                {
                                    var api = scope.ServiceProvider.GetService(typeof(BloodThinnerTracker.Mobile.Services.IInrService)) as BloodThinnerTracker.Mobile.Services.IInrService;
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
                                                logger?.LogInformation(aex, "ForegroundSyncJob: authentication required when persisting API results");
                                            }
                                            catch (Exception perEx)
                                            {
                                                logger?.LogWarning(perEx, "ForegroundSyncJob: failed to persist fetched API results");
                                            }
                                        }
                                    }
                                }
                                catch (ApiAuthenticationException aex)
                                {
                                    logger?.LogInformation(aex, "ForegroundSyncJob: authentication required when fetching API results");
                                }
                                catch (Exception ex)
                                {
                                    logger?.LogWarning(ex, "ForegroundSyncJob: failed to fetch API results for persistence");
                                }

                                // Re-read from repo after attempted persistence
                                items = await repo.GetRecentAsync(50).ConfigureAwait(false);
                            }

                            outstandingCount = items?.Count(i => !i.ReviewedByProvider) ?? 0;
                        }
                        else
                        {
                            // fallback: read last-known value and increment slightly to indicate activity
                            var prefs = ctx.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                            outstandingCount = prefs.GetInt(BadgeKey, 0) + 1;
                            prefs.Edit().PutInt(BadgeKey, outstandingCount).Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger != null) logger.LogWarning(ex, "ForegroundSyncJob #{JobId} failed to compute outstanding count", jobId);
                        else Log.Warn("ForegroundSyncJob", $"Compute outstanding failed: {ex}");
                        var prefs = ctx.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                        outstandingCount = prefs.GetInt(BadgeKey, 0) + 1;
                        prefs.Edit().PutInt(BadgeKey, outstandingCount).Commit();
                    }

                    // Persist, attempt vendor badge update, and post notification
                    var finalPrefs = ctx.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                    finalPrefs.Edit().PutInt(BadgeKey, outstandingCount).Commit();

                    try
                    {
                        // Try vendor-specific adapters for broader launcher compatibility
                        BadgeHelper.UpdateBadge(ctx, outstandingCount);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "ForegroundSyncJob #{JobId} badge update failed", jobId);
                    }

                    PostNotification(ctx, outstandingCount);
                }
                finally
                {
                    // Remove and dispose CTS
                    if (_runningJobs.TryRemove(jobId, out var existing))
                    {
                        try { existing.Dispose(); } catch { }
                    }

                    try { JobFinished(@params, false); } catch { }
                }
            }, cts.Token);

            return true; // still work running
        }

        public override bool OnStopJob(JobParameters @params)
        {
            // Try to cancel the running job cooperatively
            if (_runningJobs.TryRemove(@params.JobId, out var cts))
            {
                try { cts.Cancel(); } catch { }
                try { cts.Dispose(); } catch { }
            }

            // Return true to indicate the job should be rescheduled if it was interrupted
            return true;
        }

        void PostNotification(Context context, int outstandingCount)
        {
            var nm = (NotificationManager)context.GetSystemService(Context.NotificationService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = nm.GetNotificationChannel(ChannelId);
                if (channel == null)
                {
                    var ch = new NotificationChannel(ChannelId, "Background Sync", NotificationImportance.Low)
                    {
                        Description = "Background sync notifications"
                    };
                    ch.SetShowBadge(true);
                    nm.CreateNotificationChannel(ch);
                }
            }

            var packageName = context.PackageName;
            Intent launchIntent = context.PackageManager.GetLaunchIntentForPackage(packageName) ?? new Intent(context, typeof(Activity));
            launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            var pending = PendingIntent.GetActivity(context, 0, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            var title = "Blood Thinner Tracker";
            var text = outstandingCount == 1 ? "1 outstanding action" : $"{outstandingCount} outstanding actions";

            Notification notification;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var builder = new Notification.Builder(context, ChannelId)
                    .SetContentTitle(title)
                    .SetContentText(text)
                    .SetSmallIcon(context.ApplicationInfo.Icon)
                    .SetContentIntent(pending)
                    .SetAutoCancel(true)
                    .SetNumber(outstandingCount);

                builder.SetPriority((int)NotificationPriority.Low);
                notification = builder.Build();
            }
            else
            {
                var builder = new Notification.Builder(context)
                    .SetContentTitle(title)
                    .SetContentText(text)
                    .SetSmallIcon(context.ApplicationInfo.Icon)
                    .SetContentIntent(pending)
                    .SetAutoCancel(true)
                    .SetNumber(outstandingCount);

                builder.SetPriority((int)NotificationPriority.Low);
                notification = builder.Build();
            }

            nm.Notify(NotificationId, notification);
        }
    }
}
