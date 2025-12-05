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
- `Sync:DefaultIntervalMinutes` = 60  (default)  
- `Sync:FastPathIntervalMinutes` = 15 (when charging + Wi‑Fi)  
- `Api:BaseUrl` = `http://localhost:5237`

5) Validate sync flow:
- Open the app while API running; verify logs indicate a foreground refresh and that medications and INR tests are fetched.  
- Background jobs: simulate by running the platform background worker (or use emulator scheduler) and verify notifications are scheduled and fired.
