# Quickstart — Offline Sync & Notifications (local dev)

1) Start the API (in a terminal):

```pwsh

---

## Exact-alarm guidance (SCHEDULE_EXACT_ALARM)

### Overview
- Android protects device battery by restricting exact alarms. For high-priority medication reminders we optionally use the `SCHEDULE_EXACT_ALARM` capability so notifications fire at precise times. This is a privileged capability that must be declared in the `AndroidManifest.xml` and — on some devices / OS versions — enabled by the user in system settings.

### Manifest
- Add the permission in `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />
```

### Runtime checks and request flow
- The app should always check `AlarmManager.CanScheduleExactAlarms()` (or the platform-equivalent API) before attempting to schedule exact alarms.
- If scheduling is not allowed, the recommended UX is to open the system settings screen where the user can grant the capability:

```csharp
if (!alarmManager.CanScheduleExactAlarms())
{
		var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
		intent.SetFlags(ActivityFlags.NewTask);
		Application.Context.StartActivity(intent);
}
```

### Graceful fallback
- If the user does not grant exact-alarm capability, fall back to the WorkManager-based scheduling path (or a best-effort AlarmManager `setAndAllowWhileIdle`), and persist the desired trigger metadata in the JSON store so the app can attempt to re-register an exact alarm later.

### Do Not Disturb (DnD) and notification importance
- DnD can suppress notification alerts even if an alarm triggers. To improve reliability:
	- Use a Notification Channel with appropriate importance (e.g., `Importance.High` or `Importance.Max`) for medication reminders.
	- Advise users to allow the app to override DnD for critical reminders if their device/organization policy permits. This usually requires the user to grant the app a system-level exemption in settings.

### Manufacturer battery optimizations and vendor caveats
- Many OEMs (Xiaomi, Oppo, Vivo, Huawei, Samsung, OnePlus) implement aggressive battery saving that can prevent background work and alarms from firing. Steps to mitigate:
	- Document device-specific steps in this quickstart for the manufacturers you support — commonly: disable battery optimization for the app, allow autostart, and whitelist the app in the vendor's power management settings.
	- During onboarding or the debug flow, surface guidance and a link to the appropriate settings page.

### Debugging scheduled work and alarms (ADB)
- Useful `adb` commands for debugging on a connected device/emulator:

```powershell
# List scheduled alarms (system-wide) and inspect for your package name
adb shell dumpsys alarm | grep your.package.name -C 3

# List jobs scheduled by the JobScheduler (useful for WorkManager-backed work)
adb shell dumpsys jobscheduler --user 0 | grep your.package.name -C 5

# Inspect WorkManager internal state (device must support dumpsys for work manager)
adb shell dumpsys activity service | grep WorkManager -C 3

# Show battery optimization exemptions
adb shell dumpsys deviceidle whitelist

# (Optional) Trigger a JobScheduler job manually for testing (replace JOB_ID)
adb shell cmd jobscheduler run -f your.package.name JOB_ID
```

Replace `your.package.name` with your actual application package value (see `AndroidManifest.xml` `package` attribute).

### Troubleshooting
- If notifications are not delivered at the expected time:
	- Confirm `POST_NOTIFICATIONS` permission was granted (Android 13+). Use app settings or adb `adb shell pm grant` during development.
	- Check `dumpsys alarm` and `dumpsys jobscheduler` output for entries tied to your package.
	- Verify device battery optimization and vendor-level autostart/whitelisting settings.
	- If using exact alarms, confirm `AlarmManager.CanScheduleExactAlarms()` returns `true` and that the user has not revoked the capability.

## JSON persistence (temporary approach)

- During Phase 2 we persist scheduled reminders in a JSON file instead of the DB. Location and format:
	- Path (development): `src/BloodThinnerTracker.Mobile/Data/scheduled_reminders.json` (source template). At runtime the file is stored under the app's platform storage (MAUI `FileSystem.AppDataDirectory`).
	- Contents: list of scheduled reminders including GUID `ReminderId`, `TriggerTimeUtc`, `RepeatPattern` (if any), `SchedulerBackend` (`WorkManager` | `AlarmManager`), and platform-specific scheduling tokens (WorkRequestId, PendingIntent extras).

### Why JSON?
- Simplicity: avoids DB schema migrations during early-phase testing and keeps scheduling logic isolated.
- Portability: same file can be read on app start to re-enqueue OS-level schedules.

### Security and migration notes
- This JSON store is temporary. For production, planned migration steps:
	1. Add a proper `ScheduledReminder` entity to the mobile DB and a small migration.
	2. On app update, read `scheduled_reminders.json` and migrate entries into the DB inside a transaction, encrypting sensitive fields if necessary.
	3. After successful migration, delete the JSON file and mark the migration as complete in app settings.

- **Encryption**: The JSON file contains scheduling metadata and should be protected. Use platform-protected storage (Android: files dir + `MODE_PRIVATE`, consider the Android Keystore for keys) or encrypt the file (AES-256) with a key stored in the platform secure store. Add unit tests that assert the file is not readable by other apps in CI/device tests where possible.

## Quick verification checklist
- [ ] App has `POST_NOTIFICATIONS` and runtime permission is granted (Android 13+).
- [ ] App declares `SCHEDULE_EXACT_ALARM` if exact alarms are required and `AlarmManager.CanScheduleExactAlarms()` returns `true` or user granted the capability.
- [ ] Notification channel for reminders is created with high importance.
- [ ] JSON scheduled file exists in `FileSystem.AppDataDirectory` and contains expected entries after scheduling.
- [ ] On device reboot the `BootReceiver` reads the JSON file and re-enqueues scheduled reminders.

***

dotnet run --project src/BloodThinnerTracker.Api --urls http://localhost:5237
```

2) Start the Web (optional for UI):

```pwsh
dotnet run --project src/BloodThinnerTracker.Web --urls http://localhost:5236
```

3) Run the mobile app in Windows (fast dev iteration):

```pwsh
dotnet build -c Debug -f net10.0-windows10.0.19041.0 src/BloodThinnerTracker.Mobile/BloodThinnerTracker.Mobile.csproj
dotnet run --project src/BloodThinnerTracker.Mobile --framework net10.0-windows10.0.19041.0
```

4) Configuration (appsettings or environment):
- `SyncOptions:DefaultIntervalMinutes` = 60  (default)
- `SyncOptions:FastPathIntervalMinutes` = 15 (when charging + Wi‑Fi)
- `Features:ApiRootUrl` = `http://localhost:5237` (used by mobile runtime)

5) Database initialization and migrations (mobile):
- The mobile app registers the shared EF Core `ApplicationDbContext` (from `BloodThinnerTracker.Data.SQLite`) and will apply migrations synchronously during startup. This is performed in `MauiProgram.CreateMauiApp()` where `db.Database.Migrate()` is invoked inside a service scope. On cold start the app ensures the `bloodtracker_mobile.db` file is created under the platform AppData directory and migrations are applied before views load.

Verification test (cold-start migrations):
- A lightweight verification test is included in `tests/BloodThinnerTracker.Mobile.Tests/StartupTests.cs`. It builds the MAUI app via `MauiProgram.CreateMauiApp()` and asserts that the `ApplicationDbContext` service is resolvable. Running the test will exercise the startup migration path in the test runner environment.

5) Validate sync flow:
- Open the app while API running; verify logs indicate a foreground refresh and that medications and INR tests are fetched.  
- Background jobs: simulate by running the platform background worker (or use emulator scheduler) and verify notifications are scheduled and fired.
