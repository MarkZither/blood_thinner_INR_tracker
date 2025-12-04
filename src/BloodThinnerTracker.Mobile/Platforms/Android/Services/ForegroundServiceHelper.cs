using Android.App.Job;
#nullable enable
#pragma warning disable CS8602,CS8604,CS8765
using Android.Content;
using Android.OS;

namespace BloodThinnerTracker.Mobile.Platforms.Android.Services
{
    // Helper to schedule the ForegroundSyncJob. Use from MAUI/Platform code to
    // schedule periodic work. This keeps JobScheduler wiring separate from the
    // JobService implementation.
    public static class ForegroundServiceHelper
    {
        // Job id used for scheduling
        const int JobId = 1001;
        const string PrefsName = "com.markzither.bloodthinnertracker.prefs";
        const string ScheduledKey = "job_scheduled";

        /// <summary>
        /// Schedule a periodic job. If the platform supports SetPeriodic, it will
        /// be used; otherwise it will schedule a job with the given minimum latency.
        /// intervalMillis: desired interval in milliseconds.
        /// This method is idempotent per install; it stores a flag in SharedPreferences
        /// to avoid duplicate scheduling. Pass force=true to reschedule regardless.
        /// </summary>
        public static void SchedulePeriodicJob(Context context, long intervalMillis, bool force = false, BloodThinnerTracker.Mobile.Services.ISchedulingFlagStore? store = null)
        {
            if (context == null) return;
            try
            {
                // If a test-provided store is available, consult it. Otherwise use SharedPreferences.
                bool already;
                if (store != null)
                {
                    already = store.IsScheduled();
                }
                else
                {
                    var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                    already = prefs.GetBoolean(ScheduledKey, false);
                }

                if (already && !force)
                {
                    global::Android.Util.Log.Debug("ForegroundServiceHelper", "Periodic job already scheduled; skipping");
                    return;
                }

                var cs = context.GetSystemService(Context.JobSchedulerService) as JobScheduler;
                if (cs == null) return;

                var component = new global::Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(ForegroundSyncJob)));
                var builder = new JobInfo.Builder(JobId, component)
                    .SetRequiredNetworkType(NetworkType.Any)
                    .SetPersisted(false)
                    .SetBackoffCriteria(10_000, BackoffPolicy.Linear);

                // Prefer SetPeriodic when available (API 24+)
                if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.N)
                {
                    builder.SetPeriodic(intervalMillis);
                }
                else
                {
                    // fallback: schedule with minimum latency and reschedule at the end of the job
                    builder.SetMinimumLatency(intervalMillis);
                }

                var result = cs.Schedule(builder.Build());
                if (result == JobScheduler.ResultSuccess)
                {
                    if (store != null)
                    {
                        store.SetScheduled(true);
                    }
                    else
                    {
                        var prefs2 = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                        prefs2.Edit().PutBoolean(ScheduledKey, true).Commit();
                    }

                    global::Android.Util.Log.Debug("ForegroundServiceHelper", "Scheduled periodic job successfully");
                }
                else
                {
                    global::Android.Util.Log.Warn("ForegroundServiceHelper", $"JobScheduler failed to schedule (result={result})");
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Warn("ForegroundServiceHelper", $"Failed to schedule job: {ex}");
            }
        }

        /// <summary>
        /// Cancel the scheduled job and clear the persisted scheduled flag.
        /// </summary>
        public static void CancelScheduledJob(Context context, BloodThinnerTracker.Mobile.Services.ISchedulingFlagStore? store = null)
        {
            if (context == null) return;
            try
            {
                var cs = context.GetSystemService(Context.JobSchedulerService) as JobScheduler;
                cs?.Cancel(JobId);
                if (store != null)
                {
                    store.SetScheduled(false);
                }
                else
                {
                    var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
                    prefs.Edit().Remove(ScheduledKey).Commit();
                }
                global::Android.Util.Log.Debug("ForegroundServiceHelper", "Cancelled periodic job and cleared scheduled flag");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Warn("ForegroundServiceHelper", $"Failed to cancel job: {ex}");
            }
        }
    }
}
