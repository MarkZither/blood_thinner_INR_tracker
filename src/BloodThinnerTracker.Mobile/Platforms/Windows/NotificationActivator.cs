using System;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BloodThinnerTracker.Mobile.Platforms.Windows
{
    /// <summary>
    /// COM activator required by DesktopNotificationManagerCompat.RegisterAumidAndComServer&lt;T&gt;
    /// for unpackaged Windows apps using Microsoft.Toolkit.Uwp.Notifications 7.x.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("A1B2C3D4-E5F6-4789-ABCD-1234567890AB")] // Unique GUID for the activator
    [ComVisible(true)]
#pragma warning disable CS0618 // NotificationActivator is obsolete but required for 7.x RegisterAumidAndComServer<T>
    public class AppNotificationActivator : Microsoft.Toolkit.Uwp.Notifications.NotificationActivator
#pragma warning restore CS0618
    {
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
        {
            // Handle notification activation (e.g., navigate to a page, execute an action).
            // For now, just log activation.
            System.Diagnostics.Debug.WriteLine($"NotificationActivator.OnActivated: args={invokedArgs}, aumid={appUserModelId}");
        }
    }
}
