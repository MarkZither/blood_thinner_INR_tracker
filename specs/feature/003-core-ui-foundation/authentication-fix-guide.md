# Authentication System Fix - Implementation Guide

**Task**: T003-001  
**Priority**: P0 (Critical Security Issue)  
**Estimated Effort**: 2 days  
**Related User Story**: US-003-05

---

## Problem Summary

The authentication system is fundamentally broken with multiple critical issues:

### 1. Service Registration Missing
**Problem**: `CustomAuthenticationStateProvider` is defined but never registered in DI container.

**Impact**: 
- `AuthorizationMessageHandler` cannot inject `CustomAuthenticationStateProvider`
- Falls back to checking if it's the right type (line 43 in AuthorizationMessageHandler.cs)
- Token retrieval always fails
- No bearer tokens in API requests

**Evidence**:
```csharp
// Program.cs - MISSING REGISTRATION
// builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>(); // NOT PRESENT
// builder.Services.AddScoped<CustomAuthenticationStateProvider>(); // NOT PRESENT
```

### 2. OAuth Callbacks Don't Exchange Tokens
**Problem**: Login page links to `/signin-microsoft` and `/signin-google` but these endpoints don't exist or don't complete OAuth flow.

**Impact**:
- User clicks "Sign in with Microsoft/Google"
- Gets redirected to OAuth provider
- OAuth provider redirects back with authorization code
- No handler exchanges code for JWT tokens
- `MarkUserAsAuthenticatedAsync` never called
- User appears logged in to OAuth provider but not to our app

**Evidence**:
```csharp
// Login.razor - Links point to endpoints
<a class="btn btn-outline-primary btn-lg d-block mb-2" href="/signin-microsoft">
<a class="btn btn-outline-danger btn-lg d-block" href="/signin-google">

// Program.cs - OAuth configured but callback not implemented
.AddMicrosoftAccount(options => {
    options.CallbackPath = "/signin-microsoft"; // Path defined
    options.SaveTokens = true; // Tokens would be saved IF flow completed
})
```

### 3. Bearer Tokens Never Added to Requests
**Problem**: `AuthorizationMessageHandler.SendAsync` tries to get token but service isn't injected properly.

**Impact**:
- All API calls missing `Authorization: Bearer {token}` header
- API returns 401 Unauthorized
- User sees toast: "Cannot access data"
- Page loads anyway with empty state

**Evidence**:
```csharp
// AuthorizationMessageHandler.cs line 43-56
if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
{
    var token = await customProvider.GetTokenAsync(); // Always null or empty
    if (!string.IsNullOrEmpty(token))
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
```

### 4. Pages Load Despite Authentication Failures
**Problem**: `[Authorize]` attribute doesn't prevent page rendering when authentication fails.

**Impact**:
- Dashboard, Medications, INR pages render even when not authenticated
- User sees "No medications yet" instead of "Please log in"
- Security risk: UI exposed without proper authentication

**Evidence**:
```csharp
// Dashboard.razor
@attribute [Authorize] // Should redirect to login, but doesn't
// Page loads, makes API call, gets 401, shows empty state
```

### 5. Logout Button Invisible
**Problem**: Logout button is in Bootstrap dropdown that requires `data-bs-toggle="dropdown"` JavaScript.

**Impact**:
- Bootstrap JS not loaded (good - per Constitution)
- Dropdown never opens
- User can't log out
- Only way to "log out" is to clear browser cache manually

**Evidence**:
```razor
<!-- MainLayout.razor line 71-91 -->
<div class="dropdown">
    <button class="btn btn-outline-light dropdown-toggle" type="button" 
            data-bs-toggle="dropdown" aria-expanded="false">
        <!-- Requires Bootstrap JS to work -->
    </button>
    <ul class="dropdown-menu dropdown-menu-end">
        <li><a class="dropdown-item text-danger" href="/logout">
            <i class="fas fa-sign-out-alt me-2"></i>Logout
        </a></li>
    </ul>
</div>
```

---

## Solution Overview

### Phase 1: Service Registration (30 minutes)
Fix DI container registration so `CustomAuthenticationStateProvider` is injectable.

### Phase 2: OAuth Callback Handlers (4 hours)
Implement endpoints that exchange OAuth authorization codes for JWT tokens.

### Phase 3: Bearer Token Injection (1 hour)
Verify `AuthorizationMessageHandler` can retrieve and add tokens to requests.

### Phase 4: Authentication Guards (2 hours)
Add route guards that redirect unauthenticated users to login page.

### Phase 5: Logout UI Fix (1 hour)
Replace Bootstrap dropdown with MudBlazor `MudMenu`.

### Phase 6: Testing & Validation (3 hours)
Comprehensive testing of complete auth flow.

---

## Detailed Implementation Steps

### Step 1: Register CustomAuthenticationStateProvider

**File**: `src/BloodThinnerTracker.Web/Program.cs`

**Location**: After line 61 (after `AddCascadingAuthenticationState()`)

**Code to Add**:
```csharp
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// ADD THESE LINES:
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>(); // For direct injection

// Add HttpClient for API calls with authentication (T018k)
builder.Services.AddTransient<AuthorizationMessageHandler>();
```

**Why Both Registrations**:
- First line: Allows framework to inject `AuthenticationStateProvider` interface
- Second line: Allows pages to inject `CustomAuthenticationStateProvider` directly (like `Logout.razor` does)

**Verification**:
```bash
# Build should succeed
dotnet build src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj
```

---

### Step 2: Create OAuth Callback Handler

**New File**: `src/BloodThinnerTracker.Web/Components/Pages/OAuthCallback.razor`

```razor
@page "/signin-microsoft"
@page "/signin-google"
@using Microsoft.AspNetCore.Authentication
@using System.Security.Claims
@inject IHttpContextAccessor HttpContextAccessor
@inject CustomAuthenticationStateProvider AuthStateProvider
@inject NavigationManager Navigation
@inject ILogger<OAuthCallback> Logger
@rendermode InteractiveServer

<PageTitle>Signing In...</PageTitle>

<div class="d-flex justify-content-center align-items-center min-vh-100">
    <div class="text-center">
        <MudProgressCircular Color="Color.Primary" Indeterminate="true" Size="Size.Large" />
        <MudText Typo="Typo.h6" Class="mt-4">Completing sign in...</MudText>
    </div>
</div>

@code {
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var httpContext = HttpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                Logger.LogError("HttpContext is null during OAuth callback");
                Navigation.NavigateTo("/login?error=context_missing", forceLoad: true);
                return;
            }

            // Get authentication result from cookie authentication
            var authenticateResult = await httpContext.AuthenticateAsync();
            
            if (!authenticateResult.Succeeded)
            {
                Logger.LogError("Authentication failed during OAuth callback: {Failure}", 
                    authenticateResult.Failure?.Message ?? "Unknown");
                Navigation.NavigateTo("/login?error=auth_failed", forceLoad: true);
                return;
            }

            // Extract tokens from authentication properties
            var tokens = authenticateResult.Properties?.GetTokens();
            var accessToken = tokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
            var refreshToken = tokens?.FirstOrDefault(t => t.Name == "refresh_token")?.Value;

            if (string.IsNullOrEmpty(accessToken))
            {
                Logger.LogError("No access token received from OAuth provider");
                Navigation.NavigateTo("/login?error=no_token", forceLoad: true);
                return;
            }

            Logger.LogInformation("Successfully received access token from OAuth provider");

            // Store tokens in browser storage via CustomAuthenticationStateProvider
            await AuthStateProvider.MarkUserAsAuthenticatedAsync(accessToken, refreshToken);

            Logger.LogInformation("Tokens stored, redirecting to dashboard");

            // Redirect to dashboard
            Navigation.NavigateTo("/dashboard", forceLoad: true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during OAuth callback processing");
            Navigation.NavigateTo("/login?error=processing_failed", forceLoad: true);
        }
    }
}
```

**Register IHttpContextAccessor**:

**File**: `src/BloodThinnerTracker.Web/Program.cs`

**Location**: After MudBlazor services (after line 32)

```csharp
// Add MudBlazor services
builder.Services.AddMudServices();

// ADD THIS LINE:
builder.Services.AddHttpContextAccessor();
```

---

### Step 3: Update Login Page Error Handling

**File**: `src/BloodThinnerTracker.Web/Components/Pages/Login.razor`

**Replace the `@code` block** with:

```csharp
@code {
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string _errorMessage = string.Empty;

    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(Error))
        {
            _errorMessage = Error switch
            {
                "context_missing" => "Authentication context error. Please try again.",
                "auth_failed" => "Authentication failed. Please check your credentials.",
                "no_token" => "No access token received. Please try again.",
                "processing_failed" => "Error processing authentication. Please try again.",
                _ => "An unknown error occurred. Please try again."
            };
        }
    }
}
```

---

### Step 4: Add Authentication State Logging

**File**: `src/BloodThinnerTracker.Web/Services/AuthorizationMessageHandler.cs`

**Update the `SendAsync` method** (lines 40-56) with enhanced logging:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
{
    if (_authStateProvider is CustomAuthenticationStateProvider customProvider)
    {
        var token = await customProvider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("✅ Added Bearer token to request: {Method} {Uri}", request.Method, request.RequestUri);
        }
        else
        {
            _logger.LogWarning("⚠️ No token available for request: {Method} {Uri}", request.Method, request.RequestUri);
        }
    }
    else
    {
        _logger.LogError("❌ AuthStateProvider is not CustomAuthenticationStateProvider (type: {Type})", 
            _authStateProvider?.GetType().Name ?? "null");
    }

    var response = await base.SendAsync(request, cancellationToken);

    // Handle 401 Unauthorized - token might be expired
    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
        _logger.LogWarning("⚠️ Received 401 Unauthorized response. Token may be expired.");
        
        // TODO: Implement automatic token refresh using refresh token
        // For now, just log the user out
        if (_authStateProvider is CustomAuthenticationStateProvider provider)
        {
            _logger.LogInformation("Logging out user due to 401 response");
            await provider.MarkUserAsLoggedOutAsync();
        }
    }

    return response;
}
```

---

### Step 5: Replace Logout Dropdown with MudBlazor

**File**: `src/BloodThinnerTracker.Web/Components/Layout/MainLayout.razor`

**Find** (around line 71-91):
```razor
<!-- User Menu -->
<div class="dropdown">
    <button class="btn btn-outline-light dropdown-toggle" type="button" 
            data-bs-toggle="dropdown" aria-expanded="false">
        <i class="fas fa-user-circle me-1"></i>
        @(userName ?? "User")
    </button>
    <ul class="dropdown-menu dropdown-menu-end">
        <li><a class="dropdown-item" href="/profile">
            <i class="fas fa-user me-2"></i>Profile
        </a></li>
        <li><a class="dropdown-item" href="/profile">
            <i class="fas fa-cog me-2"></i>Settings
        </a></li>
        <li><hr class="dropdown-divider"></li>
        <li><a class="dropdown-item" href="/help">
            <i class="fas fa-question-circle me-2"></i>Help & Support
        </a></li>
        <li><hr class="dropdown-divider"></li>
        <li><a class="dropdown-item text-danger" href="/logout">
            <i class="fas fa-sign-out-alt me-2"></i>Logout
        </a></li>
    </ul>
</div>
```

**Replace with**:
```razor
<!-- User Menu - MudBlazor -->
<MudMenu AnchorOrigin="Origin.BottomRight" TransformOrigin="Origin.TopRight">
    <ActivatorContent>
        <MudButton Variant="Variant.Outlined" 
                   Color="Color.Inherit" 
                   StartIcon="@Icons.Material.Filled.Person"
                   Class="text-white border-white">
            @(userName ?? "User")
        </MudButton>
    </ActivatorContent>
    <ChildContent>
        <MudMenuItem Icon="@Icons.Material.Filled.Person" Href="/profile">
            Profile
        </MudMenuItem>
        <MudMenuItem Icon="@Icons.Material.Filled.Settings" Href="/profile">
            Settings
        </MudMenuItem>
        <MudDivider />
        <MudMenuItem Icon="@Icons.Material.Filled.Help" Href="/help">
            Help & Support
        </MudMenuItem>
        <MudDivider />
        <MudMenuItem Icon="@Icons.Material.Filled.Logout" 
                     Href="/logout"
                     Class="mud-error-text">
            Logout
        </MudMenuItem>
    </ChildContent>
</MudMenu>
```

---

### Step 6: Add Authentication Debug Page

**New File**: `src/BloodThinnerTracker.Web/Components/Pages/AuthDebug.razor`

```razor
@page "/auth/status"
@using System.Security.Claims
@using Microsoft.AspNetCore.Components.Authorization
@inject AuthenticationStateProvider AuthStateProvider
@inject CustomAuthenticationStateProvider CustomAuthState
@inject ILogger<AuthDebug> Logger
@rendermode InteractiveServer

<PageTitle>Authentication Status</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h4" GutterBottom="true">Authentication Debug Information</MudText>
        
        <MudDivider Class="my-4" />
        
        <MudText Typo="Typo.h6" Class="mt-4">Authentication State</MudText>
        <MudList>
            <MudListItem>
                <MudText>
                    <strong>Is Authenticated:</strong> 
                    @if (authState?.User?.Identity?.IsAuthenticated == true)
                    {
                        <MudChip Color="Color.Success" Size="Size.Small">Yes</MudChip>
                    }
                    else
                    {
                        <MudChip Color="Color.Error" Size="Size.Small">No</MudChip>
                    }
                </MudText>
            </MudListItem>
            <MudListItem>
                <MudText>
                    <strong>Token Present:</strong>
                    @if (!string.IsNullOrEmpty(token))
                    {
                        <MudChip Color="Color.Success" Size="Size.Small">Yes</MudChip>
                    }
                    else
                    {
                        <MudChip Color="Color.Error" Size="Size.Small">No</MudChip>
                    }
                </MudText>
            </MudListItem>
            <MudListItem>
                <MudText>
                    <strong>Token Preview:</strong> 
                    <code>@(token?.Length > 20 ? token.Substring(0, 20) + "..." : token ?? "null")</code>
                </MudText>
            </MudListItem>
        </MudList>

        @if (authState?.User?.Identity?.IsAuthenticated == true)
        {
            <MudText Typo="Typo.h6" Class="mt-4">User Claims</MudText>
            <MudList>
                @foreach (var claim in authState.User.Claims)
                {
                    <MudListItem>
                        <MudText><strong>@claim.Type:</strong> @claim.Value</MudText>
                    </MudListItem>
                }
            </MudList>
        }

        <MudButton Variant="Variant.Filled" 
                   Color="Color.Primary" 
                   Class="mt-4"
                   OnClick="RefreshState">
            Refresh State
        </MudButton>
    </MudPaper>
</MudContainer>

@code {
    private AuthenticationState? authState;
    private string? token;

    protected override async Task OnInitializedAsync()
    {
        await LoadState();
    }

    private async Task LoadState()
    {
        authState = await AuthStateProvider.GetAuthenticationStateAsync();
        token = await CustomAuthState.GetTokenAsync();
        
        Logger.LogInformation("Auth Debug - IsAuthenticated: {IsAuth}, Token: {HasToken}", 
            authState?.User?.Identity?.IsAuthenticated,
            !string.IsNullOrEmpty(token));
    }

    private async Task RefreshState()
    {
        await LoadState();
        StateHasChanged();
    }
}
```

---

## Testing Checklist

### Unit Tests

**File**: `tests/BloodThinnerTracker.Web.Tests/AuthenticationTests.cs`

```csharp
using BloodThinnerTracker.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace BloodThinnerTracker.Web.Tests;

public class AuthenticationTests
{
    [Fact]
    public async Task CustomAuthStateProvider_WhenTokenPresent_ReturnsAuthenticatedState()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var mockLogger = new Mock<ILogger<CustomAuthenticationStateProvider>>();
        
        var fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        
        mockJsRuntime
            .Setup(js => js.InvokeAsync<string>("localStorage.getItem", new object[] { "authToken" }))
            .ReturnsAsync(fakeToken);
        
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object, mockLogger.Object);
        
        // Act
        var state = await provider.GetAuthenticationStateAsync();
        
        // Assert
        Assert.True(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task CustomAuthStateProvider_WhenNoToken_ReturnsAnonymousState()
    {
        // Arrange
        var mockJsRuntime = new Mock<IJSRuntime>();
        var mockLogger = new Mock<ILogger<CustomAuthenticationStateProvider>>();
        
        mockJsRuntime
            .Setup(js => js.InvokeAsync<string>("localStorage.getItem", new object[] { "authToken" }))
            .ReturnsAsync((string?)null);
        
        var provider = new CustomAuthenticationStateProvider(mockJsRuntime.Object, mockLogger.Object);
        
        // Act
        var state = await provider.GetAuthenticationStateAsync();
        
        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated);
    }
}
```

### Integration Test

**File**: `tests/BloodThinnerTracker.Integration.Tests/OAuthFlowTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BloodThinnerTracker.Integration.Tests;

public class OAuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OAuthFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginPage_Should_Load_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign in with Microsoft", content);
        Assert.Contains("Sign in with Google", content);
    }

    [Fact]
    public async Task ProtectedPage_Without_Auth_Should_Redirect()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/dashboard");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }
}
```

### E2E Test (Manual for now, automate with Playwright later)

1. **Login Flow**:
   - [ ] Navigate to `http://localhost:5000/login`
   - [ ] Click "Sign in with Microsoft" (or Google)
   - [ ] Complete OAuth flow on provider's site
   - [ ] Verify redirect back to `/signin-microsoft` (or `/signin-google`)
   - [ ] Verify automatic redirect to `/dashboard`
   - [ ] Check browser console for log: "✅ Added Bearer token to request"
   - [ ] Verify dashboard loads data (no 401 errors)
   - [ ] Navigate to `/auth/status` and verify:
     - "Is Authenticated: Yes"
     - "Token Present: Yes"
     - Claims are displayed

2. **API Call with Token**:
   - [ ] Open browser DevTools → Network tab
   - [ ] Navigate to `/medications`
   - [ ] Look for API call to `/api/medications`
   - [ ] Click on request → Headers
   - [ ] Verify `Authorization: Bearer eyJ...` header is present
   - [ ] Verify response status is 200 OK (not 401)

3. **Logout Flow**:
   - [ ] Click user menu button (top-right)
   - [ ] Verify dropdown menu opens (MudBlazor menu)
   - [ ] Click "Logout"
   - [ ] Verify redirect to `/logout`
   - [ ] Verify redirect to `/login`
   - [ ] Verify browser console log: "User logged out successfully"
   - [ ] Try to navigate to `/dashboard`
   - [ ] Verify redirect back to `/login` (not authenticated)
   - [ ] Navigate to `/auth/status`
   - [ ] Verify "Is Authenticated: No" and "Token Present: No"

4. **Token Expiry**:
   - [ ] Log in successfully
   - [ ] Open browser DevTools → Application → LocalStorage
   - [ ] Delete `authToken` key (simulates expired/cleared token)
   - [ ] Navigate to `/medications`
   - [ ] Verify 401 error is caught
   - [ ] Verify automatic redirect to `/login`
   - [ ] Check console for log: "Logging out user due to 401 response"

---

## Rollback Plan

If authentication fix causes critical issues:

1. **Revert commits**:
   ```bash
   git revert <commit-hash-of-auth-changes>
   git push origin feature/003-core-ui-foundation
   ```

2. **Disable authentication temporarily** (NOT RECOMMENDED for production):
   ```csharp
   // Program.cs - Comment out [Authorize] requirement
   // builder.Services.AddAuthorization();
   ```

3. **Fallback to mock authentication** (for development only):
   ```csharp
   // Add mock always-authenticated provider
   builder.Services.AddScoped<AuthenticationStateProvider, MockAuthenticationStateProvider>();
   ```

---

## Success Criteria

Authentication is considered "fixed" when:

- ✅ User can complete OAuth login flow (Microsoft or Google)
- ✅ JWT token is stored in browser localStorage
- ✅ API requests include `Authorization: Bearer {token}` header
- ✅ API returns 200 OK for authenticated requests (not 401)
- ✅ Protected pages redirect to login when not authenticated
- ✅ User can logout using visible MudBlazor menu
- ✅ Token expiry triggers automatic logout
- ✅ All authentication logs appear in console (for debugging)
- ✅ `/auth/status` page shows correct authentication state
- ✅ No medical data visible without valid authentication

---

**Document Version**: 1.0  
**Last Updated**: October 29, 2025  
**Next Review**: After T003-001 completion
