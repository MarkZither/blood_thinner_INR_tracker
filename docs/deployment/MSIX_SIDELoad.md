Local MSIX Sideload & Debugging (Windows)

This document describes how to build and sideload a packaged (MSIX) version of the app locally so you can register and debug WinRT background tasks (the real OS-hosted background task execution).

Why: WinRT background tasks require a packaged app context. When you run the app unpackaged (for example `dotnet run` or using the portable MAUI runner from VS Code) background task registration will be skipped or will throw. Packaging the app (MSIX) and installing it locally is the recommended way to test background tasks end-to-end.

Prerequisites
- Windows 10/11 with developer mode enabled (Settings → Update & Security → For developers → Developer mode).
- Visual Studio (recommended) or .NET SDK with MAUI workloads installed.
- Appropriate signing certificate for MSIX (for local dev you can use a self-signed certificate created by Visual Studio).

High-level steps
1. Build an MSIX package for the app (recommended via Visual Studio Packaging project or use dotnet publish with `WindowsPackageType`).
2. Install/sideload the MSIX locally using `Add-AppxPackage` (PowerShell).
3. Launch the app and verify background task registration runs.
4. Attach the debugger to the background task host (see notes below).

Commands (examples)

Using Visual Studio (recommended)
- Add a Windows Application Packaging Project to the solution (or use the MAUI packaging options in the project properties).
- Set the Packaging project to reference `BloodThinnerTracker.Mobile` and choose Debug configuration.
- Press F5 on the Packaging project — VS will build, sign (using a local dev cert), install the package and run the app. You can set breakpoints in the background task code and VS will hit them when the background task runs.

Using dotnet CLI (example)
> Note: Packaging with the CLI can be more involved, Visual Studio provides the easiest path. If you prefer CLI you can publish with the MSIX target supported by the workload.

PowerShell script (install an already-built MSIX)
Use the helper script in `tools/scripts/install-msix.ps1` (see repo). Example usage:

```powershell
# From repo root
.\tools\scripts\install-msix.ps1 -PackagePath ".\artifacts\MyApp_1.0.0.0_x64_Debug.msix"
```

How to attach the debugger to a WinRT background task
- Option A (Visual Studio): Start debugging the Packaging project (F5). Visual Studio deploys and attaches automatically to the app and background tasks.
- Option B (Attach manually): Install the package, launch the app once, then in Visual Studio use "Debug → Attach to Process...".
  - Look for `BackgroundTaskHost.exe` or `RuntimeBroker.exe` hosting the task. Filtering by process name and watching for the moment the task runs helps (the host process may start only when the task runs).
  - Set breakpoints in `SyncBackgroundTask` (or your background task implementation) before triggering the task.

Dev alternative (fast, when not packaged)
- If you prefer not to package while iterating, use the in-process dev-worker approach: implement an `IHostedService` or a debug-only timer that executes the same sync logic inside the main app process. Keep the WinRT registration guarded with `IsPackagedApp()` so it is only used in packaged runs. This approach is fast and easy to debug from VSCode but does not exercise the OS background-host environment.

Troubleshooting
- If `Add-AppxPackage` fails with certificate/signing errors, configure the packaging project to use a developer certificate or trust the local test certificate.
- If your background task does not trigger, confirm the background task is declared in `Package.appxmanifest` and that the `EntryPoint` and `TaskType` match. Also, confirm the registration code runs (packaged app startup path) and that the trigger is set.

Security note
- Do not commit private signing certificates to source control. Use Visual Studio's automatic developer certs or document cert creation in a secure way.

References
- Microsoft Docs: Packaging and distributing a Windows app
- Microsoft Docs: Debug background tasks
