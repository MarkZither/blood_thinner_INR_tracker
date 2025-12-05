# Quickstart — Offline Sync & Notifications (local dev)

1) Start the API (in a terminal):

```pwsh
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
