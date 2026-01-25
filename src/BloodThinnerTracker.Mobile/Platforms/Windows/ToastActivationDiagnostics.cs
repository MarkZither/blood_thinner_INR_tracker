using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Platforms.Windows
{
    internal static class ToastActivationDiagnostics
    {
        private const string ToastArg = "-ToastActivated";
        private const string ActivatorClsid = "A1B2C3D4-E5F6-4789-ABCD-1234567890AB";
        private const string DefaultAumid = "MarkZither.BloodThinnerTracker.Mobile";

        public static void RunIfToastActivationPresent(string[]? args)
        {
            try
            {
                args ??= Environment.GetCommandLineArgs();
                if (args.Any(a => string.Equals(a, ToastArg, StringComparison.OrdinalIgnoreCase)))
                {
                    // Run diagnostics asynchronously (do a little work synchronously so early failures are captured)
                    Task.Run(() => RunDiagnosticsAsync(args)).ConfigureAwait(false);

                    // Sleep briefly to allow the hosting process to remain alive while diagnostics are collected.
                    // This also gives DCOM a short window to complete activation while we capture logs.
                    try { Thread.Sleep(TimeSpan.FromSeconds(15)); } catch { }
                }
            }
            catch { /* best-effort - swallow to avoid breaking normal startup */ }
        }

        private static async Task RunDiagnosticsAsync(string[] args)
        {
            try
            {
                var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BloodThinnerTracker", "diagnostics");
                Directory.CreateDirectory(baseDir);
                var file = Path.Combine(baseDir, $"toast-activation-{DateTime.UtcNow:yyyyMMddHHmmss}.log");

                await File.AppendAllTextAsync(file, $"=== Toast activation diagnostics {DateTime.UtcNow:O} ===\n").ConfigureAwait(false);
                await File.AppendAllTextAsync(file, $"Args: {string.Join(' ', args)}\n").ConfigureAwait(false);
                var procPath = Environment.ProcessPath ?? "(unknown)";
                await File.AppendAllTextAsync(file, $"ProcessPath: {procPath}\n").ConfigureAwait(false);
                await File.AppendAllTextAsync(file, $"IsPackaged: {Background.WindowsSchedulingHelper.IsPackagedApp()}\n").ConfigureAwait(false);

                // Check registry for LocalServer32
                try
                {
                    var clsidPath = $@"SOFTWARE\Classes\CLSID\{{{ActivatorClsid}}}\LocalServer32";
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(clsidPath);
                    var exists = key != null;
                    await File.AppendAllTextAsync(file, $"COM LocalServer32 present (HKCU): {exists}\n").ConfigureAwait(false);
                    if (exists)
                    {
                        var v = key?.GetValue(null) as string ?? "(empty)";
                        await File.AppendAllTextAsync(file, $"COM LocalServer32 value: {v}\n").ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    await File.AppendAllTextAsync(file, $"Registry check failed: {ex}\n").ConfigureAwait(false);
                }

                // If not registered, attempt to create registration via toolkit helper (best-effort workaround)
                try
                {
                    var clsidPath = $@"SOFTWARE\Classes\CLSID\{{{ActivatorClsid}}}\LocalServer32";
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(clsidPath);
                    if (key == null)
                    {
                        await File.AppendAllTextAsync(file, "COM LocalServer32 missing; invoking TryRegisterAumidAndComServer workaround...\n").ConfigureAwait(false);
                        WindowsNotificationRegistration.TryRegisterAumidAndComServer(DefaultAumid);
                        WindowsNotificationRegistration.EnsureStartMenuShortcutWithAumid(DefaultAumid);

                        // Re-check
                        using var key2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(clsidPath);
                        var presentAfter = key2 != null;
                        var valAfter = key2?.GetValue(null) as string ?? "(empty)";
                        await File.AppendAllTextAsync(file, $"After attempt, COM LocalServer32 present: {presentAfter}\n").ConfigureAwait(false);
                        if (presentAfter)
                        {
                            await File.AppendAllTextAsync(file, $"After attempt COM LocalServer32 value: {valAfter}\n").ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await File.AppendAllTextAsync(file, $"TryRegisterAumidAndComServer failed: {ex}\n").ConfigureAwait(false);
                }

                await File.AppendAllTextAsync(file, "Diagnostics complete.\n\n").ConfigureAwait(false);
            }
            catch { /* swallow - best-effort */ }
        }
    }
}
