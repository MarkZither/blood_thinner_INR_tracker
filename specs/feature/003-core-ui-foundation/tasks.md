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
- [ ] **Register CustomAuthenticationStateProvider in Program.cs**
  - [ ] Add `builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();`
  - [ ] Add `builder.Services.AddScoped<CustomAuthenticationStateProvider>();` (for direct injection)
  - [ ] Verify injection works in AuthorizationMessageHandler

- [ ] **Implement OAuth Callback Handlers**
  - [ ] Create `OAuthCallbackController.cs` or Razor page for `/signin-microsoft`
  - [ ] Create handler for `/signin-google`
  - [ ] Exchange authorization code for JWT access token + refresh token
  - [ ] Call `CustomAuthenticationStateProvider.MarkUserAsAuthenticatedAsync(token, refreshToken)`
  - [ ] Redirect to `/dashboard` on success, `/login?error=...` on failure
  - [ ] Add comprehensive error logging for OAuth failures

- [ ] **Fix Bearer Token Injection**
  - [ ] Verify `AuthorizationMessageHandler.SendAsync` can retrieve token
  - [ ] Add logging: "Token retrieved: {HasToken}" before adding header
  - [ ] Test API call includes `Authorization: Bearer {token}` header
  - [ ] Verify 401 responses trigger logout (already implemented in handler)

- [ ] **Add Authentication Route Guards**
  - [ ] Create `AuthorizedLayoutBase` or similar base component
  - [ ] Check authentication state in `OnInitializedAsync`
  - [ ] Redirect to `/login?returnUrl={currentUrl}` if not authenticated
  - [ ] Apply to Dashboard, Medications, INR, Profile pages

- [ ] **Fix Logout UI Visibility**
  - [ ] Replace Bootstrap dropdown in MainLayout with MudBlazor MudMenu
  - [ ] Ensure logout link is visible and clickable
  - [ ] Test logout flow: click → `/logout` → token cleared → redirect to `/login`

- [ ] **Add Authentication State Debugging**
  - [ ] Log authentication state on every protected page load
  - [ ] Log token presence/absence before API calls
  - [ ] Add `[Authorize]` attribute validation logging
  - [ ] Create `/auth/status` debug page showing: IsAuthenticated, Token Present, Claims

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
- [ ] Register services in `Program.cs`
- [ ] Update pages to use services instead of direct HttpClient

**Acceptance Criteria**:
- All API calls go through service layer
- Services use dependency injection
- Services handle errors and return typed results
- Pages have no direct HttpClient dependencies

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
- [ ] Replace medication cards/table:
  - [ ] Option A: Use `<MudDataGrid>` for table view
  - [ ] Option B: Use `<MudCard>` for card view
  - [ ] Implement responsive switch (table on desktop, cards on mobile)
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
**Estimate**: 1.5 days  
**Status**: TODO

**Tasks**:
- [ ] Add bUnit package to test project
- [ ] Write tests for shared components:
  - [ ] `LoadingSpinner_Renders_WithCorrectText`
  - [ ] `ErrorMessage_ShowsRetry_WhenEnabled`
  - [ ] `EmptyState_RendersAction_WhenProvided`
- [ ] Write tests for page components:
  - [ ] `MedicationsPage_LoadsData_OnInitialized`
  - [ ] `MedicationsPage_ShowsEmptyState_WhenNoData`
  - [ ] `INRTrackingPage_LoadsData_OnInitialized`
  - [ ] `INRTrackingPage_ShowsEmptyState_WhenNoData`
- [ ] Mock authentication context for protected pages
- [ ] Achieve 80%+ code coverage on components

**Acceptance Criteria**:
- Shared components have comprehensive tests
- Key page rendering scenarios tested
- Code coverage ≥ 80% on tested components
- All tests pass

---

### T003-014: Accessibility Testing [P1]
**Owner**: TBD  
**Estimate**: 1 day  
**Status**: TODO

**Tasks**:
- [ ] Install accessibility testing tools (axe DevTools, Lighthouse)
- [ ] Test keyboard navigation:
  - [ ] Tab through all interactive elements
  - [ ] Enter/Space activate buttons and links
  - [ ] Escape closes modals/drawers
- [ ] Test screen reader compatibility:
  - [ ] All images have alt text
  - [ ] Form inputs have labels
  - [ ] ARIA labels on custom components
- [ ] Run automated accessibility audit:
  - [ ] Lighthouse accessibility score ≥ 95
  - [ ] axe DevTools reports no critical issues
- [ ] Test color contrast (WCAG 2.1 AA):
  - [ ] Text contrast ratio ≥ 4.5:1
  - [ ] Large text contrast ratio ≥ 3:1
- [ ] Document accessibility findings and fixes

**Acceptance Criteria**:
- Lighthouse accessibility score ≥ 95
- No critical accessibility issues
- WCAG 2.1 AA compliance verified
- Keyboard navigation works on all pages

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
  - [ ] What to do if INR is out of range
  - [ ] (Future: How to add INR tests)
- [ ] Create `docs/user-guide/getting-started.md`
  - [ ] First login guide
  - [ ] Dashboard overview
  - [ ] Navigation guide
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
- **Phase 4 (Testing)**: 3 tasks (4 days)
- **Phase 5 (Documentation)**: 2 tasks (2 days)
- **Phase 6 (Polish)**: 2 tasks (1 day)

**Total Estimated Effort**: 21 days (4.2 weeks with buffer)

**CRITICAL PATH**: T003-001 (Auth Fix) must be completed first - all other work depends on working authentication.

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
