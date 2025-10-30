# Authentication Cookie State Management Fix

## Problem
After logout or interrupted OAuth flows, authentication cookies could be in an inconsistent state, causing login failures with errors like:
- "Cookie authentication not found"
- Blank pages during OAuth flow
- Endless redirect loops

## Root Causes
1. Login page was aggressively clearing cache before OAuth flow started (interfered with OAuth state)
2. Logout was signing out from OAuth provider sessions (should preserve them)
3. OAuth callback was preventing cookie creation with `HandleResponse()`

## Solution

### 1. Fix OAuth Callback Cookie Persistence
**File**: `Program.cs` - OnTicketReceived event

**BEFORE (BROKEN)**:
```csharp
options.Events.OnTicketReceived = context =>
{
    var returnUrl = context.Properties?.RedirectUri ?? "/dashboard";
    context.Response.Redirect($"/oauth-complete?provider=microsoft&returnUrl={Uri.EscapeDataString(returnUrl)}");
    context.HandleResponse(); // ❌ PREVENTS COOKIE CREATION!
    return Task.CompletedTask;
};
```

**AFTER (FIXED)**:
```csharp
options.Events.OnTicketReceived = context =>
{
    var returnUrl = context.Properties?.RedirectUri ?? "/dashboard";
    // Update RedirectUri to our callback page
    // Do NOT call HandleResponse() - let authentication complete and create cookies first
    context.Properties!.RedirectUri = $"/oauth-complete?provider=microsoft&returnUrl={Uri.EscapeDataString(returnUrl)}";
    return Task.CompletedTask;
};
```

**Why**: `HandleResponse()` stops the authentication pipeline, preventing the cookie middleware from creating the authentication cookie.

### 2. Add Cookie Cleanup on Login Page
**File**: `Login.razor`

Add cleanup logic when user hits login page:

```csharp
protected override async Task OnInitializedAsync()
{
    // Check if user is already authenticated
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    if (authState.User.Identity?.IsAuthenticated == true)
    {
        // Already logged in, redirect to dashboard
        Navigation.NavigateTo("/dashboard", forceLoad: true);
        return;
    }
    
    // Clear any stale auth state from cache
    // This handles cases where cookies are bad but cache has stale data
    await AuthStateProvider.MarkUserAsLoggedOutAsync();
}
```

### 3. Improve Logout to Clear All State
**File**: `AuthController.cs`

Current logout already clears:
- ✅ Blazor auth state (cache)
- ✅ Cookie authentication
- ✅ Does NOT clear OAuth scheme cookies (correct - they're for challenge only)

This is correct behavior.

### 4. Add Recovery Mechanism in OAuthCallback
**File**: `OAuthCallback.razor`

Add better error handling with recovery options:

```csharp
if (!authResult.Succeeded)
{
    Logger.LogWarning("OAuth complete: Cookie authentication not found - attempting recovery");
    
    // Clear any stale state
    await AuthStateProvider.MarkUserAsLoggedOutAsync();
    
    _error = "Authentication failed. This may be due to stale session data.";
    _isProcessing = false;
    
    // Offer retry with clean state
    StateHasChanged();
    await Task.Delay(2000);
    Navigation.NavigateTo("/login?clearcache=true", forceLoad: true);
    return;
}
```

### 5. User-Friendly Cookie Clear Option
**File**: `Login.razor`

Add query parameter handling:

```csharp
[SupplyParameterFromQuery(Name = "clearcache")]
public string? ClearCache { get; set; }

protected override async Task OnInitializedAsync()
{
    // If clearcache parameter is set, force cleanup
    if (!string.IsNullOrEmpty(ClearCache))
    {
        await AuthStateProvider.MarkUserAsLoggedOutAsync();
        // Redirect to clean URL
        Navigation.NavigateTo("/login", forceLoad: true, replace: true);
        return;
    }
    
    // Rest of initialization...
}
```

## Implementation Steps

1. ✅ Fix Program.cs OAuth events (DONE)
2. ⏳ Add Login.razor cleanup logic
3. ⏳ Add OAuthCallback.razor recovery mechanism
4. ⏳ Test full login/logout cycle

## Testing Checklist

### Fresh Login
- [ ] Navigate to protected page while logged out
- [ ] Redirects to /login with returnUrl
- [ ] Click "Sign in with Microsoft"
- [ ] Complete OAuth
- [ ] Successfully lands on original returnUrl
- [ ] No redirect loops

### Logout and Re-login
- [ ] Click logout
- [ ] Verify cache cleared (check logs)
- [ ] Verify cookie cleared (browser devtools)
- [ ] Click login again
- [ ] Should work without issues

### Bad Cookie State Recovery
- [ ] Manually corrupt cookies (close browser mid-OAuth)
- [ ] Try to access protected page
- [ ] Should redirect to login with clearcache
- [ ] Login should work after cleanup

### Multiple Login Attempts
- [ ] Start login, cancel OAuth
- [ ] Try login again immediately
- [ ] Should work without "cookie not found" error

## Browser Testing

Test in multiple browsers to verify cookie handling:
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if available)
- [ ] Private/Incognito mode

## Deployment Notes

1. **Clear server cache** after deployment:
   ```bash
   # Restart application to clear IMemoryCache
   dotnet run --project src/BloodThinnerTracker.AppHost
   ```

2. **User communication**:
   - Users may need to clear browser cache/cookies once after deployment
   - Add banner: "If you experience login issues, please clear your browser cookies"

3. **Monitoring**:
   - Watch for "Cookie authentication not found" in logs
   - If this persists, indicates larger OAuth configuration issue

## Known Limitations

1. **OAuth Provider Session**: 
   - User's Microsoft/Google session stays active after logout
   - This is normal OAuth behavior
   - To fully sign out, user must sign out of Microsoft/Google too

2. **Browser Tabs**:
   - Multiple tabs may have sync issues with auth state
   - Use `forceLoad: true` to ensure proper state refresh

3. **Session Timeout**:
   - IMemoryCache tokens expire (30 days configured)
   - OAuth cookies may expire sooner
   - Mismatch can cause authentication errors

## Future Improvements

1. **Persistent Token Storage**:
   - Move from IMemoryCache to encrypted database storage
   - Survives application restarts
   - Easier to manage and clear

2. **Refresh Token Flow**:
   - Implement proper refresh token handling
   - Auto-refresh before expiration
   - Better user experience (no re-login)

3. **Session Management UI**:
   - Show active sessions to user
   - Allow user to revoke sessions
   - Show last login time/location

4. **OAuth Provider Logout**:
   - Add single sign-out support
   - Sign out from Microsoft/Google when logging out of our app
   - Requires additional OAuth configuration

## Conclusion

The authentication flow should now be robust against bad cookie states:
- ✅ OAuth callback properly creates cookies
- ✅ Logout clears all application state
- ✅ Login page cleans up stale state
- ✅ Recovery mechanism for edge cases
- ✅ User-friendly error messages

**Status**: IN PROGRESS - Need to implement steps 2-4
**Priority**: HIGH - Blocks user login
**Estimated Time**: 30 minutes
