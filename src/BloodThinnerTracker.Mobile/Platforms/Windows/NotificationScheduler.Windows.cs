using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
// Windows.* types are referenced fully-qualified (global::Windows.*) to avoid
// accidental resolution against the local `BloodThinnerTracker.Mobile.Platforms.Windows` namespace

namespace BloodThinnerTracker.Mobile.Platforms.Windows
{
    // Minimal Windows implementation of INotificationScheduler using ScheduledToastNotification
    public class NotificationSchedulerWindows : Services.INotificationScheduler
    {
        private const string DefaultAumid = "MarkZither.BloodThinnerTracker.Mobile";
        private readonly string _storePath;

        public NotificationSchedulerWindows()
        {
            _storePath = Path.Combine(FileSystem.AppDataDirectory, "scheduled_reminders.json");
            // Reconcile store with OS scheduled toasts
            _ = ReconcileWithOsAsync();
        }

        public async Task<IEnumerable<Services.NotificationInfo>> GetAllScheduledAsync()
        {
            var results = new List<Services.NotificationInfo>();

            try
            {
                global::Windows.UI.Notifications.ToastNotifier? notifier = null;
                try
                {
                    notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(DefaultAumid);
                }
                catch
                {
                    // Fall back to default (may still throw) — we handle exceptions below
                    try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(); } catch { notifier = null; }
                }
                if (notifier == null) return results;
                var scheduled = notifier.GetScheduledToastNotifications();
                var store = await ReadStoreAsync();
                foreach (var s in scheduled)
                {
                    var idGuid = Guid.TryParse(s.Id, out var g) ? g : Guid.Empty;
                    // Try to find store entry by Id first, then fallback to matching by delivery
                    // time and content when Id is not available (unpackaged scenarios).
                    var matched = store.FirstOrDefault(x => x.Id == idGuid);
                    if (matched == null)
                    {
                        matched = store.FirstOrDefault(x => StoreEntryMatchesScheduled(x, s));
                    }

                    results.Add(new Services.NotificationInfo(
                        matched != null ? matched.Id : idGuid,
                        matched != null ? matched.TriggerTime : s.DeliveryTime,
                        matched?.Title ?? string.Empty,
                        matched?.Body ?? string.Empty,
                        IsScheduled: true,
                        RequestCode: null,
                        AlarmType: null
                    ));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.GetAllScheduledAsync: {ex}");
            }

            return await Task.FromResult(results);
        }

        public async Task ScheduleAsync(Services.NotificationRequest request)
        {
            try
            {
                // Decide whether to set ScheduledToastNotification.Id or embed the id
                // in the toast launch args depending on whether the process has an
                // AppUserModelID (AUMID) / packaged identity. Setting the Id often
                // fails in unpackaged scenarios (COM HRESULT 0x803E0120), so avoid
                // attempting it when we detect no AUMID.
                var hasAumid = HasExplicitAppUserModelId();
                var launchArg = hasAumid ? null : $"id={request.Id}";
                var toastXml = CreateToastXml(request.Title ?? "Reminder", request.Body ?? string.Empty, launch: launchArg);
                var when = request.TriggerTime.ToLocalTime();
                var scheduled = new global::Windows.UI.Notifications.ScheduledToastNotification(toastXml, when);

                if (hasAumid)
                {
                    // Only attempt to set the Id when we have an AUMID; still tolerate failures.
                    try
                    {
                        scheduled.Id = request.Id.ToString();
                    }
                    catch (Exception ex)
                    {
                        var hr = ex.HResult;
                        System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows: Failed to set ScheduledToastNotification.Id='{request.Id}' (HR=0x{hr:X8}): {ex}");
                    }
                }

                global::Windows.UI.Notifications.ToastNotifier? notifier = null;
                try
                {
                    try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(DefaultAumid); }
                    catch { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(); }
                    notifier.AddToSchedule(scheduled);
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    // 0x80070490 (ERROR_NOT_FOUND) often indicates the notification platform
                    // isn't available for the current app identity (no AUMID / unpackaged app),
                    // so surface detailed info and fall back to persisting the schedule locally.
                    System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ScheduleAsync: CreateToastNotifier failed (HR=0x{comEx.HResult:X8}): {comEx}");
                    // Persist the request so the app knows about the scheduled reminder even
                    // if the OS refused to create a scheduled toast for this process.
                }

                await AddOrUpdateStoreAsync(new ScheduledStoreEntry
                {
                    Id = request.Id,
                    TriggerTime = request.TriggerTime,
                    Title = request.Title,
                    Body = request.Body
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ScheduleAsync: {ex}");
            }
        }

        public async Task CancelAsync(Guid id)
        {
            try
            {
                global::Windows.UI.Notifications.ToastNotifier? notifier = null;
                try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(DefaultAumid); } catch { try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(); } catch { notifier = null; } }
                if (notifier != null)
                {
                    var scheduled = notifier.GetScheduledToastNotifications();
                    var match = scheduled.FirstOrDefault(s => string.Equals(s.Id, id.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        // Try fallback by matching delivery time and content
                        var store = await ReadStoreAsync();
                        var entry = store.FirstOrDefault(x => x.Id == id);
                        if (entry != null)
                        {
                            match = scheduled.FirstOrDefault(s => StoreEntryMatchesScheduled(entry, s));
                        }
                    }

                    if (match != null)
                    {
                        notifier.RemoveFromSchedule(match);
                    }
                }


                await RemoveFromStoreAsync(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.CancelAsync: {ex}");
            }
        }

        public async Task<Services.NotificationInfo?> GetScheduledAsync(Guid reminderId)
        {
            try
            {
                global::Windows.UI.Notifications.ToastNotifier? notifier = null;
                try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(DefaultAumid); } catch { try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(); } catch { notifier = null; } }
                if (notifier != null)
                {
                    var scheduled = notifier.GetScheduledToastNotifications();
                    // Try to find by Id first
                    var match = scheduled.FirstOrDefault(s => string.Equals(s.Id, reminderId.ToString(), StringComparison.OrdinalIgnoreCase));
                    var store = await ReadStoreAsync();
                    var entry = store.FirstOrDefault(x => x.Id == reminderId);
                    if (match != null)
                    {
                        return new Services.NotificationInfo(
                            reminderId,
                            entry != null ? entry.TriggerTime : match.DeliveryTime,
                            entry?.Title ?? string.Empty,
                            entry?.Body ?? string.Empty,
                            IsScheduled: true,
                            RequestCode: null,
                            AlarmType: null
                        );
                    }

                    // Fallback: match by delivery time + content when Id not set on scheduled toast
                    var fallback = scheduled.FirstOrDefault(s => entry != null ? StoreEntryMatchesScheduled(entry, s) : false);
                    if (fallback != null && entry != null)
                    {
                        return new Services.NotificationInfo(
                            entry.Id,
                            entry.TriggerTime,
                            entry.Title ?? string.Empty,
                            entry.Body ?? string.Empty,
                            IsScheduled: true,
                            RequestCode: null,
                            AlarmType: null
                        );
                    }

                    if (entry != null)
                    {
                        return new Services.NotificationInfo(
                            entry.Id,
                            entry.TriggerTime,
                            entry.Title ?? string.Empty,
                            entry.Body ?? string.Empty,
                            IsScheduled: false,
                            RequestCode: null,
                            AlarmType: null
                        );
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.GetScheduledAsync: {ex}");
            }

            return null;
        }

                                private global::Windows.Data.Xml.Dom.XmlDocument CreateToastXml(string title, string body, string? launch = null)
                                {
                                                var launchAttr = string.IsNullOrEmpty(launch) ? string.Empty : $" launch=\"{System.Net.WebUtility.HtmlEncode(launch)}\"";
                                                var xml =
                                                    "<toast" + launchAttr + ">" +
                                                    "<visual>" +
                                                    "<binding template=\"ToastGeneric\">" +
                                                    "<text>" + System.Net.WebUtility.HtmlEncode(title) + "</text>" +
                                                    "<text>" + System.Net.WebUtility.HtmlEncode(body) + "</text>" +
                                                    "</binding>" +
                                                    "</visual>" +
                                                    "</toast>";
                                                var doc = new global::Windows.Data.Xml.Dom.XmlDocument();
                                                doc.LoadXml(xml);
                                                return doc;
                                }

                                private bool StoreEntryMatchesScheduled(ScheduledStoreEntry entry, global::Windows.UI.Notifications.ScheduledToastNotification s)
                                {
                                            try
                                            {
                                                // Compare delivery times within 2 seconds tolerance and title/body when available
                                                var delivery = s.DeliveryTime;
                                                var entryLocal = entry.TriggerTime.ToLocalTime();
                                                var diff = Math.Abs((entryLocal - delivery).TotalSeconds);
                                                if (diff <= 2)
                                                {
                                                    // Try to read title/body from the scheduled toast XML
                                                    try
                                                    {
                                                        var xml = s.Content?.GetXml() ?? string.Empty;
                                                        var hasTitle = xml.Contains(System.Net.WebUtility.HtmlEncode(entry.Title ?? string.Empty));
                                                        var hasBody = xml.Contains(System.Net.WebUtility.HtmlEncode(entry.Body ?? string.Empty));
                                                        if (hasTitle || hasBody) return true;
                                                    }
                                                    catch { /* ignore XML parse issues; fall back to time-only match */ }

                                                    return true;
                                                }
                                            }
                                            catch { }
                                            return false;
                                }

                                [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
                                private static extern int GetCurrentProcessExplicitAppUserModelID(out IntPtr appID);

                                private static bool HasExplicitAppUserModelId()
                                {
                                            try
                                            {
                                                IntPtr p;
                                                var hr = GetCurrentProcessExplicitAppUserModelID(out p);
                                                if (hr == 0 && p != IntPtr.Zero)
                                                {
                                                    try
                                                    {
                                                        var s = Marshal.PtrToStringUni(p) ?? string.Empty;
                                                        return !string.IsNullOrEmpty(s);
                                                    }
                                                    finally
                                                    {
                                                        try { Marshal.FreeCoTaskMem(p); } catch { }
                                                    }
                                                }
                                            }
                                            catch { }
                                            return false;
                                }

        private async Task ReconcileWithOsAsync()
        {
            try
            {
                var store = await ReadStoreAsync();
                try
                {
                    global::Windows.UI.Notifications.ToastNotifier? notifier = null;
                    try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(DefaultAumid); } catch { try { notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(); } catch { notifier = null; } }
                    if (notifier == null) return;
                    var scheduled = notifier.GetScheduledToastNotifications();

                    // build set of existing scheduled IDs
                    var existing = new HashSet<string>(scheduled.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);

                    var toReschedule = store.Where(e => !existing.Contains(e.Id.ToString())).ToList();
                    foreach (var entry in toReschedule)
                    {
                        // Recreate scheduled toast
                        var toastXml = CreateToastXml(entry.Title ?? "Reminder", entry.Body ?? string.Empty);
                        var when = entry.TriggerTime.ToLocalTime();
                        var scheduledToast = new global::Windows.UI.Notifications.ScheduledToastNotification(toastXml, when);
                        // Only set the Id when we have an AUMID to avoid COM exceptions
                        if (HasExplicitAppUserModelId())
                        {
                            try
                            {
                                scheduledToast.Id = entry.Id.ToString();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ReconcileWithOsAsync: Failed to set ScheduledToastNotification.Id for {entry.Id} (HR=0x{ex.HResult:X8}): {ex}");
                            }
                        }
                        notifier.AddToSchedule(scheduledToast);
                    }
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ReconcileWithOsAsync: CreateToastNotifier or scheduling failed (HR=0x{comEx.HResult:X8}): {comEx}");
                    // If OS scheduling isn't available, leave the store intact — the debug UI
                    // will show reminders stored locally but not scheduled with the OS.
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ReconcileWithOsAsync: {ex}");
            }
        }

        private async Task<List<ScheduledStoreEntry>> ReadStoreAsync()
        {
            try
            {
                if (!File.Exists(_storePath)) return new List<ScheduledStoreEntry>();
                var json = await File.ReadAllTextAsync(_storePath).ConfigureAwait(false);
                var items = JsonSerializer.Deserialize<List<ScheduledStoreEntry>>(json) ?? new List<ScheduledStoreEntry>();
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.ReadStoreAsync: {ex}");
                return new List<ScheduledStoreEntry>();
            }
        }

        private async Task AddOrUpdateStoreAsync(ScheduledStoreEntry entry)
        {
            var list = await ReadStoreAsync();
            var existing = list.FirstOrDefault(x => x.Id == entry.Id);
            if (existing != null)
            {
                existing.TriggerTime = entry.TriggerTime;
                existing.Title = entry.Title;
                existing.Body = entry.Body;
            }
            else
            {
                list.Add(entry);
            }
            await WriteStoreAsync(list);
        }

        private async Task RemoveFromStoreAsync(Guid id)
        {
            var list = await ReadStoreAsync();
            var remaining = list.Where(x => x.Id != id).ToList();
            await WriteStoreAsync(remaining);
        }

        private async Task WriteStoreAsync(List<ScheduledStoreEntry> list)
        {
            try
            {
                var dir = Path.GetDirectoryName(_storePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(list);
                await File.WriteAllTextAsync(_storePath, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationSchedulerWindows.WriteStoreAsync: {ex}");
            }
        }

        /// <summary>
        /// Returns Windows-specific diagnostic info (AUMID, shortcut path, etc.).
        /// </summary>
        public Task<IDictionary<string, string>> GetPlatformDiagnosticsAsync()
        {
            var diag = new Dictionary<string, string>();

            // Get explicit AUMID (if set on this process)
            var aumid = WindowsNotificationRegistration.GetCurrentExplicitAppUserModelId();
            diag["AUMID"] = aumid ?? "(none)";
            diag["RegisteredAUMID"] = DefaultAumid;

            // Get shortcut path and existence
            var shortcutPath = WindowsNotificationRegistration.GetExpectedStartMenuShortcutPath();
            diag["ShortcutPath"] = shortcutPath;
            diag["ShortcutExists"] = File.Exists(shortcutPath) ? "Yes" : "No";

            // Check if packaged
            diag["IsPackaged"] = Background.WindowsSchedulingHelper.IsPackagedApp() ? "Yes" : "No";

            // Check registry for COM LocalServer32
            try
            {
                var clsidPath = @"SOFTWARE\Classes\CLSID\{A1B2C3D4-E5F6-4789-ABCD-1234567890AB}\LocalServer32";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(clsidPath);
                diag["COMRegistered"] = key != null ? "Yes" : "No";
                if (key != null)
                {
                    diag["COMPath"] = key.GetValue(null) as string ?? "(empty)";
                }
            }
            catch
            {
                diag["COMRegistered"] = "Error checking";
            }

            return Task.FromResult<IDictionary<string, string>>(diag);
        }

        private class ScheduledStoreEntry
        {
            public Guid Id { get; set; }
            public DateTimeOffset TriggerTime { get; set; }
            public string? Title { get; set; }
            public string? Body { get; set; }
        }
    }
}
