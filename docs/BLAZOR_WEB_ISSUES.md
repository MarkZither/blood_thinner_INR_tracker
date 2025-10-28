# Blazor Web App Issues - T018 Analysis

**Date**: October 21, 2025  
**Task**: T018 [US1] Implement user account creation UI

## 🚨 Critical Issues Found

### 1. Route Mismatches (Navigation Broken)

The navigation links in `MainLayout.razor` don't match the actual page routes:

| Nav Link | Actual Route | Status |
|----------|--------------|--------|
| `/` (Dashboard) | Home exists at `/`, Dashboard at `/dashboard` | ⚠️ Confusing |
| `/inr-tracking` | `/inr` | ❌ **BROKEN** |
| `/medication-log` | `/medications` | ❌ **BROKEN** |
| `/reports/inr-trends` | Not implemented | ❌ **BROKEN** |
| `/reports/medication-adherence` | Not implemented | ❌ **BROKEN** |
| `/reports/export` | Not implemented | ❌ **BROKEN** |
| `/profile` | `/profile` | ✅ Works |
| `/help` | Not implemented | ❌ **BROKEN** |
| `/logout` | Not implemented | ❌ **BROKEN** |

### 2. Hardcoded Mock Data

- **User**: "John Doe" is hardcoded in MainLayout
- **Authentication**: No actual authentication logic
- **Notifications**: Badge shows "3" hardcoded
- **Data**: All pages use mock data, not connected to API

### 3. Missing Pages (Referenced but Don't Exist)

These are linked in the nav but files don't exist:
- `/help`
- `/logout` 
- `/reports/inr-trends`
- `/reports/medication-adherence`
- `/reports/export`

### 4. No API Integration

Pages exist but have **zero API connectivity**:
- Dashboard.razor - mock data
- INRTracking.razor - mock data
- Medications.razor - mock data
- Profile.razor - mock data

All the pages call methods like:
```csharp
private async Task LoadMedicationsAsync()
{
    // Simulate API call
    await Task.Delay(500);
    
    // Mock data
    medications = new List<Medication>
    {
        new Medication { /* hardcoded values */ }
    };
}
```

### 5. Authentication State Not Implemented

- No `AuthenticationStateProvider`
- No login/logout functionality (despite having Login.razor page)
- No token storage
- No protected routes
- Username dropdown does nothing

### 6. Missing Components

These are referenced but not implemented:
- Notification offcanvas (defined in MainLayout but not functional)
- User profile dropdown actions
- Settings page
- Help system

## 📋 Missing Tasks (Should Be Added)

Based on this analysis, T018 should be broken down:

```markdown
- [ ] T018 [P] [US1] Implement user account creation UI in src/BloodThinnerTracker.Mobile/Pages/ and src/BloodThinnerTracker.Web/Pages/
  - [ ] T018a Fix navigation route mismatches in MainLayout.razor
  - [ ] T018b Create missing report pages (/reports/inr-trends, /reports/medication-adherence, /reports/export)
  - [ ] T018c Implement AuthenticationStateProvider and authentication logic
  - [ ] T018d Connect Dashboard to actual API endpoints (GET /api/users/profile, GET /api/medications, GET /api/inr)
  - [ ] T018e Connect INRTracking page to API (GET /api/inr, POST /api/inr)
  - [ ] T018f Connect Medications page to API (GET /api/medications, POST /api/medications)
  - [ ] T018g Implement logout functionality
  - [ ] T018h Create Help/Support page
  - [ ] T018i Add [Authorize] attributes to protected pages
  - [ ] T018j Implement user profile dropdown functionality
```

## 🔧 Immediate Fixes Needed

### Fix 1: Correct Navigation Routes

In `MainLayout.razor`, line ~22-32:
```razor
<!-- WRONG -->
<a class="nav-link" href="/inr-tracking">

<!-- SHOULD BE -->
<a class="nav-link" href="/inr">
```

### Fix 2: Add Missing Route Aliases

In `INRTracking.razor`:
```razor
@page "/inr"
@page "/inr-tracking"  <!-- Add alias -->
```

### Fix 3: Create API Service

Missing `ApiService.cs` for HTTP calls:
```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    
    public async Task<User> GetCurrentUserAsync()
    {
        return await _httpClient.GetFromJsonAsync<User>("/api/users/profile");
    }
    
    // etc.
}
```

### Fix 4: Add Authentication State Provider

Missing `CustomAuthenticationStateProvider.cs`:
```csharp
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    // JWT token management
    // User claims management
    // Login/Logout logic
}
```

## 📊 Completion Status

### What EXISTS:
- ✅ Page components (Dashboard, INR, Medications, Profile, Login, Register)
- ✅ UI/UX design and styling
- ✅ Medical disclaimers
- ✅ Form layouts
- ✅ Mock data structures

### What's MISSING:
- ❌ API connectivity (0% implemented)
- ❌ Authentication logic (0% implemented)
- ❌ Working navigation (50% broken links)
- ❌ Real user data (100% mock)
- ❌ Protected routes (0% implemented)
- ❌ State management (0% implemented)

## 🎯 Actual Completion: ~30%

T018 should be marked as **30% complete** - the UI shells exist but nothing functional behind them.

## 📝 Recommended Action

1. **Uncheck T018** in tasks.md
2. **Add subtasks T018a-j** to break down the remaining work
3. **Create T018k**: "Add HttpClient configuration and base API service"
4. **Create T018l**: "Implement authentication state management with JWT"
5. **Create T018m**: "Add protected route middleware"

## Related Task Issues

These tasks are also affected:
- **T015**: AuthController exists but Web app doesn't use it
- **T017**: JWT tokens issued but Web doesn't store/use them
- **T021-T023**: API endpoints exist but Web doesn't call them
- **T014**: Medical disclaimer shows but not tested across all scenarios

## Priority

🔴 **HIGH** - Users can't actually use the application without these fixes.
