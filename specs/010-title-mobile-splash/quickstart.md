# Quickstart — Run the MAUI prototype (mock data)

This quickstart explains how to run a local MAUI prototype using the mock data service. It assumes you have .NET 10 SDK installed and the MAUI workloads configured.

- Install .NET 10 SDK and MAUI workloads (see project global.json for required SDK).
- Install Android SDK / Visual Studio workload for Android, or use Windows for desktop target.
- Install .NET 10 SDK and MAUI workloads (see project global.json for required SDK).
- Install Android SDK / Visual Studio workload for Android, or use Windows for desktop target.

2) Restore and build
```pwsh
# from repository root
dotnet restore
dotnet build src\BloodThinnerTracker.Mobile\BloodThinnerTracker.Mobile.csproj -f net10.0
```

3) Run with mock data (Windows)
```pwsh
# Run the MAUI app in Debug using mock service
setx USE_MOCK_INR 1
dotnet run --project src\BloodThinnerTracker.Mobile\BloodThinnerTracker.Mobile.csproj -f net10.0-windows10.0.19041.0
```

4) Run on Android emulator
```pwsh
setx USE_MOCK_INR 1
dotnet build src\BloodThinnerTracker.Mobile\BloodThinnerTracker.Mobile.csproj -f net10.0-android
dotnet run --project src\BloodThinnerTracker.Mobile\BloodThinnerTracker.Mobile.csproj -f net10.0-android
```

5) Developer toggle
- In Debug builds the app exposes a developer menu to switch between `MockInrService` and `ApiInrService`. Use environment variable `USE_MOCK_INR=1` to default to mocks.

6) Notes
- OAuth flows require redirect URIs to be configured. For local testing, use the mock OAuth provider or test accounts with the app's redirect configuration.
- The AES key used to encrypt the cache is created on first-run and stored in platform `SecureStorage`.
