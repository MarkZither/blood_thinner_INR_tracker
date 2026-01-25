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

## Windows: AUMID, activator, and packaging notes

- Windows toast notifications require an Application User Model ID (AUMID) for full activation and action handling. Packaged apps (MSIX) get an AUMID automatically; unpackaged desktop apps must register an AUMID and optionally register a COM activator to handle activation callbacks.

- During development you can use a helper like `DesktopNotificationManagerCompat` (CommunityToolkit) to register an AUMID and COM server. In this repository we provide a best-effort reflection-based attempt to call `DesktopNotificationManagerCompat.RegisterAumidAndComServer` if the library is present. For production reliability prefer packaging the app or adding the COM activator registration explicitly.

- Packaging guidance (brief):
	- Build and test your app as a packaged MSIX to get a stable AUMID and toast activation support.
	- In Visual Studio, create an MSIX packaging project or use `msix` targets in your CI to produce an MSIX bundle. Ensure the manifest declares the ToastActivations support and includes the AUMID.

- Unpackaged apps: to enable toast activation you must register a compatible AUMID and a COM server at install time. See Microsoft docs: https://learn.microsoft.com/windows/apps/design/shell/tiles-and-notifications/enable-desktop-toasts

- Debugging scheduled toasts on Windows:
	- Use the debug page in the app (`Debug -> Scheduled Reminders`) to view both the JSON-scheduled reminder entries and the OS-scheduled toasts.
	- If toasts do not appear, verify the app has a valid AUMID (packaged apps) or that `DesktopNotificationManagerCompat` (or equivalent) registered the AUMID for unpackaged cases.


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

6) Verify Fetch & Upsert (T014) — sync smoke tests

- Run the integration smoke test that exercises the full fetch-and-upsert path (applies migrations to a temporary SQLite DB, calls the API client, and verifies DB persistence):

```pwsh
dotnet test tests/Mobile.IntegrationTests/Mobile.IntegrationTests.csproj --filter FetchAndUpsertAsync_PopulatesDatabase -c Debug
```

Alternatively, there is a full E2E test that starts the AppHost (API + dependencies), seeds test data, and verifies the mobile `CacheSyncService.FetchAndUpsertAsync()` end-to-end:

```pwsh
dotnet test tests/Mobile.IntegrationTests/Mobile.IntegrationTests.csproj --filter FetchAndUpsertAsync_FetchesFromRunningApiAndPersistsToLocalDb -c Debug
```

- Expected behavior:
	- INR tests returned by the API are persisted into `INRTests` table (or cached under `inr_tests` if DB persist fails).
	- Medications returned by the API are upserted, but **invalid medications** (e.g., missing dosage) are skipped and **cached** under the `medications` cache key instead of being persisted.
	- Medication logs are persisted only when their related medication exists locally; if the related medication is missing, logs are not persisted and may be cached under `medication_logs` if persistence fails.
	- The sync worker prevents overlapping runs (concurrent calls to `SyncOnceAsync()` are skipped).

If the test passes, the fetch-and-upsert behavior is functioning as expected and will be exercised by existing CI integration steps (no new CI job required).

5) Validate sync flow:
- Open the app while API running; verify logs indicate a foreground refresh and that medications and INR tests are fetched.  
- Background jobs: simulate by running the platform background worker (or use emulator scheduler) and verify notifications are scheduled and fired.
 
Notes: The sync worker now includes simple retry/backoff logic for transient API failures (3 attempts, exponential backoff). Idempotency is ensured by repository upsert semantics; running the sync multiple times will not create duplicate medications or INR tests.

---

## Manual Verification Checklist: Reliable Local Reminders (US2)

This checklist provides step-by-step instructions for manually verifying that medication reminders are delivered reliably on Android and Windows devices.

### Prerequisites

- [ ] API is running at `http://localhost:5237` (or configured URL)
- [ ] Mobile app is built and installed on target device
- [ ] At least one active medication exists with `RemindersEnabled = true`
- [ ] Device notifications are enabled for the app

### Android Verification Steps

**Test Case 1: Basic Reminder Delivery (App Backgrounded)**

1. [ ] Open the app and navigate to Settings
2. [ ] Verify "Notification Status" shows "Enabled ✓"
3. [ ] Navigate to the Medications list and note an active medication's schedule
4. [ ] Tap "Sync Now" to trigger reminder computation
5. [ ] Navigate to "Debug Reminders" to verify scheduled reminders are listed
6. [ ] Background the app (press home button)
7. [ ] Wait for the scheduled reminder time
8. [ ] **Expected**: Notification appears in system tray with medication name and dosage

**Test Case 2: Reminder Delivery (Device Locked)**

1. [ ] Follow steps 1-5 above to schedule a reminder for 2 minutes from now
2. [ ] Lock the device screen
3. [ ] Wait for the scheduled time
4. [ ] **Expected**: Notification appears on lock screen (if device allows)

**Test Case 3: Notification Permission Denied**

1. [ ] Open device Settings > Apps > Blood Thinner Tracker > Notifications
2. [ ] Disable all notifications
3. [ ] Open the app and navigate to Settings
4. [ ] **Expected**: "Notification Status" shows "Disabled ✗" with guidance text
5. [ ] Tap "Enable Notifications" or "Open Notification Settings"
6. [ ] Re-enable notifications
7. [ ] Return to app and verify status updates to "Enabled ✓"

**Test Case 4: Exact Alarm Permission (Android 12+)**

1. [ ] Open the app and navigate to Settings
2. [ ] Check if "Exact Alarm Permission" section is visible
3. [ ] If "Not Granted", tap "Grant Exact Alarm Permission"
4. [ ] Grant the permission in system settings
5. [ ] Return to app and verify status updates to "Enabled ✓"
6. [ ] Schedule an exact alarm via Debug page
7. [ ] **Expected**: Alarm fires at precise scheduled time

**Test Case 5: Device Reboot Persistence**

1. [ ] Schedule a reminder for 10 minutes from now via Debug page
2. [ ] Verify reminder is listed in scheduled reminders
3. [ ] Reboot the device
4. [ ] Open the app after reboot
5. [ ] Navigate to Debug Reminders
6. [ ] **Expected**: Reminder should still be scheduled (re-enqueued by BootReceiver)
7. [ ] Wait for scheduled time
8. [ ] **Expected**: Notification is delivered

### Windows Verification Steps

**Test Case 1: Basic Toast Notification**

1. [ ] Open the app and navigate to Settings
2. [ ] Tap "Sync Now" to trigger reminder computation
3. [ ] Navigate to "Debug Reminders" to verify scheduled reminders
4. [ ] Check "AUMID" diagnostic info is displayed
5. [ ] Wait for scheduled reminder time
6. [ ] **Expected**: Toast notification appears in Action Center

**Test Case 2: Unpackaged App Limitations**

1. [ ] If running unpackaged (not MSIX):
   - [ ] Check Debug diagnostics show "IsPackaged: No"
   - [ ] Scheduled toasts may not appear due to missing AUMID
   - [ ] JSON store should still contain scheduled entries
2. [ ] If running packaged (MSIX):
   - [ ] Check Debug diagnostics show "IsPackaged: Yes"
   - [ ] **Expected**: Toast notifications work correctly

**Test Case 3: App Restart Reconciliation**

1. [ ] Schedule a reminder via Debug page
2. [ ] Close the app completely
3. [ ] Reopen the app
4. [ ] Navigate to Debug Reminders
5. [ ] **Expected**: Reminder appears in both JSON store and OS scheduled list

### Sync-Triggered Reminder Scheduling

**Test Case: Reminders Computed After Sync**

1. [ ] Ensure API has medications with `RemindersEnabled = true` and `ScheduledTimes` set
2. [ ] Open app and navigate to Settings
3. [ ] Note current scheduled reminders count in Debug page
4. [ ] Tap "Sync Now"
5. [ ] Return to Debug Reminders
6. [ ] **Expected**: New reminders computed based on medication schedules

### Expected Behaviors

| Scenario | Android | Windows |
|----------|---------|---------|
| App in foreground | Notification shown | Toast shown |
| App backgrounded | Notification shown | Toast shown |
| Device locked | Lock screen notification | Toast in Action Center |
| App killed | Notification shown (WorkManager/AlarmManager) | Toast may not show (unpackaged) |
| Device rebooted | Reminders re-scheduled on boot | Reminders reconciled on app start |
| Permissions denied | No notification; guidance shown in Settings | Toast may fail silently |

### Troubleshooting

**Android: Notifications not appearing**

1. Check `dumpsys notification` for blocked packages:
   ```shell
   adb shell dumpsys notification | grep bloodthinner
   ```
2. Check battery optimization settings:
   ```shell
   adb shell dumpsys deviceidle whitelist
   ```
3. Verify notification channel exists:
   ```shell
   adb shell dumpsys notification | grep -A 5 "bloodthinner_reminders"
   ```

**Windows: Toasts not appearing**

1. Check if app has toast permissions in Windows Settings > System > Notifications
2. Verify AUMID registration via Debug diagnostics in the app
3. For unpackaged apps, ensure COM registration is complete or package the app as MSIX

### Pass/Fail Criteria

- **PASS**: All test cases marked with expected results achieved
- **PARTIAL**: Core notification delivery works but edge cases (reboot, permission denial) have issues
- **FAIL**: Basic notification delivery (Test Case 1) does not work on target platform
