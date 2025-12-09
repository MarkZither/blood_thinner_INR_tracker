#if ANDROID
using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
// removed stray `using Android;` which causes namespace resolution issues
using Microsoft.Maui.ApplicationModel;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    public static class NotificationPermissionHelper
    {
        public const int RequestPostNotificationsCode = 1001;

        public static bool AreNotificationsEnabled()
        {
            try
            {
                var ctx = global::Android.App.Application.Context;
                if (ctx == null) return false;
                var nm = NotificationManagerCompat.From(ctx);
                if (nm == null) return false;
                return nm.AreNotificationsEnabled();
            }
            catch
            {
                return false;
            }
        }

        public static void RequestPostNotificationsIfNeeded()
        {
            try
            {
                if ((int)Build.VERSION.SdkInt < (int)BuildVersionCodes.Tiramisu)
                    return;

                var activity = Platform.CurrentActivity;
                if (activity == null)
                {
                    // Can't request - open app settings as fallback
                    OpenAppNotificationSettings();
                    return;
                }

                if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(activity, new[] { global::Android.Manifest.Permission.PostNotifications }, RequestPostNotificationsCode);
                }
            }
            catch
            {
                OpenAppNotificationSettings();
            }
        }

        public static void OpenAppNotificationSettings()
        {
            try
            {
                var ctx = global::Android.App.Application.Context;
                if (ctx == null) return;

                var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionAppNotificationSettings)
                    .PutExtra(global::Android.Provider.Settings.ExtraAppPackage, ctx.PackageName)
                    .AddFlags(global::Android.Content.ActivityFlags.NewTask);
                ctx.StartActivity(intent);
            }
            catch
            {
                // Best-effort
            }
        }
    }
}
#endif
