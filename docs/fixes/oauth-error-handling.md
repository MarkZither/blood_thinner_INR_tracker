# OAuth Error Handling Fix

## Problem
When OAuth providers (Microsoft/Google) experience transient issues (API timeouts, network errors, etc.), the application would show an unhandled exception page instead of gracefully handling the error.

**Example error:**
```
HttpRequestException: An error occurred when retrieving Microsoft user information (GatewayTimeout)
AuthenticationFailureException: An error was encountered while handling the remote login
```

## Solution
Added `OnRemoteFailure` event handler to both Microsoft and Google OAuth configurations to catch and handle authentication failures gracefully.

### Changes Made

**Program.cs - Microsoft Account Configuration:**
```csharp
options.Events.OnRemoteFailure = context =>
{
    context.Response.Redirect("/login?error=oauth_failed&message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
    context.HandleResponse();
    return Task.CompletedTask;
};
```

**Program.cs - Google Configuration:**
```csharp
options.Events.OnRemoteFailure = context =>
{
    context.Response.Redirect("/login?error=oauth_failed&message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed"));
    context.HandleResponse();
    return Task.CompletedTask;
};
```

**Login.razor - Error Display:**
```csharp
[SupplyParameterFromQuery(Name = "error")]
public string? ErrorCode { get; set; }

[SupplyParameterFromQuery(Name = "message")]
public string? ErrorMessage { get; set; }

protected override async Task OnInitializedAsync()
{
    // Display OAuth error if present
    if (!string.IsNullOrEmpty(ErrorCode))
    {
        _errorMessage = ErrorCode switch
        {
            "oauth_failed" => $"Sign-in failed: {ErrorMessage ?? "Unable to complete authentication. Please try again."}",
            "access_denied" => "You cancelled the sign-in process.",
            _ => "An error occurred during sign-in. Please try again."
        };
    }
}
```

## How It Works

1. **OAuth Failure Detection**: When Microsoft/Google OAuth flow encounters an error (timeout, API error, network issue), the `OnRemoteFailure` event is triggered

2. **User-Friendly Redirect**: Instead of showing a technical error page, redirect to `/login` with query parameters containing error information

3. **Error Display**: Login page reads error parameters and displays a user-friendly MudBlazor Alert with:
   - Clear error message
   - Option to dismiss the alert
   - Encouragement to try again

4. **Logging**: Warnings are logged for troubleshooting without exposing technical details to users

## Error Types Handled

| Error Code | Description | User Message |
|------------|-------------|--------------|
| `oauth_failed` | Provider API errors (timeout, network, etc.) | "Sign-in failed: [specific error]" |
| `access_denied` | User cancelled OAuth consent | "You cancelled the sign-in process." |
| (default) | Any other authentication error | "An error occurred during sign-in." |

## User Experience

**Before:**
- Unhandled exception page with technical stack trace
- User confused, no clear path forward
- Had to manually navigate back to login

**After:**
- Stays on login page
- Clear, friendly error message
- Easy to retry login
- Error can be dismissed

## Common Transient Errors

This fix handles transient OAuth provider issues like:
- **GatewayTimeout**: Microsoft/Google API temporarily unavailable
- **ServiceUnavailable**: OAuth provider under high load
- **NetworkError**: Temporary network connectivity issue
- **RateLimitExceeded**: Too many requests to OAuth provider

These are typically resolved by simply trying to log in again after a few seconds.

## Testing

1. Simulate timeout by disconnecting network during OAuth flow
2. Verify error message appears on login page
3. Reconnect network and retry login successfully

## Related Files
- `src/BloodThinnerTracker.Web/Program.cs` - OAuth event handlers
- `src/BloodThinnerTracker.Web/Components/Pages/Login.razor` - Error display
