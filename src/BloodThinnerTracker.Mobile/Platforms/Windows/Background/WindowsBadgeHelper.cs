using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Extensions.Logging;

namespace BloodThinnerTracker.Mobile.Platforms.Windows.Background
{
    /// <summary>
    /// Helper class for updating the Windows app tile badge.
    /// Uses <see cref="BadgeUpdateManager"/> to display a numeric badge
    /// on the app's tile in the Start menu and taskbar.
    /// </summary>
    public static class WindowsBadgeHelper
    {
        /// <summary>
        /// Update the app badge with the specified count.
        /// </summary>
        /// <param name="count">The number to display on the badge. Use 0 to clear the badge.</param>
        /// <param name="logger">Optional logger for error reporting.</param>
        public static void UpdateBadge(int count, ILogger? logger = null)
        {
            try
            {
                if (count <= 0)
                {
                    ClearBadge(logger);
                    return;
                }

                // Create badge XML with numeric value
                // Badge values: "none", "activity", "alert", "attention", "available", "away",
                // "busy", "newMessage", "paused", "playing", "unavailable", "error", or a number 1-99
                var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
                var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
                if (badgeElement != null)
                {
                    // Badge supports 1-99; numbers over 99 display as "99+"
                    badgeElement.SetAttribute("value", Math.Min(count, 99).ToString());
                }

                var badgeNotification = new BadgeNotification(badgeXml);
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeNotification);

                logger?.LogDebug("Updated Windows badge to {Count}", count);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to update Windows badge");
            }
        }

        /// <summary>
        /// Clear the app badge.
        /// </summary>
        /// <param name="logger">Optional logger for error reporting.</param>
        public static void ClearBadge(ILogger? logger = null)
        {
            try
            {
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                logger?.LogDebug("Cleared Windows badge");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to clear Windows badge");
            }
        }
    }
}
