# Quickstart — Local development (Docker & Azure archived)

Version: 1.0.1

Note: Docker Compose, Azure deployment templates and the CI/CD workflow were created during an earlier iteration but are OUT-OF-SCOPE for this feature. They have been archived under `specs/feature/002-docker-deployment-infrastructure/archive/infra/` for reference.

This quickstart shows how to run the services locally using .NET (no Docker). It's suitable for most developer workflows while we finish feature 002.

Prerequisites
- .NET 10 SDK (use the preview SDK version specified in global.json)
- A supported IDE (VS Code, Visual Studio) or a terminal

Verify environment
```powershell
dotnet --version
git --version
```

Run the API and Web locally (recommended)

1. Start the API (uses local SQLite by default in Development):

```powershell
cd src\BloodThinnerTracker.Api
dotnet run
```

2. In a separate terminal start the Web app:

```powershell
cd src\BloodThinnerTracker.Web
dotnet run
```

Notes
- By default the API will use the local SQLite files (see `appsettings.Development.json`).
- Migrations are applied automatically on startup via `EnsureDatabaseAsync()` in `Program.cs`.
- Health endpoint: `http://localhost:<api-port>/health` (port is printed by dotnet run; default configured ports may vary).

When to use the archived Docker/Cloud artifacts
- Use the archived files only for reference or as a starting point when we re-open Docker/Azure work in a future feature. They are preserved at:
	- `specs/feature/002-docker-deployment-infrastructure/archive/infra/`

Troubleshooting
- If migrations fail, check the console logs for EF Core errors.
- To reset the local SQLite DB (development only): stop the app and delete `bloodtracker_dev.db` in the repository root (or the file indicated in `appsettings.Development.json`).

If you'd like, I can add a short troubleshooting snippet that automates migration clearing for local dev.
