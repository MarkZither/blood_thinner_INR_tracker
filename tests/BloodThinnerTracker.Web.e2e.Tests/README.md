# BloodThinnerTracker Web E2E Tests

Quick start (local)

1. Install Playwright CLI and browsers for .NET (one-time):

```powershell
# from repo root
dotnet tool restore
dotnet add tests\BloodThinnerTracker.Web.e2e.Tests package Microsoft.Playwright
# Install browsers once
playwright install
```

2. Start the web app locally (example):

```powershell
# from repo root
dotnet run --project src\BloodThinnerTracker.Web --urls "http://localhost:5000"
```

3. Configure the test base URL (optional, defaults to http://localhost:5000):

```powershell
$env:TEST_BASE_URL = "http://localhost:5000"
```

4. Run the e2e tests (skipped tests are placeholders; remove Skip attribute to run them once configured):

```powershell
dotnet test tests\BloodThinnerTracker.Web.e2e.Tests --filter Category=ReturnUrl
```

Notes
- Tests use the `/_test/logs` test-hook to assert blocked ReturnUrl events deterministically; ensure the dev server is running and exposes that controller.
- Tests are skipped by default to avoid CI failures if Playwright browsers are not installed. Remove `[Fact(Skip = ...)]` on a per-test basis to run them locally.
