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

## Phase 1: Architecture & Code Organization (5 days)

### T003-001: Create Service Layer [P0]
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

### T003-002: Refactor Layout to MudBlazor [P1]
**Owner**: TBD  
**Estimate**: 2 days  
**Status**: TODO

Replace Bootstrap navigation with MudBlazor components for consistency.

**Tasks**:
- [ ] Update `MainLayout.razor` to use MudBlazor components
  - [ ] Replace Bootstrap navbar with `MudAppBar`
  - [ ] Add `MudDrawer` for side navigation
  - [ ] Implement responsive drawer (collapsible on mobile)
  - [ ] Add `MudThemeProvider` configuration
- [ ] Create new `NavMenu.razor` with MudBlazor
  - [ ] Use `MudNavMenu` and `MudNavLink` components
  - [ ] Add icons with `MudIcon`
  - [ ] Highlight active page
  - [ ] Add user info/avatar
  - [ ] Add logout button
- [ ] Update CSS/styling
  - [ ] Remove Bootstrap navbar styles
  - [ ] Add MudBlazor theme customization
  - [ ] Ensure mobile responsiveness
- [ ] Test navigation on all breakpoints (320px, 768px, 1024px, 1440px)

**Acceptance Criteria**:
- All pages use MudBlazor layout components
- Navigation works on mobile and desktop
- Current page highlighted in navigation
- Responsive drawer closes automatically on mobile after navigation
- No Bootstrap navbar dependencies remain

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

## Phase 2: Shared Components (3 days)

### T003-004: Create LoadingSpinner Component [P0]
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

### T003-005: Create ErrorMessage Component [P0]
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

### T003-006: Create EmptyState Component [P0]
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

### T003-007: Refactor Pages to Use Shared Components [P1]
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

## Phase 3: Testing (4 days)

### T003-008: Unit Tests for Services [P0]
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

### T003-009: Component Tests with bUnit [P1]
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

### T003-010: Accessibility Testing [P1]
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

## Phase 4: Documentation (2 days)

### T003-011: User Documentation [P1]
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

### T003-012: Developer Documentation [P1]
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

## Phase 5: Final Polish & Validation (1 day)

### T003-013: Performance Testing [P2]
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

### T003-014: Cross-Browser Testing [P2]
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
- **Total Tasks**: 14
- **Completed**: 0
- **In Progress**: 0
- **TODO**: 14
- **Blocked**: 0

### By Priority
- **P0 (Critical)**: 5 tasks
- **P1 (High)**: 7 tasks
- **P2 (Medium)**: 2 tasks

### By Phase
- **Phase 1 (Architecture)**: 3 tasks (5 days)
- **Phase 2 (Components)**: 4 tasks (3 days)
- **Phase 3 (Testing)**: 3 tasks (4 days)
- **Phase 4 (Documentation)**: 2 tasks (2 days)
- **Phase 5 (Polish)**: 2 tasks (1 day)

**Total Estimated Effort**: 15 days (3 weeks with buffer)

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
