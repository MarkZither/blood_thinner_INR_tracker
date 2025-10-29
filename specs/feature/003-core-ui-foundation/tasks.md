# Feature 003: Core UI Foundation - Tasks

**Status**: In Progress (Validation & Polish Phase)  
**Branch**: `feature/003-core-ui-foundation`  
**Started**: October 29, 2025  
**Target Completion**: November 12, 2025 (2 weeks)

---

## Current State Assessment

### ✅ Already Implemented (Exceeds Spec)
The following components exist and exceed the original spec requirements:

**Pages**:
- ✅ `Dashboard.razor` - Full dashboard with stats cards, charts, upcoming reminders
- ✅ `Medications.razor` - Full CRUD medication management (search, filter, sort, add, edit, delete)
- ✅ `INRTracking.razor` - Full CRUD INR tracking (stats, trends, add, edit, delete)
- ✅ `Login.razor` - OAuth authentication flow
- ✅ `Register.razor` - User registration
- ✅ `Profile.razor` - User profile management
- ✅ `Logout.razor` - Logout handling
- ✅ `Reports/` - Report pages (beyond spec)
- ✅ `Help.razor`, `AccessDenied.razor`, `NotFound.razor`, `Error.razor` - Support pages

**Layout**:
- ✅ `MainLayout.razor` - Bootstrap-based navigation (NOT MudBlazor-based)
- ⚠️ `NavMenu.razor` - Exists but appears to be default template (not integrated)

**Authentication**:
- ✅ OAuth 2.0 integration (Azure AD + Google)
- ✅ JWT token handling
- ✅ Protected routes with `[Authorize]` attribute
- ✅ Custom authentication state provider

**UI Framework**:
- ✅ MudBlazor installed and partially integrated
- ⚠️ Bootstrap currently primary UI framework (mixed approach)
- ⚠️ Font Awesome icons used throughout

### ❌ Missing Components (Per Spec)

**Shared/Reusable Components**:
- ❌ `Components/Shared/LoadingSpinner.razor` - Reusable loading component
- ❌ `Components/Shared/ErrorMessage.razor` - Reusable error display
- ❌ `Components/Shared/EmptyState.razor` - Reusable "no data" state

**Services**:
- ❌ `MedicationService.cs` - Currently using direct HttpClient in pages
- ❌ `INRService.cs` - Currently using direct HttpClient in pages

**Tests**:
- ❌ Unit tests for page components
- ❌ Unit tests for services
- ❌ Integration tests for API calls
- ❌ E2E tests with Playwright
- ❌ Accessibility tests

**Documentation**:
- ❌ User guide for medication tracking
- ❌ User guide for INR tracking
- ❌ Developer documentation for component patterns

### 🔧 Architectural Issues Identified

**Layout Inconsistency**:
- MainLayout uses Bootstrap navbar, not MudBlazor MudAppBar/MudDrawer
- NavMenu.razor appears unused (default template still in place)
- Mixed UI frameworks (Bootstrap + MudBlazor) creates maintenance burden

**Code Organization**:
- HttpClient calls embedded directly in page components
- No service layer abstraction for API calls
- Business logic mixed with UI logic in code-behind

**Responsive Design**:
- Currently uses Bootstrap responsive classes
- Not optimized for MudBlazor breakpoints
- Mobile navigation could be improved

---

## Phase 1: Authentication & Architecture (6 days)

### T003-001: Fix Critical Authentication Issues [P0 - SECURITY CRITICAL]
**Owner**: TBD  
**Estimate**: 2 days  
**Status**: TODO  
**Dependencies**: None  
**Related**: US-003-05

**Problem Statement**: The authentication system is fundamentally broken:
1. `CustomAuthenticationStateProvider` is defined but never registered in DI
2. No bearer tokens are added to API requests (results in 401 errors)
3. OAuth callback handlers don't exchange tokens or call `MarkUserAsAuthenticatedAsync`
4. Pages load despite authentication failures (security risk)
5. Logout button invisible (Bootstrap dropdown requires JavaScript)

**Tasks**:
- [X] **Register CustomAuthenticationStateProvider in Program.cs**
  - [X] Add `builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();`
  - [X] Add `builder.Services.AddScoped<CustomAuthenticationStateProvider>();` (for direct injection)
  - [ ] Verify injection works in AuthorizationMessageHandler

- [X] **Implement OAuth Callback Handlers**
  - [X] **Create Razor page approach** (recommended for Blazor Server):
    - [X] Create `Components/Pages/OAuthCallback.razor` with `@page "/signin-microsoft"` and `@page "/signin-google"`
    - [X] Use `@inject IHttpContextAccessor` to access HttpContext
    - [X] Use `@rendermode InteractiveServer` for OAuth processing
  - [ ] **Alternative MVC controller approach** (NOT recommended - adds complexity):
    - [ ] Would require mixing MVC and Blazor in same app
    - [ ] Complicates routing and state management
    - [ ] **DO NOT USE unless Razor page fails**
  - [X] In `OnInitializedAsync()` method:
    - [X] Get HttpContext: `var httpContext = HttpContextAccessor.HttpContext;`
    - [X] Authenticate: `var result = await httpContext.AuthenticateAsync();`
    - [X] Extract tokens: `var tokens = result.Properties?.GetTokens();`
    - [X] Get access_token and refresh_token from tokens
  - [X] Call `CustomAuthenticationStateProvider.MarkUserAsAuthenticatedAsync(token, refreshToken)`
  - [X] Redirect to `/dashboard` on success using `Navigation.NavigateTo()`
  - [X] Redirect to `/login?error=...` on failure with descriptive error code
  - [X] Add comprehensive error logging for each failure point:
    - [X] HttpContext is null
    - [X] Authentication failed
    - [X] No access token received
    - [X] Token storage failed
    - [X] General exception caught
  - [X] Register `IHttpContextAccessor` in Program.cs: `builder.Services.AddHttpContextAccessor();`

  **Why Razor Page Approach**:
  - Keeps everything in Blazor ecosystem (no MVC mixing)
  - Interactive Server render mode allows async OAuth processing
  - Direct access to Navigation, IHttpContextAccessor, and services
  - Simpler dependency injection (no need for separate controller DI)
  - Consistent with rest of Blazor Server application architecture

  **See detailed implementation** in `authentication-fix-guide.md` lines 150-230 for complete code example.


- [X] **Fix Bearer Token Injection**
  - [X] Verify `AuthorizationMessageHandler.SendAsync` can retrieve token
  - [X] Add logging: "Token retrieved: {HasToken}" before adding header
  - [ ] Test API call includes `Authorization: Bearer {token}` header
  - [X] Verify 401 responses trigger logout (already implemented in handler)

- [X] **Add Authentication Route Guards**
  - [X] **Approach**: Use `<AuthorizeView>` + NavigationManager (not custom base component)
  - [X] Wrap page content in `<AuthorizeView>` with `<Authorizing>` and `<NotAuthorized>` templates
  - [X] In `<NotAuthorized>`: Use NavigationManager to redirect to `/login?returnUrl={currentUrl}`
  - [X] In `<Authorizing>`: Show MudProgressCircular loading spinner
  - [X] Apply to Dashboard.razor, Medications.razor, INRTracking.razor, Profile.razor
  - [X] Fix context naming conflict in Profile.razor (EditForm vs Authorized)
  - [ ] Test redirect flow: unauthenticated → login → return to original page

- [X] **Fix Logout UI Visibility**
  - [X] Replace Bootstrap dropdown in MainLayout with MudBlazor MudMenu
  - [X] Ensure logout link is visible and clickable
  - [X] Use MudBlazor Icons (Material Design) instead of FontAwesome
  - [ ] Test logout flow: click → `/logout` → token cleared → redirect to `/login`

- [X] **Add Authentication State Debugging**
  - [X] Create `/auth/status` debug page (AuthStatus.razor) with:
    - [X] IsAuthenticated status display
    - [X] Token present/absent indicator
    - [X] Token expiry time and status (Valid/Expiring Soon/Expired)
    - [X] User claims table with all claim types and values
    - [X] Quick action buttons (Refresh, Dashboard, Login/Logout)
    - [X] Loading state with MudProgressCircular
    - [X] Responsive MudBlazor layout (MudContainer, MudCard, MudPaper, MudGrid)
  - [ ] Log authentication state on every protected page load
  - [ ] Log token presence/absence before API calls
  - [ ] Add `[Authorize]` attribute validation logging

**Acceptance Criteria**:
- ✅ CustomAuthenticationStateProvider is registered and injectable
- ✅ OAuth login flow completes and stores JWT token
- ✅ API requests include `Authorization: Bearer {token}` header
- ✅ API returns 200 OK instead of 401 Unauthorized for authenticated requests
- ✅ Logout button is visible in MudBlazor menu (not Bootstrap dropdown)
- ✅ Clicking logout clears tokens and redirects to login
- ✅ Protected pages redirect to login when not authenticated
- ✅ Token expiry detection works (auto-logout on expired token)
- ✅ No medical data visible without valid authentication

**Testing**:
- [ ] Unit test: CustomAuthenticationStateProvider registration
- [ ] Integration test: OAuth callback handler token exchange
- [ ] E2E test: Complete login flow (click login → OAuth → callback → dashboard)
- [ ] E2E test: API call with bearer token (verify 200 response)
- [ ] E2E test: Complete logout flow (click logout → token cleared → login page)
- [ ] E2E test: Expired token detection and auto-logout

**Rollback Plan**:

If authentication fix causes regressions or production issues:

1. **Immediate Rollback** (revert code changes):
   - Revert DI registration changes in `Program.cs` (remove CustomAuthenticationStateProvider registration)
   - Disable OAuth callback routes (comment out `@page` directives in OAuthCallback.razor)
   - Restore previous MainLayout.razor with Bootstrap dropdown
   - Keep existing AuthorizationMessageHandler (broken, but maintains status quo)
   
2. **Verification After Rollback**:
   - Application builds and runs without errors
   - Users can access login page
   - Existing broken auth behavior restored (known issue, documented)
   
3. **No Data Migration Required**:
   - No database schema changes in this feature
   - No data loss possible from rollback
   
4. **Communication**:
   - Document rollback in git commit message: "Revert: T003-001 authentication fix due to [specific issue]"
   - Create GitHub issue with regression details
   - Plan fix iteration with learnings from regression
   
5. **Rollback Decision Criteria**:
   - Application fails to start after deployment
   - OAuth login completely broken (worse than current state)
   - API requests fail with 500 errors (not just 401s)
   - Browser console errors prevent page rendering
   
6. **Forward Fix vs Rollback**:
   - Prefer forward fix for minor issues (error messages, UI glitches)
   - Choose rollback only for critical failures that block all users

**Concurrent Token Refresh Handling**:
- Implement lock/flag in CustomAuthenticationStateProvider to prevent concurrent refresh attempts
- Use `SemaphoreSlim` or boolean flag to ensure only one refresh operation at a time
- If refresh in progress, queue other API calls until refresh completes
- Pseudocode:
  ```csharp
  private SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);
  
  private async Task<string> RefreshTokenIfNeededAsync()
  {
      await _refreshSemaphore.WaitAsync();
      try
      {
          // Check if token still expired (may have been refreshed by another call)
          if (!IsTokenExpired()) return _currentToken;
          
          // Perform refresh
          var newToken = await CallRefreshEndpointAsync();
          return newToken;
      }
      finally
      {
          _refreshSemaphore.Release();
      }
  }
  ```

---

### T003-002: Remove Bootstrap & Font Awesome Dependencies [P0]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

Remove all non-MudBlazor UI framework dependencies per Constitution Principle III.

**Tasks**:
- [ ] Audit `BloodThinnerTracker.Web.csproj` for UI framework packages
- [ ] Remove Bootstrap NuGet packages (if any)
- [ ] Remove Font Awesome NuGet packages (if any)
- [ ] Delete `wwwroot/css/bootstrap*.css` files
- [ ] Delete `wwwroot/js/bootstrap*.js` files
- [ ] Delete `wwwroot/css/fontawesome*.css` files
- [ ] Remove Bootstrap CDN links from `_Host.cshtml` or `App.razor`
- [ ] Remove Font Awesome CDN links
- [ ] Verify project builds successfully without Bootstrap/Font Awesome
- [ ] Run app and verify no console errors about missing CSS/JS files

**Acceptance Criteria**:
- ✅ No Bootstrap packages in `.csproj`
- ✅ No Font Awesome packages in `.csproj`
- ✅ No Bootstrap files in `wwwroot/`
- ✅ No Font Awesome files in `wwwroot/`
- ✅ No CDN references to Bootstrap or Font Awesome
- ✅ Application builds without errors
- ✅ No console errors in browser

**Note**: This task removes dependencies but does NOT update component usage. Component migration happens in T003-004 through T003-008.

---

### T003-003: Create Service Layer for API Calls [P1]
- [ ] Remove `bootstrap.bundle.min.js` script references
- [ ] Update `_Imports.razor` to remove Bootstrap usings
- [ ] Verify MudBlazor is sole UI dependency
- [ ] Document custom CSS that remains (medical-specific only)

**Acceptance Criteria**:
- No Bootstrap packages in `.csproj`
- No Font Awesome packages in `.csproj`
- No Bootstrap/Font Awesome files in `wwwroot/`
- No Bootstrap/Font Awesome CDN references
- Project builds successfully without these dependencies
- Only MudBlazor and custom medical CSS remain

---

### T003-002: Create Service Layer [P0]
**Owner**: TBD  
**Estimate**: 2 days  
**Status**: TODO

Create service abstractions to separate API calls from UI components.

**Tasks**:
- [ ] Create `Services/IMedicationService.cs` interface
- [ ] Create `Services/MedicationService.cs` implementation
  - [ ] `GetMedicationsAsync()`
  - [ ] `GetMedicationByIdAsync(Guid id)`
  - [ ] `CreateMedicationAsync(CreateMedicationRequest request)`
  - [ ] `UpdateMedicationAsync(Guid id, UpdateMedicationRequest request)`
  - [ ] `DeleteMedicationAsync(Guid id)`
- [ ] Create `Services/IINRService.cs` interface
- [ ] Create `Services/INRService.cs` implementation
  - [ ] `GetINRTestsAsync()`
  - [ ] `GetINRTestByIdAsync(Guid id)`
  - [ ] `CreateINRTestAsync(CreateINRTestRequest request)`
  - [ ] `UpdateINRTestAsync(Guid id, UpdateINRTestRequest request)`
  - [ ] `DeleteINRTestAsync(Guid id)`
- [ ] Register services in `Program.cs`:
  ```csharp
  builder.Services.AddScoped<IMedicationService, MedicationService>();
  builder.Services.AddScoped<IINRService, INRService>();
  ```
  - Lifetime: Scoped (per request/circuit in Blazor Server)
  - Order: After HttpClient registration, before AddRazorComponents
- [ ] Update pages to use services instead of direct HttpClient

**Service Layer Error Handling Requirements**:
- All service methods return `Result<T>` or `Result` wrapper (success/failure pattern)
- Catch and wrap all exceptions - no exceptions thrown to UI layer
- Exception handling:
  - `HttpRequestException` → `Result.Failure("Network error. Please check your connection.")`
  - `JsonException` → `Result.Failure("Invalid response from server. Please try again.")`
  - `TaskCanceledException` → `Result.Failure("Request timed out. Please try again.")`
  - `UnauthorizedAccessException` → Handled by AuthorizationMessageHandler (triggers logout)
- Log all errors at Error level with request context (method, endpoint, user)
- Include correlation ID in logs for request tracing

**API Resilience (Retry/Timeout) Requirements**:
- **Timeout**: 30 seconds for all API calls (configurable via appsettings.json)
- **Retry Policy**: 3 attempts with exponential backoff (1s, 2s, 4s)
- **Retry Conditions**:
  - Network errors (HttpRequestException)
  - Server errors (5xx status codes)
  - Timeouts (TaskCanceledException)
- **No Retry Conditions**:
  - Client errors (4xx except 401)
  - 401 Unauthorized (handled by AuthorizationMessageHandler - triggers token refresh or logout)
  - 403 Forbidden (user lacks permission)
- **Implementation**: Use Polly library for retry policies
  ```csharp
  services.AddHttpClient<IMedicationService, MedicationService>()
      .AddPolicyHandler(GetRetryPolicy())
      .AddPolicyHandler(GetTimeoutPolicy());
  ```

**Result<T> Pattern Example**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
}
```

**Acceptance Criteria**:
- All API calls go through service layer
- Services use dependency injection
- Services handle errors and return typed results
- Pages have no direct HttpClient dependencies
- All errors caught and wrapped (no uncaught exceptions)
- Retry policy configured for transient failures
- Timeout policy prevents hanging requests

---

### T003-003: Migrate Layout to MudBlazor [P0]
**Owner**: TBD  
**Estimate**: 2 days  
**Status**: TODO

Replace Bootstrap navigation with MudBlazor components per Constitution Principle III.

**Tasks**:
- [ ] Update `MainLayout.razor` structure:
  - [ ] Add `<MudThemeProvider />` with custom theme
  - [ ] Add `<MudPopoverProvider />`
  - [ ] Add `<MudDialogProvider />`
  - [ ] Add `<MudSnackbarProvider />`
  - [ ] Replace `<nav class="navbar">` with `<MudLayout>`
  - [ ] Add `<MudAppBar>` for top navigation
  - [ ] Add `<MudDrawer>` for side navigation menu
  - [ ] Configure drawer to be responsive (collapse on mobile)
  - [ ] Add user info/avatar in AppBar using `<MudAvatar>`
  - [ ] Add logout button with `<MudIconButton>`
- [ ] Create new `NavMenu.razor` with MudBlazor:
  - [ ] Use `<MudNavMenu>` as container
  - [ ] Use `<MudNavLink>` for each menu item
  - [ ] Replace all Font Awesome icons with `<MudIcon Icon="@Icons.Material.Filled.*">`
  - [ ] Dashboard → `Icons.Material.Filled.Dashboard`
  - [ ] Medications → `Icons.Material.Filled.Medication`
  - [ ] INR Tracking → `Icons.Material.Filled.ShowChart`
  - [ ] Profile → `Icons.Material.Filled.Person`
  - [ ] Logout → `Icons.Material.Filled.Logout`
  - [ ] Add active page highlighting with `Match="NavLinkMatch.All"`
- [ ] Remove all Bootstrap navbar CSS classes
- [ ] Test responsive behavior (320px, 768px, 1024px, 1440px)
- [ ] Test drawer open/close on mobile
- [ ] Test navigation on desktop

**Acceptance Criteria**:
- Layout uses only MudBlazor components
- No Bootstrap classes (`navbar`, `navbar-nav`, `nav-item`, etc.)
- No Font Awesome icon classes (`fas fa-*`)
- Responsive drawer works on mobile
- Navigation highlights active page
- All pages render within MudLayout

---

### T003-003: Code Cleanup & Consistency [P1]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

Remove unused code and ensure consistent patterns.

**Tasks**:
- [ ] Remove unused default template files
  - [ ] `Counter.razor` (if exists)
  - [ ] `Weather.razor` (if exists)
  - [ ] Default `NavMenu.razor` (if unused)
- [ ] Standardize page headers (consistent format across all pages)
- [ ] Standardize error handling patterns
- [ ] Standardize loading state patterns
- [ ] Remove duplicate code snippets
- [ ] Add XML documentation comments to public methods
- [ ] Run code formatter (dotnet format)

**Acceptance Criteria**:
- No unused template files remain
- All pages follow consistent patterns
- Code passes linting rules
- XML docs on all public APIs

---

## Phase 2: Page Migration to MudBlazor (5 days)

## Phase 3: Shared Components & Cleanup (3 days)

### T003-008: Create LoadingSpinner Component [P0]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

Convert Dashboard page to use only MudBlazor components.

**Tasks**:
- [ ] Replace Bootstrap grid with `<MudGrid>` and `<MudItem>`
- [ ] Replace `<div class="card">` with `<MudCard>` and `<MudCardContent>`
- [ ] Replace `<div class="alert alert-warning">` with `<MudAlert Severity="Severity.Warning">`
- [ ] Replace stat cards:
  - [ ] Remove Bootstrap `stat-card` classes
  - [ ] Use `<MudPaper>` for card containers
  - [ ] Use `<MudText Typo="Typo.h5">` for titles
  - [ ] Use `<MudIcon>` for stat icons (replace `<i class="fas fa-*">`)
- [ ] Replace all Font Awesome icons:
  - [ ] `fa-pills` → `Icons.Material.Filled.Medication`
  - [ ] `fa-check-circle` → `Icons.Material.Filled.CheckCircle`
  - [ ] `fa-chart-line` → `Icons.Material.Filled.ShowChart`
  - [ ] `fa-calendar-clock` → `Icons.Material.Filled.CalendarToday`
- [ ] Remove all Bootstrap utility classes (`mb-4`, `mt-3`, `d-flex`, etc.)
- [ ] Use MudBlazor spacing (Class="mt-4 mb-2")
- [ ] Test responsive layout at all breakpoints

**Acceptance Criteria**:
- No Bootstrap classes remain
- No Font Awesome icon classes remain
- Uses only MudBlazor components
- Responsive design works 320px-2560px
- Medical disclaimer uses `<MudAlert>`

---

### T003-005: Migrate Medications.razor to MudBlazor [P0]
**Owner**: TBD  
**Estimate**: 2 days  
**Status**: TODO

Convert Medications page to use only MudBlazor components.

**Tasks**:
- [ ] Replace search/filter controls:
  - [ ] `<input class="form-control">` → `<MudTextField>`
  - [ ] `<select class="form-select">` → `<MudSelect>`
  - [ ] `<button class="btn">` → `<MudButton>`
- [ ] Replace medication cards/table with responsive design:
  - [ ] Desktop (≥768px): Use `<MudDataGrid>` for sortable, filterable table view
  - [ ] Mobile (<768px): Use `<MudCard>` in grid layout for touch-friendly cards
  - [ ] Implement responsive breakpoint switch using MudBlazor `Breakpoint.Md`
  - [ ] Test transition between views at breakpoint
- [ ] Replace dropdown menu:
  - [ ] `<div class="dropdown">` → `<MudMenu>`
  - [ ] Add `<MudMenuItem>` for each action
- [ ] Replace all icons:
  - [ ] `fa-search` → `Icons.Material.Filled.Search`
  - [ ] `fa-plus` → `Icons.Material.Filled.Add`
  - [ ] `fa-pills` → `Icons.Material.Filled.Medication`
  - [ ] `fa-edit` → `Icons.Material.Filled.Edit`
  - [ ] `fa-trash` → `Icons.Material.Filled.Delete`
  - [ ] `fa-times` → `Icons.Material.Filled.Close`
- [ ] Replace status badges:
  - [ ] Bootstrap badges → `<MudChip>` with appropriate color
- [ ] Remove all Bootstrap grid classes (`row`, `col-*`)
- [ ] Use `<MudGrid>` and `<MudItem>` for layout
- [ ] Test search and filter functionality
- [ ] Test responsive behavior

**Acceptance Criteria**:
- No Bootstrap form controls remain
- No Font Awesome icons remain
- Uses MudDataGrid or MudCard consistently
- Search and filter work correctly
- Add/Edit/Delete actions work
- Mobile view is user-friendly

**Note**: Detailed migration steps and component mapping tables are in spec.md Phase 2 (lines 240-327). Tasks above reference this comprehensive guide.

---

### T003-006: Migrate INRTracking.razor to MudBlazor [P0]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

Convert INR Tracking page to use only MudBlazor components.

**Tasks**:
- [ ] Replace stat cards (same pattern as Dashboard)
- [ ] Replace alerts:
  - [ ] `<div class="alert alert-warning">` → `<MudAlert Severity="Severity.Warning">`
- [ ] Replace INR value table/list:
  - [ ] Use `<MudDataGrid>` or `<MudTable>`
  - [ ] Add sortable columns
  - [ ] Add color-coded INR values (red for out-of-range)
- [ ] Replace status indicators:
  - [ ] Bootstrap badges → `<MudChip Color="Color.Success/Error">`
- [ ] Replace all icons (same pattern as other pages)
- [ ] Add `<MudProgressLinear>` for time-in-range visualization
- [ ] Replace buttons with `<MudButton Variant="Filled/Outlined">`
- [ ] Remove Bootstrap grid classes
- [ ] Test responsive layout

**Acceptance Criteria**:
- No Bootstrap classes remain
- No Font Awesome icons remain
- INR values display with proper color coding
- Time-in-range shows visually (progress bar)
- Add/Edit/Delete INR tests work correctly

---

### T003-007: Migrate Form Pages (Login, Register, Profile) [P0]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

Convert authentication and profile pages to MudBlazor forms.

**Tasks**:
- [ ] **Login.razor**:
  - [ ] Replace `<input type="email">` → `<MudTextField T="string" InputType="InputType.Email">`
  - [ ] Replace `<input type="password">` → `<MudTextField T="string" InputType="InputType.Password">`
  - [ ] Replace `<button class="btn-primary">` → `<MudButton Variant="Filled" Color="Color.Primary">`
  - [ ] Add validation styling with `Error="true"` and `ErrorText="@errorMessage"`
- [ ] **Register.razor**:
  - [ ] Same pattern as Login
  - [ ] Add `<MudCheckBox>` for terms acceptance
  - [ ] Add `<MudDatePicker>` for date of birth (if exists)
- [ ] **Profile.razor**:
  - [ ] Replace all form controls with MudBlazor equivalents
  - [ ] Use `<MudSwitch>` for boolean settings
  - [ ] Use `<MudNumericField>` for INR target ranges
  - [ ] Use `<MudTextField>` with validation for text inputs
  - [ ] Replace tabs (if any) with `<MudTabs>` and `<MudTabPanel>`
- [ ] Remove all Bootstrap form classes (`form-control`, `form-label`, `form-select`)
- [ ] Test form validation
- [ ] Test form submission

**Acceptance Criteria**:
- All forms use MudBlazor form components
- Validation works correctly
- Error messages display properly
- Forms submit successfully
- No Bootstrap form classes remain

---
**Owner**: TBD  
**Estimate**: 0.5 days  
**Status**: TODO

**Tasks**:
- [ ] Create `Components/Shared/LoadingSpinner.razor`
- [ ] Add parameters:
  - [ ] `Text` (string, optional message)
  - [ ] `Size` (enum: Small, Medium, Large)
  - [ ] `Color` (MudBlazor color, default: Primary)
- [ ] Use `MudProgressCircular` component
- [ ] Add centered layout option
- [ ] Create usage examples in documentation

**Acceptance Criteria**:
- Component is reusable across all pages
- Supports customization via parameters
- Follows MudBlazor design patterns

---

### T003-009: Create ErrorMessage Component [P0]
**Owner**: TBD  
**Estimate**: 0.5 days  
**Status**: TODO

**Tasks**:
- [ ] Create `Components/Shared/ErrorMessage.razor`
- [ ] Add parameters:
  - [ ] `Message` (string, error text)
  - [ ] `Title` (string, optional title)
  - [ ] `ShowRetry` (bool, show retry button)
  - [ ] `OnRetry` (EventCallback, retry action)
- [ ] Use `MudAlert` component (Severity.Error)
- [ ] Add dismiss functionality
- [ ] Create usage examples

**Acceptance Criteria**:
- Component displays error messages consistently
- Supports retry functionality
- Can be dismissed by user
- Follows MudBlazor design patterns

---

### T003-010: Create EmptyState Component [P0]
**Owner**: TBD  
**Estimate**: 0.5 days  
**Status**: TODO

**Tasks**:
- [ ] Create `Components/Shared/EmptyState.razor`
- [ ] Add parameters:
  - [ ] `Icon` (string, MudBlazor icon name)
  - [ ] `Title` (string, main message)
  - [ ] `Message` (string, detailed message)
  - [ ] `ActionText` (string, optional button text)
  - [ ] `OnAction` (EventCallback, button action)
- [ ] Use `MudIcon` + `MudText` components
- [ ] Add centered layout with proper spacing
- [ ] Create usage examples

**Acceptance Criteria**:
- Component shows "no data" states clearly
- Supports optional call-to-action button
- Follows MudBlazor design patterns
- Used in Medications and INR pages

---

### T003-011: Refactor Pages to Use Shared Components [P1]
**Owner**: TBD  
**Estimate**: 1.5 days  
**Status**: TODO

**Tasks**:
- [ ] Update `Medications.razor` to use:
  - [ ] `<LoadingSpinner>` for loading states
  - [ ] `<ErrorMessage>` for API errors
  - [ ] `<EmptyState>` when no medications exist
- [ ] Update `INRTracking.razor` to use:
  - [ ] `<LoadingSpinner>` for loading states
  - [ ] `<ErrorMessage>` for API errors
  - [ ] `<EmptyState>` when no INR tests exist
- [ ] Update `Dashboard.razor` to use shared components
- [ ] Update `Profile.razor` to use shared components
- [ ] Remove duplicate loading/error markup from all pages

**Acceptance Criteria**:
- All pages use shared components consistently
- No duplicate loading/error UI code
- Empty states are user-friendly and actionable

---

## Phase 4: Testing (4 days)

### T003-012: Unit Tests for Services [P0]
**Owner**: TBD  
**Estimate**: 1.5 days  
**Status**: TODO

**Tasks**:
- [ ] Create `BloodThinnerTracker.Web.Tests` project (if not exists)
- [ ] Add test dependencies (xUnit, Moq, FluentAssertions)
- [ ] Write tests for `MedicationService`:
  - [ ] `GetMedicationsAsync_Success_ReturnsMedications`
  - [ ] `GetMedicationsAsync_ApiError_ReturnsError`
  - [ ] `CreateMedicationAsync_Success_ReturnsCreatedMedication`
  - [ ] `CreateMedicationAsync_ValidationError_ReturnsError`
- [ ] Write tests for `INRService`:
  - [ ] `GetINRTestsAsync_Success_ReturnsTests`
  - [ ] `GetINRTestsAsync_ApiError_ReturnsError`
  - [ ] `CreateINRTestAsync_Success_ReturnsCreatedTest`
- [ ] Mock HttpClient with proper responses
- [ ] Achieve 90%+ code coverage on service layer

**Acceptance Criteria**:
- All service methods have unit tests
- Tests use mocked HttpClient (no real API calls)
- Code coverage ≥ 90%
- All tests pass

---

### T003-013: Component Tests with bUnit [P1]
**Owner**: TBD  
**Estimate**: 2 days (expanded to include page components)
**Status**: TODO

**Tasks**:
- [ ] Add bUnit package to test project
- [ ] Create test project structure in `tests/BloodThinnerTracker.Web.Tests/Components/`

- [ ] **Test Shared Components** (0.5 days):
  - [ ] LoadingSpinner.razor - parameter variations, size/color
  - [ ] ErrorMessage.razor - message display, retry callback
  - [ ] EmptyState.razor - icon/title/message display

- [ ] **Test Layout Components** (0.5 days):
  - [ ] MainLayout.razor - AppBar renders, Drawer toggles, user menu displays
  - [ ] NavMenu.razor - links render, active state highlights, authentication state

- [ ] **Test Page Components** (1 day - CONSTITUTION COMPLIANCE):
  - [ ] Dashboard.razor tests:
    - [ ] Renders stats cards with correct data
    - [ ] Handles loading state (shows LoadingSpinner)
    - [ ] Handles error state (shows ErrorMessage)
    - [ ] Handles empty state (no medications/INR tests)
    - [ ] Calculates adherence percentage correctly
  - [ ] Medications.razor tests:
    - [ ] MudDataGrid renders with medication data
    - [ ] Search filter works correctly
    - [ ] Sort functionality works
    - [ ] Empty state displays when no data
    - [ ] Error handling displays ErrorMessage
  - [ ] INRTracking.razor tests:
    - [ ] MudTable renders with INR test data
    - [ ] Color-coding works (red for out-of-range)
    - [ ] Stats calculation is correct
    - [ ] Empty state displays when no tests
    - [ ] Error handling displays ErrorMessage
  - [ ] Profile.razor tests:
    - [ ] Form loads with user data
    - [ ] Validation works on submit
    - [ ] Success message displays on save

- [ ] Test component parameters and binding
- [ ] Test component events and callbacks
- [ ] Test conditional rendering logic
- [ ] Mock service dependencies (IMedicationService, IINRService)
- [ ] Verify 80%+ component test coverage (per Constitution Principle II)

**Acceptance Criteria**:
- ✅ All shared components have bUnit tests
- ✅ All layout components have bUnit tests
- ✅ **All page components (Dashboard, Medications, INR, Profile) have bUnit tests**
- ✅ Tests verify rendering, parameters, events, conditional logic
- ✅ **80%+ code coverage for all components (Constitution requirement)**
- ✅ All tests pass in CI/CD pipeline
- ✅ Tests use mocked services (no real API calls)
- ✅ Coverage report generated and reviewed

**Why This Matters**:
Constitution Principle II requires 90%+ overall test coverage. Without page component tests, we cannot meet this requirement since pages contain significant logic (state management, API calls, error handling).


---

### T003-014: Accessibility Testing (WCAG 2.1 AA) [P1]
**Owner**: TBD  
**Estimate**: 1.5 days  
**Status**: TODO

**Tasks**:
- [ ] **Automated Accessibility Audits**:
  - [ ] Run Lighthouse accessibility audit on all pages (Dashboard, Medications, INR, Profile, Login)
  - [ ] Run axe DevTools scan on all pages
  - [ ] Fix all HIGH and CRITICAL issues
  - [ ] Document any MEDIUM issues for future work

- [ ] **Keyboard Navigation Testing (WCAG 2.1.1, 2.1.2, 2.4.7)**:
  - [ ] **Tab Order**: Verify logical tab order on all pages
  - [ ] **Focus Indicators**: All interactive elements show visible focus (MudBlazor default + custom)
  - [ ] **Keyboard Shortcuts**:
    - [ ] Enter activates buttons/links
    - [ ] Space toggles checkboxes/switches
    - [ ] Escape closes dialogs/menus
    - [ ] Arrow keys navigate within MudDataGrid/MudTable
  - [ ] **No Keyboard Traps**: Can tab in and out of all components
  - [ ] **Skip Links**: Add "Skip to main content" link on MainLayout

- [ ] **Screen Reader Testing (WCAG 4.1.2, 4.1.3)**:
  - [ ] Test with NVDA (Windows) OR JAWS (if available)
  - [ ] **Forms**: All inputs have labels, error messages announced
  - [ ] **Tables**: MudDataGrid has proper headers, row/cell associations
  - [ ] **Buttons**: All buttons have accessible names (not just icons)
  - [ ] **Status Messages**: Loading/error states announced to screen reader
  - [ ] **Navigation**: Landmarks properly identified (nav, main, aside)
  - [ ] **Alt Text**: All icons have aria-label or title

- [ ] **Color Contrast (WCAG 1.4.3)**:
  - [ ] **Text Contrast**: All text meets 4.5:1 ratio (3:1 for large text ≥18pt)
  - [ ] **Interactive Element Contrast**: Buttons, links, inputs meet 3:1 ratio
  - [ ] Use WebAIM Contrast Checker for verification
  - [ ] Test MudBlazor theme colors against white/dark backgrounds
  - [ ] **Out-of-Range INR Values**: Red text must meet contrast (add background if needed)

- [ ] **High Contrast Mode Testing**:
  - [ ] Enable Windows High Contrast
  - [ ] Verify all UI elements visible
  - [ ] Test focus indicators still visible

- [ ] **Mobile Accessibility (Touch Targets - WCAG 2.5.5)**:
  - [ ] All touch targets ≥44x44 CSS pixels
  - [ ] Test on real device (Android/iOS)
  - [ ] Drawer navigation accessible with screen reader

- [ ] **Document Accessibility Features**:
  - [ ] Create accessibility statement (supported assistive technologies)
  - [ ] Document keyboard shortcuts in user guide
  - [ ] Add ARIA landmarks documentation for developers

**Acceptance Criteria**:
- ✅ **Lighthouse accessibility score ≥95 on all pages**
- ✅ **axe DevTools reports 0 HIGH/CRITICAL issues**
- ✅ All pages navigable by keyboard only (no mouse)
- ✅ Tab order is logical and intuitive
- ✅ Focus indicators visible on all interactive elements
- ✅ Escape closes all dialogs/menus
- ✅ **Screen reader announces all form labels, errors, and status changes**
- ✅ **All text meets 4.5:1 contrast ratio (WCAG AA)**
- ✅ **Interactive elements meet 3:1 contrast ratio**
- ✅ High contrast mode displays all UI elements
- ✅ Touch targets meet 44x44px minimum
- ✅ **WCAG 2.1 AA compliant** (verified with checklist)
- ✅ Accessibility statement created

**WCAG 2.1 AA Compliance Checklist**:
- [ ] 1.1.1 Non-text Content (Alt text)
- [ ] 1.3.1 Info and Relationships (Semantic HTML)
- [ ] 1.4.3 Contrast (4.5:1 text, 3:1 UI)
- [ ] 2.1.1 Keyboard Access
- [ ] 2.1.2 No Keyboard Trap
- [ ] 2.4.1 Bypass Blocks (Skip links)
- [ ] 2.4.7 Focus Visible
- [ ] 3.2.1 On Focus (No unexpected changes)
- [ ] 3.3.1 Error Identification
- [ ] 3.3.2 Labels or Instructions
- [ ] 4.1.2 Name, Role, Value (ARIA)
- [ ] 4.1.3 Status Messages

**Testing Tools**:
- Lighthouse (Chrome DevTools)
- axe DevTools (browser extension)
- NVDA screen reader (free)
- WebAIM Contrast Checker
- Windows High Contrast mode


---

## Phase 5: Documentation (2 days)

### T003-015: User Documentation [P1]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

**Tasks**:
- [ ] Create `docs/user-guide/medication-tracking.md`
  - [ ] How to view medications
  - [ ] Understanding medication information
  - [ ] Filtering and searching medications
  - [ ] (Future: How to add/edit medications)
- [ ] Create `docs/user-guide/inr-tracking.md`
  - [ ] How to view INR history
  - [ ] Understanding INR values and ranges
  - [ ] Interpreting color-coded results
  - [ ] What to do if INR is out of range
  - [ ] (Future: How to add INR tests)
- [ ] Create `docs/user-guide/getting-started.md`
  - [ ] First login guide
  - [ ] Dashboard overview
  - [ ] Navigation guide
- [ ] Create `docs/user-guide/authentication.md`
  - [ ] How to sign in with Microsoft/Google
  - [ ] Understanding authentication status
  - [ ] How to log out
  - [ ] Troubleshooting login issues
- [ ] Create `docs/user-guide/faq.md`
  - [ ] "Why can't I add medications yet?" (future feature)
  - [ ] "Why can't I add INR tests yet?" (future feature)
  - [ ] Authentication troubleshooting
  - [ ] Browser compatibility
- [ ] Add screenshots to user guides
- [ ] Update main `README.md` with user guide links

**Acceptance Criteria**:
- User guides are clear and well-organized
- Screenshots show current UI
- Guides include troubleshooting tips
- Accessible from main README

---

### T003-016: Developer Documentation [P1]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

**Tasks**:
- [ ] Create `docs/developer/blazor-architecture.md`
  - [ ] Project structure overview
  - [ ] Service layer pattern
  - [ ] Component hierarchy
  - [ ] State management approach
- [ ] Create `docs/developer/mudblazor-patterns.md`
  - [ ] Component usage guidelines
  - [ ] Theme customization
  - [ ] Responsive design patterns
  - [ ] Common component examples
- [ ] Create `docs/developer/testing-guide.md`
  - [ ] How to run tests
  - [ ] Writing unit tests for services
  - [ ] Writing component tests with bUnit
  - [ ] Mocking authentication
- [ ] Update `CONTRIBUTING.md` with Blazor-specific guidelines

**Acceptance Criteria**:
- Developer docs cover all key patterns
- Code examples are correct and runnable
- Testing guide enables new contributors
- Architecture is clearly documented

---

## Phase 6: Final Polish & Validation (1 day)

### T003-017: Performance Testing [P2]
**Owner**: TBD  
**Estimate**: 0.5 days  
**Status**: TODO

**Tasks**:
- [ ] Measure page load times:
  - [ ] Dashboard load time < 2 seconds
  - [ ] Medications page load time < 2 seconds
  - [ ] INR page load time < 2 seconds
- [ ] Test with realistic data volumes:
  - [ ] 50 medications
  - [ ] 100 INR tests
- [ ] Identify and fix performance bottlenecks
- [ ] Document performance benchmarks

**Acceptance Criteria**:
- All pages load within 2 seconds (with realistic data)
- No console errors or warnings
- Memory usage is reasonable (< 100MB for SPA)

---

### T003-018: Cross-Browser Testing [P2]
**Owner**: TBD  
**Estimate**: 0.5 days  
**Status**: TODO

**Tasks**:
- [ ] Test on Chrome (latest)
- [ ] Test on Firefox (latest)
- [ ] Test on Edge (latest)
- [ ] Test on Safari (latest) - if available
- [ ] Test on mobile browsers:
  - [ ] Chrome Mobile (Android)
  - [ ] Safari Mobile (iOS)
- [ ] Fix any browser-specific issues
- [ ] Document supported browsers

**Acceptance Criteria**:
- App works on all major browsers
- No critical bugs on any tested browser
- Mobile browsers render correctly

---

## Summary Statistics

### Overall Progress
- **Total Tasks**: 19
- **Completed**: 0
- **In Progress**: 0
- **TODO**: 19
- **Blocked**: 0

### By Priority
- **P0 (Critical)**: 10 tasks (includes T003-001 authentication fix)
- **P1 (High)**: 7 tasks
- **P2 (Medium)**: 2 tasks

### By Phase
- **Phase 1 (Authentication & Architecture)**: 4 tasks (6 days) - includes critical auth fix
- **Phase 2 (MudBlazor Migration)**: 4 tasks (5 days)
- **Phase 3 (Components & Cleanup)**: 4 tasks (3 days)
- **Phase 4 (Testing)**: 3 tasks (4.5 days) - **expanded T003-013 and T003-014**
- **Phase 5 (Documentation)**: 2 tasks (2 days)
- **Phase 6 (Polish)**: 2 tasks (1 day)

**Total Estimated Effort**: 21.5 days (~4.3 weeks with buffer)

**CRITICAL PATH**: T003-001 (Auth Fix) must be completed first - all other work depends on working authentication.

**Recent Updates** (from spec analysis remediation):
- T003-001: Clarified OAuth Razor page approach (vs MVC controller)
- T003-013: Expanded to include page component tests (Constitution compliance)
- T003-014: Added detailed WCAG 2.1 AA checklist with specific test cases


---

## Definition of Done

A task is considered "done" when:
- [ ] Code is written and reviewed
- [ ] Unit tests pass (if applicable)
- [ ] Integration tests pass (if applicable)
- [ ] Accessibility requirements met (if applicable)
- [ ] Documentation updated
- [ ] Code merged to `feature/003-core-ui-foundation` branch
- [ ] No blocking bugs or regressions

Feature 003 is considered "complete" when:
- [ ] All P0 and P1 tasks are done
- [ ] All tests pass (90%+ coverage)
- [ ] Accessibility audit passed (95+ Lighthouse score)
- [ ] User and developer documentation complete
- [ ] Code review approved
- [ ] PR merged to `main`
- [ ] Feature deployed to staging environment

---

**Last Updated**: October 29, 2025  
**Next Review**: November 5, 2025
