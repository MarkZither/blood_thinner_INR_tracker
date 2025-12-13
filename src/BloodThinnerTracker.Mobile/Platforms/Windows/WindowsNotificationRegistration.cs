using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BloodThinnerTracker.Mobile.Platforms.Windows
{
    internal static class WindowsNotificationRegistration
    {
        /// <summary>
        /// Register the AUMID and COM activator for unpackaged apps using
        /// Microsoft.Toolkit.Uwp.Notifications 7.x.
        /// </summary>
#pragma warning disable CS0618 // DesktopNotificationManagerCompat is obsolete but required for 7.x
        public static void TryRegisterAumidAndComServer(string aumid)
        {
            try
            {
                // Step 1: Register AUMID and COM server (creates registry entries + shortcut)
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Calling RegisterAumidAndComServer<AppNotificationActivator>('{aumid}')...");
                DesktopNotificationManagerCompat.RegisterAumidAndComServer<AppNotificationActivator>(aumid);
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: RegisterAumidAndComServer completed.");

                // Step 2: Register the activator (required before CreateToastNotifier works)
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Calling RegisterActivator<AppNotificationActivator>()...");
                DesktopNotificationManagerCompat.RegisterActivator<AppNotificationActivator>();
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: RegisterActivator completed.");

                // Verify we can now create a notifier
                try
                {
                    var notifier = DesktopNotificationManagerCompat.CreateToastNotifier();
                    System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: CreateToastNotifier succeeded.");
                }
                catch (Exception notifierEx)
                {
                    System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: CreateToastNotifier failed: {notifierEx.Message}");
                }

                // Check for shortcut
                var shortcutPath = GetExpectedStartMenuShortcutPath();
                var shortcutExists = !string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath);
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Shortcut at '{shortcutPath}' exists: {shortcutExists}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Exception during registration: {ex}");
            }
        }
#pragma warning restore CS0618

        /// <summary>
        /// Send a test notification to verify notification system is working.
        /// </summary>
#pragma warning disable CS0618 // DesktopNotificationManagerCompat is obsolete but required for 7.x
        public static void SendTestNotification()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("WindowsNotificationRegistration: Sending test notification...");
                var notifier = DesktopNotificationManagerCompat.CreateToastNotifier();
                
                // Build a simple toast using the toolkit's builder
                var content = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "Blood Thinner Tracker"
                                },
                                new AdaptiveText()
                                {
                                    Text = "Test notification - system working!"
                                }
                            }
                        }
                    }
                };
                
                var xml = content.GetXml();
                // Use reflection to create ToastNotification since Windows runtime types aren't directly available
                var tn = System.Type.GetType("Windows.UI.Notifications.ToastNotification");
                if (tn != null)
                {
                    var toast = System.Activator.CreateInstance(tn, new object[] { xml });
                    var method = notifier.GetType().GetMethod("Show");
                    method?.Invoke(notifier, new[] { toast });
                    System.Diagnostics.Debug.WriteLine("WindowsNotificationRegistration: Test notification sent successfully.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("WindowsNotificationRegistration: Could not find ToastNotification type.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Failed to send test notification: {ex}");
            }
        }
#pragma warning restore CS0618

        // Ensure an AppUserModelID is registered for unpackaged debugging by creating
        // a Start Menu shortcut with the requested AUMID set. This gives the process
        // an identity that the toast APIs will accept in many unpackaged scenarios.
        public static void EnsureStartMenuShortcutWithAumid(string aumid)
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: cannot find current exe path to create shortcut: {exePath}");
                    return;
                }

                var appName = Path.GetFileNameWithoutExtension(exePath);
                var programs = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                var shortcutDir = Path.Combine(programs, "Programs");
                if (!Directory.Exists(shortcutDir)) Directory.CreateDirectory(shortcutDir);

                var shortcutPath = Path.Combine(shortcutDir, appName + ".lnk");

                // If a shortcut already exists and contains the same AppUserModelID, we're done.
                if (File.Exists(shortcutPath))
                {
                    try
                    {
                        var shellType = Type.GetTypeFromCLSID(CLSID_ShellLink);
                        if (shellType != null)
                        {
                            var ole = Activator.CreateInstance(shellType);
                            if (ole != null && ole is IShellLinkW link)
                            {
                                var persist = (IPersistFile)link;
                                try
                                {
                                    persist.Load(shortcutPath, 0);
                                    var propStore = (IPropertyStore)link;
                                    PROPVARIANT pv;
                                    PROPERTYKEY key = PKEY_AppUserModel_ID;
                                    propStore.GetValue(ref key, out pv);
                                    try
                                    {
                                        var existing = pv.GetString();
                                        if (string.Equals(existing, aumid, StringComparison.OrdinalIgnoreCase))
                                        {
                                            System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: shortcut already has AUMID '{aumid}'");
                                            return;
                                        }
                                    }
                                    finally { PropVariantClear(ref pv); }
                                }
                                finally
                                {
                                    try { Marshal.ReleaseComObject(persist); } catch { }
                                    try { Marshal.ReleaseComObject(link); } catch { }
                                    try { Marshal.ReleaseComObject(ole); } catch { }
                                }
                            }
                        }
                    }
                    catch { /* ignore and recreate below */ }
                }

                // Create new shortcut and set AppUserModelID
                var shellTypeNew = Type.GetTypeFromCLSID(CLSID_ShellLink);
                if (shellTypeNew != null)
                {
                    var oleNew = Activator.CreateInstance(shellTypeNew);
                    if (oleNew != null && oleNew is IShellLinkW linkNew)
                    {
                        IPersistFile? persistNew = null;
                        try
                        {
                            linkNew.SetPath(exePath);
                            linkNew.SetArguments("");
                            linkNew.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? string.Empty);
                            linkNew.SetIconLocation(exePath, 0);

                            persistNew = (IPersistFile)linkNew;
                            persistNew.Save(shortcutPath, false);

                            var propStore = (IPropertyStore)linkNew;
                            PROPVARIANT pv;
                            var hr = InitPropVariantFromString(aumid, out pv);
                            if (hr == 0)
                            {
                                try
                                {
                                    PROPERTYKEY key = PKEY_AppUserModel_ID;
                                    propStore.SetValue(ref key, ref pv);
                                    propStore.Commit();
                                }
                                finally
                                {
                                    PropVariantClear(ref pv);
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Created Start Menu shortcut at '{shortcutPath}' with AUMID '{aumid}'");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Failed to create shortcut: {ex}");
                        }
                        finally
                        {
                            try { if (persistNew != null) Marshal.ReleaseComObject(persistNew); } catch { }
                            try { Marshal.ReleaseComObject(linkNew); } catch { }
                            try { Marshal.ReleaseComObject(oleNew); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowsNotificationRegistration: Failed to create Start Menu shortcut/AUMID: {ex}");
            }
        }

        // --- COM interop types and helpers for creating shell shortcuts and setting AppUserModelID ---
        private static readonly Guid CLSID_ShellLink = new Guid("00021401-0000-0000-C000-000000000046");

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY { public Guid fmtid; public uint pid; }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPVARIANT
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr p;

            public string GetString()
            {
                if (vt != 31) return string.Empty; // VT_LPWSTR == 31
                return Marshal.PtrToStringUni(p) ?? string.Empty;
            }
        }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        }

        [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PROPERTYKEY pkey);
            void GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
            void Commit();
        }

        private static readonly PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };

        [DllImport("propsys.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int InitPropVariantFromString([MarshalAs(UnmanagedType.LPWStr)] string psz, out PROPVARIANT propvar);

        // Expose current explicit AppUserModelID for diagnostics
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int GetCurrentProcessExplicitAppUserModelID(out IntPtr appID);

        /// <summary>
        /// Returns the explicit AppUserModelID for the current process, or null if none.
        /// This is a best-effort diagnostic helper intended to be called reflectively from shared UI code.
        /// </summary>
        public static string? GetCurrentExplicitAppUserModelId()
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
                        return string.IsNullOrEmpty(s) ? null : s;
                    }
                    finally
                    {
                        try { Marshal.FreeCoTaskMem(p); } catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Returns the expected Start Menu Programs shortcut path for the current process executable.
        /// </summary>
        public static string GetExpectedStartMenuShortcutPath()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
                var appName = Path.GetFileNameWithoutExtension(exePath);
                var programs = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                var shortcutDir = Path.Combine(programs, "Programs");
                var shortcutPath = Path.Combine(shortcutDir, appName + ".lnk");
                return shortcutPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        [DllImport("ole32.dll", PreserveSig = true, EntryPoint = "PropVariantClear")]
        private static extern int PropVariantClearNative(ref PROPVARIANT pvar);

        private static void PropVariantClear(ref PROPVARIANT pv)
        {
            try { PropVariantClearNative(ref pv); } catch { }
        }
    }
}
