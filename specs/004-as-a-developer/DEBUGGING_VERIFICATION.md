# Debugging Verification Checklist

**Feature**: User Story 5 - Integrated Debugging Experience  
**Date**: November 1, 2025  
**Status**: ✅ Complete

## Prerequisites

- [ ] Visual Studio 2025, VS Code, or Rider installed
- [ ] .NET 10 SDK installed (`dotnet --version` shows 10.0.x)
- [ ] Docker running (`docker ps` succeeds)
- [ ] Solution builds successfully (`dotnet build`)

## Test 1: Basic Debugging Setup (T065, T066)

### Visual Studio 2025
- [ ] Open `BloodThinnerTracker.sln`
- [ ] Verify `BloodThinnerTracker.AppHost` is set as startup project (bold in Solution Explorer)
- [ ] Debug dropdown shows "https" or "http" profile
- [ ] Press F5 → Application starts
- [ ] Dashboard opens at https://localhost:17225
- [ ] Status: ✅ PASS / ❌ FAIL

### Visual Studio Code
- [ ] Open repository root folder
- [ ] `.vscode/launch.json` exists with AppHost configuration
- [ ] Press F5 → Select "AppHost" configuration
- [ ] Application starts
- [ ] Dashboard accessible
- [ ] Status: ✅ PASS / ❌ FAIL

## Test 2: API Controller Breakpoint (T067)

1. **Setup**:
   - [ ] Open `src/BloodThinnerTracker.Api/Controllers/HealthController.cs`
   - [ ] Set breakpoint on line: `return Ok(new { status = "Healthy", ... });`

2. **Execute**:
   - [ ] Press F5 to start debugging
   - [ ] Wait for application to start
   - [ ] Open browser to `http://localhost:5234/health`

3. **Verify**:
   - [ ] Debugger stops at breakpoint
   - [ ] Can inspect variables in Locals window
   - [ ] Call Stack shows HTTP request chain
   - [ ] Press F5 to continue → API returns response
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 3: Blazor Component Breakpoint (T068)

1. **Setup**:
   - [ ] Open `src/BloodThinnerTracker.Web/Components/Pages/Home.razor`
   - [ ] Set breakpoint in `OnInitializedAsync()` method

2. **Execute**:
   - [ ] Press F5 to start debugging
   - [ ] Navigate to `http://localhost:5235/` in browser

3. **Verify**:
   - [ ] Debugger stops at breakpoint
   - [ ] Can inspect Blazor component state
   - [ ] Press F5 to continue → Page renders
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 4: Cross-Service Debugging (T069)

1. **Setup**:
   - [ ] Set breakpoint in Web: `Components/Pages/Home.razor` → `OnInitializedAsync()`
   - [ ] Set breakpoint in API: `Controllers/HealthController.cs` → `Health()` method

2. **Execute**:
   - [ ] Press F5 to start debugging
   - [ ] Navigate to home page (triggers API health check)

3. **Verify**:
   - [ ] Debugger stops at **first breakpoint** (Web component)
   - [ ] Press F5 → Debugger stops at **second breakpoint** (API controller)
   - [ ] Call Stack shows: `Health()` ← `HttpClient` ← `OnInitializedAsync()`
   - [ ] Press F5 → Both services complete successfully
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 5: Hot Reload - Blazor .razor Files (T070)

1. **Setup**:
   - [ ] Press F5 to start debugging
   - [ ] Navigate to `http://localhost:5235/`

2. **Execute**:
   - [ ] Open `src/BloodThinnerTracker.Web/Components/Pages/Home.razor`
   - [ ] Change text: `<h1>Welcome</h1>` → `<h1>Welcome to Blood Thinner Tracker</h1>`
   - [ ] Save file (Ctrl+S)

3. **Verify**:
   - [ ] Browser auto-refreshes within 2 seconds
   - [ ] New heading text visible
   - [ ] No application restart occurred
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 6: Hot Reload - C# Code Files (T071)

1. **Setup**:
   - [ ] Press F5 to start debugging
   - [ ] Set breakpoint in `Controllers/HealthController.cs` → `Health()` method

2. **Execute**:
   - [ ] Trigger breakpoint: `curl http://localhost:5234/health`
   - [ ] **While paused at breakpoint**, modify method:
     ```csharp
     // Change:
     return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
     // To:
     return Ok(new { status = "Healthy", message = "All systems operational", timestamp = DateTime.UtcNow });
     ```
   - [ ] Save file
   - [ ] Press F5 to continue

3. **Verify**:
   - [ ] Visual Studio shows 🔥 Hot Reload indicator
   - [ ] Next API call returns modified response with "message" field
   - [ ] No application restart occurred
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 7: Hot Reload Limitations (T072)

1. **Test: AppHost changes require restart**:
   - [ ] Start debugging (F5)
   - [ ] Modify `src/BloodThinnerTracker.AppHost/Program.cs` (e.g., change port)
   - [ ] Save file
   - [ ] Verify: Visual Studio shows "Hot Reload not supported" warning
   - [ ] Must stop (Shift+F5) and restart (F5) for changes to apply
   - [ ] Status: ✅ PASS (restart required as expected)

2. **Test: appsettings.json changes require restart**:
   - [ ] Start debugging (F5)
   - [ ] Modify `src/BloodThinnerTracker.Api/appsettings.json`
   - [ ] Save file
   - [ ] Verify: Changes don't apply until restart
   - [ ] Status: ✅ PASS (restart required as expected)

3. **Test: Method signature changes require restart**:
   - [ ] Start debugging (F5)
   - [ ] Try adding new parameter to method: `Health(string test)`
   - [ ] Save file
   - [ ] Verify: Visual Studio shows Hot Reload not supported
   - [ ] Status: ✅ PASS (restart required as expected)

## Test 8: Dashboard During Debugging (T073)

1. **Setup**:
   - [ ] Press F5 to start debugging

2. **Execute**:
   - [ ] Open Dashboard: `http://localhost:17225` in **separate browser window**
   - [ ] Set breakpoint in API controller
   - [ ] Trigger breakpoint via API call

3. **Verify**:
   - [ ] Dashboard remains accessible while debugger is paused
   - [ ] Can view logs in Dashboard while at breakpoint
   - [ ] Can view metrics while debugging
   - [ ] Dashboard doesn't freeze or become unresponsive
   - [ ] Status: ✅ PASS / ❌ FAIL

## Test 9: Exception Debugging (T074)

1. **Setup**:
   - [ ] Add test exception to API controller:
     ```csharp
     [HttpGet("test-exception")]
     public IActionResult TestException()
     {
         throw new InvalidOperationException("Test exception for debugging");
     }
     ```

2. **Execute**:
   - [ ] Press F5 to start debugging
   - [ ] Open Dashboard → Console Logs tab
   - [ ] Call endpoint: `curl http://localhost:5234/test-exception`

3. **Verify - In IDE**:
   - [ ] Visual Studio breaks on exception (if "Break on CLR Exceptions" enabled)
   - [ ] Exception details visible in Exception Helper window
   - [ ] Can inspect exception properties (Message, StackTrace, InnerException)
   - [ ] Status: ✅ PASS / ❌ FAIL

4. **Verify - In Dashboard**:
   - [ ] Navigate to Console Logs → Filter by "Error"
   - [ ] Exception appears with full stack trace
   - [ ] Can see exception message: "Test exception for debugging"
   - [ ] Trace ID visible for correlation
   - [ ] Status: ✅ PASS / ❌ FAIL

5. **Cleanup**:
   - [ ] Remove test exception endpoint
   - [ ] Commit only the documentation changes

## Test 10: Call Stack Inspection

1. **Setup**:
   - [ ] Set breakpoints in:
     - Web: `Components/Pages/Home.razor` → `OnInitializedAsync()`
     - API: `Controllers/HealthController.cs` → `Health()`
     - API: Data service (if applicable)

2. **Execute**:
   - [ ] Press F5 to start debugging
   - [ ] Navigate to home page

3. **Verify**:
   - [ ] When stopped at API breakpoint, open Call Stack window
   - [ ] Call stack shows complete flow:
     ```
     HealthController.Health() ← Current
     ControllerActionInvoker.InvokeActionMethodAsync()
     ...
     HttpClient.GetFromJsonAsync()
     Home.OnInitializedAsync() ← Origin
     ```
   - [ ] Can double-click call stack entries to navigate to code
   - [ ] Status: ✅ PASS / ❌ FAIL

## Summary

**Total Tests**: 10  
**Passed**: ___  
**Failed**: ___  

**Overall Status**: ✅ PASS / ❌ FAIL

**Notes**:
- All tests should pass for User Story 5 to be considered complete
- If any test fails, document the issue and investigate
- Hot Reload limitations are expected behavior (not failures)

**Tested By**: _______________  
**Date**: _______________  
**Environment**: 
- OS: _______________
- IDE: _______________
- .NET SDK: _______________
- Docker: _______________

**Sign-off**: 
- [ ] All tests passed
- [ ] Documentation complete
- [ ] Ready for Phase 8 (Polish & Cross-Cutting Concerns)
