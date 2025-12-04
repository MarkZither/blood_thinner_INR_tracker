using System;
using Android.Content;
using Android.Net;
using Android.OS;

namespace BloodThinnerTracker.Mobile.Platforms.Android.Services
{
    /// <summary>
    /// Best-effort badge updater that attempts multiple vendor-specific intents
    /// and approaches to increase the chance a launcher will update the app badge.
    /// This is inherently best-effort and will not work on all OEM launchers.
    /// </summary>
    public static class BadgeHelper
    {
        private const string Tag = "BadgeHelper";

        public static void UpdateBadge(Context context, int count)
        {
            // Many modern launchers honor Notification.Builder.SetNumber + NotificationChannel.showBadge
            // Older OEMs require vendor-specific broadcast intents; attempt a few common ones.

            try
            {
                // Sony
                try
                {
                    var intent = new Intent("com.sonyericsson.home.action.UPDATE_BADGE");
                    intent.PutExtra("com.sonyericsson.home.intent.extra.badge_count", count);
                    intent.PutExtra("com.sonyericsson.home.intent.extra.badge_package_name", context.PackageName);
                    var launchClass = context.PackageManager.GetLaunchIntentForPackage(context.PackageName)?.Component?.ClassName;
                    if (launchClass != null) intent.PutExtra("com.sonyericsson.home.intent.extra.badge_class_name", launchClass);
                    context.SendBroadcast(intent);
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Debug(Tag, $"Sony badge update failed (expected on non-Sony): {ex.Message}");
                }

                // HTC
                try
                {
                    var intent = new Intent("com.htc.launcher.action.SET_BADGE");
                    intent.PutExtra("com.htc.launcher.extra.COMPONENT", context.PackageName + "/" + (context.PackageManager.GetLaunchIntentForPackage(context.PackageName)?.Component?.ClassName ?? ""));
                    intent.PutExtra("com.htc.launcher.extra.BADGE_COUNT", count);
                    context.SendBroadcast(intent);
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Debug(Tag, $"HTC badge update failed (expected on non-HTC): {ex.Message}");
                }

                // Samsung / generic - some implementations listen for this
                try
                {
                    var intent = new Intent("com.sec.android.app.badge.update");
                    intent.PutExtra("badge_count", count);
                    intent.PutExtra("badge_count_package_name", context.PackageName);
                    intent.PutExtra("badge_count_class_name", context.PackageManager.GetLaunchIntentForPackage(context.PackageName)?.Component?.ClassName ?? "");
                    context.SendBroadcast(intent);
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Debug(Tag, $"Samsung badge update failed (expected on non-Samsung): {ex.Message}");
                }

                // Apex/Nova: use ACTION_APPLICATION_MESSAGE (common pattern)
                try
                {
                    var launchClass = context.PackageManager.GetLaunchIntentForPackage(context.PackageName)?.Component?.ClassName;
                    if (!string.IsNullOrEmpty(launchClass))
                    {
                        var intent = new Intent("com.anddoes.launcher.COUNTER_CHANGED");
                        intent.PutExtra("package", context.PackageName);
                        intent.PutExtra("class", launchClass);
                        intent.PutExtra("count", count);
                        context.SendBroadcast(intent);
                    }
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Debug(Tag, $"Apex/Nova badge update failed: {ex.Message}");
                }

                // Huawei / some Xiaomi launchers: try content provider approach
                try
                {
                    var launchClass = context.PackageManager.GetLaunchIntentForPackage(context.PackageName)?.Component?.ClassName;
                    if (!string.IsNullOrEmpty(launchClass))
                    {
                        var uri = Uri.Parse($"content://com.huawei.android.launcher.settings/badge/");
                        var values = new Android.Content.ContentValues();
                        values.Put("package", context.PackageName);
                        values.Put("class", launchClass);
                        values.Put("badge", count);
                        try
                        {
                            context.ContentResolver.Insert(uri, values);
                        }
                        catch (Exception ex)
                        {
                            Android.Util.Log.Debug(Tag, $"Huawei content provider badge insert failed: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Debug(Tag, $"Huawei badge update failed (expected on non-Huawei): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn(Tag, $"Badge update failed unexpectedly: {ex.Message}");
            }
        }
    }
}
