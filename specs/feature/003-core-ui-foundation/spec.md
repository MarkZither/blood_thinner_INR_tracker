# Feature 003: Core UI Foundation

**Status**: Planned  
**Priority**: P1  
**Branch**: `feature/003-core-ui-foundation`  
**Dependencies**: None (Feature 002 must be merged first)  
**Estimated Effort**: 2 weeks  
**Target Release**: v1.1

---

## Overview

Establish the foundational UI structure for the Blood Thinner Tracker application using Blazor Server and MudBlazor components. This feature provides authentication-protected pages for viewing existing medication and INR data (read-only initially) with mobile-responsive design.

**Why This Feature**: Creates the baseline UI framework that all subsequent features will build upon. Users can authenticate and view their data before we add complex data entry functionality.

---

## Goals

### Primary Goals
1. Implement authentication-protected Blazor pages
2. Integrate MudBlazor component library for consistent UI
3. Create read-only medication list page
4. Create read-only INR reading history page
5. Implement mobile-responsive navigation

### Secondary Goals
1. Set up page routing and layouts
2. Create reusable UI components
3. Establish UI design patterns and standards
4. Implement loading states and error handling

### Non-Goals (Future Features)
- ❌ Data entry forms (Feature 004, 005)
- ❌ Real-time notifications (Feature 006)
- ❌ Reminder scheduling (Feature 006, 007)
- ❌ Charts and visualizations (Feature 005)

---

## User Stories

### US-003-01: View Medication List
**As a** patient  
**I want to** view my current medications  
**So that** I can see what I'm taking and when

**Acceptance Criteria**:
- User must be authenticated to access page
- Page displays list of medications with name, dosage, frequency
- List is sorted by most recent dose first
- Empty state shows "No medications yet" message
- Page loads within 2 seconds
- Page is mobile-responsive

---

### US-003-02: View INR History
**As a** patient  
**I want to** view my INR test history  
**So that** I can track my blood clotting levels over time

**Acceptance Criteria**:
- User must be authenticated to access page
- Page displays list of INR tests with date, value, notes
- List is sorted by most recent test first
- Empty state shows "No INR tests yet" message
- Page loads within 2 seconds
- Page is mobile-responsive

---

### US-003-03: Navigate Between Pages
**As a** patient  
**I want to** easily navigate between medication and INR pages  
**So that** I can quickly access the information I need

**Acceptance Criteria**:
- Navigation menu/drawer is accessible from all pages
- Current page is highlighted in navigation
- Navigation works on mobile and desktop
- Navigation collapses on mobile (hamburger menu)
- Logout button is accessible from navigation

---

## Technical Design

### Architecture

```
┌─────────────────────────────────────────┐
│         Blazor Server (Web)             │
├─────────────────────────────────────────┤
│  Pages/                                 │
│    ├── Login.razor (existing)           │
│    ├── MedicationList.razor (new)       │
│    ├── INRHistory.razor (new)           │
│    └── Index.razor (dashboard)          │
├─────────────────────────────────────────┤
│  Components/                            │
│    ├── Layout/                          │
│    │   ├── MainLayout.razor             │
│    │   └── NavMenu.razor                │
│    └── Shared/                          │
│        ├── LoadingSpinner.razor         │
│        └── ErrorMessage.razor           │
├─────────────────────────────────────────┤
│  Services/                              │
│    ├── MedicationService.cs             │
│    └── INRService.cs                    │
└─────────────────────────────────────────┘
         ↓ HTTP/SignalR
┌─────────────────────────────────────────┐
│         API (Existing)                  │
│    GET /api/medications                 │
│    GET /api/inr-tests                   │
└─────────────────────────────────────────┘
```

### Component Hierarchy

```
App.razor
└── MainLayout.razor
    ├── MudThemeProvider
    ├── MudDialogProvider
    ├── MudSnackbarProvider
    └── MudLayout
        ├── MudAppBar (with auth state)
        ├── MudDrawer (navigation)
        └── Body (@Body)
            ├── Index.razor (dashboard)
            ├── MedicationList.razor
            └── INRHistory.razor
```

### Data Flow

```
User → Page Component → Service → HttpClient → API → Database
                ↓
         MudBlazor Components (render data)
```

---

## Implementation Plan

### Phase 1: Setup & Configuration
**Duration**: 2 days

1. **Install MudBlazor** (if not already installed)
   ```bash
   dotnet add src/BloodThinnerTracker.Web package MudBlazor
   ```

2. **Configure MudBlazor in Program.cs**
   ```csharp
   builder.Services.AddMudServices();
   ```

3. **Update MainLayout.razor** to use MudBlazor layout components

4. **Create base page routing**

---

### Phase 2: Navigation & Layout
**Duration**: 2 days

1. **Create NavMenu.razor with MudBlazor components**
   - MudNavMenu with authentication state
   - Links to: Dashboard, Medications, INR History, Logout

2. **Create MainLayout.razor**
   - MudLayout with AppBar and Drawer
   - Authentication state display (username, avatar)
   - Responsive breakpoints for mobile

3. **Create Index.razor (Dashboard)**
   - Welcome message with user's name
   - Quick stats (total medications, latest INR)
   - Links to main pages

---

### Phase 3: Medication List Page
**Duration**: 3 days

1. **Create MedicationList.razor page**
   - `@page "/medications"`
   - `@attribute [Authorize]`
   - MudTable to display medications

2. **Create MedicationService.cs**
   - `GetMedicationsAsync()` method
   - Calls API: `GET /api/medications`
   - Handles loading and error states

3. **Implement UI**
   - MudTable with columns: Name, Dosage, Frequency, Last Dose
   - MudProgressCircular for loading state
   - MudAlert for error state
   - Empty state with MudText

4. **Add mobile responsiveness**
   - Card view on mobile (stacked, not table)
   - Swipe gestures for future actions

---

### Phase 4: INR History Page
**Duration**: 3 days

1. **Create INRHistory.razor page**
   - `@page "/inr-history"`
   - `@attribute [Authorize]`
   - MudTable to display INR tests

2. **Create INRService.cs**
   - `GetINRTestsAsync()` method
   - Calls API: `GET /api/inr-tests`
   - Handles loading and error states

3. **Implement UI**
   - MudTable with columns: Date, INR Value, Target Range, Notes
   - Color-coded INR values (red if out of range)
   - MudProgressCircular for loading state
   - MudAlert for error state
   - Empty state with MudText

4. **Add mobile responsiveness**
   - Card view on mobile
   - Highlight out-of-range values

---

### Phase 5: Shared Components
**Duration**: 2 days

1. **Create LoadingSpinner.razor**
   - Reusable MudProgressCircular with consistent styling
   - Parameterized text message

2. **Create ErrorMessage.razor**
   - Reusable MudAlert for error display
   - Parameterized error text
   - Retry button support

3. **Create EmptyState.razor**
   - Reusable component for "no data" states
   - Parameterized icon, title, message

---

### Phase 6: Testing & Polish
**Duration**: 2 days

1. **Write unit tests**
   - Service layer tests with mocked HttpClient
   - Component tests with bUnit

2. **Test responsive design**
   - Mobile breakpoints (320px, 375px, 768px)
   - Tablet breakpoints (1024px)
   - Desktop breakpoints (1440px+)

3. **Accessibility testing**
   - Keyboard navigation
   - Screen reader compatibility
   - WCAG 2.1 AA compliance

4. **Performance testing**
   - Page load times
   - API response times
   - Memory usage

---

## API Requirements

### Existing API Endpoints (Used by this feature)

#### GET /api/medications
**Description**: Returns list of medications for authenticated user

**Request**:
```http
GET /api/medications
Authorization: Bearer {jwt_token}
```

**Response**:
```json
{
  "medications": [
    {
      "id": "123",
      "name": "Warfarin",
      "dosage": "5mg",
      "frequency": "Once daily",
      "lastDose": "2025-10-28T08:00:00Z",
      "isActive": true
    }
  ]
}
```

---

#### GET /api/inr-tests
**Description**: Returns list of INR tests for authenticated user

**Request**:
```http
GET /api/inr-tests
Authorization: Bearer {jwt_token}
```

**Response**:
```json
{
  "tests": [
    {
      "id": "456",
      "date": "2025-10-28",
      "value": 2.5,
      "targetMin": 2.0,
      "targetMax": 3.0,
      "notes": "Feeling good",
      "isInRange": true
    }
  ]
}
```

---

## Success Criteria

### Functional Requirements
- ✅ User can log in and see medication list
- ✅ User can view INR test history
- ✅ Navigation between pages works correctly
- ✅ All pages require authentication
- ✅ Logout functionality works

### Non-Functional Requirements
- ✅ Pages load within 2 seconds
- ✅ Responsive design works on all devices
- ✅ WCAG 2.1 AA accessibility compliance
- ✅ 90%+ test coverage
- ✅ No console errors or warnings

### Quality Gates
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ Code review approved
- ✅ Accessibility audit passed
- ✅ Performance benchmarks met

---

## Testing Strategy

### Unit Tests
- Service layer: Mock HttpClient, test API calls
- Component logic: Test state management, data binding

### Integration Tests
- API integration: Test real API calls (dev environment)
- Authentication: Test protected routes

### E2E Tests (Playwright)
- Login → Navigate to Medications → See list
- Login → Navigate to INR History → See list
- Mobile navigation flow

### Accessibility Tests
- Keyboard navigation through all pages
- Screen reader compatibility (NVDA, JAWS)
- Color contrast validation

---

## Risks & Mitigations

### Risk 1: API Not Ready
**Probability**: Medium  
**Impact**: High  
**Mitigation**: 
- Verify API endpoints exist before starting
- Use mock data service for development if needed
- Parallel development with API team

### Risk 2: MudBlazor Learning Curve
**Probability**: Low  
**Impact**: Medium  
**Mitigation**:
- Review MudBlazor documentation upfront
- Create proof-of-concept components first
- Pair programming for complex components

### Risk 3: Mobile Responsiveness Issues
**Probability**: Medium  
**Impact**: Medium  
**Mitigation**:
- Test on real devices early
- Use browser dev tools for breakpoint testing
- Follow MudBlazor responsive patterns

---

## Dependencies

### External
- MudBlazor 8.13.0+ (already installed)
- .NET 10 RC2+ (already installed)
- API endpoints (must be deployed)

### Internal
- Feature 002 must be merged (deployment infrastructure)
- Authentication must be working (completed)

---

## Documentation

### User Documentation
- "Getting Started" guide for viewing medications
- "Getting Started" guide for viewing INR history
- FAQ: "Why can't I add medications yet?"

### Developer Documentation
- MudBlazor component usage guide
- Service layer architecture
- Testing guidelines for Blazor components

---

## Rollout Plan

### Development
1. Create branch: `feature/003-core-ui-foundation`
2. Implement in phases (2 weeks)
3. Code review
4. Merge to main

### Staging
1. Deploy to staging environment
2. QA testing (2 days)
3. Fix any issues
4. Re-deploy and re-test

### Production
1. Feature flag: `core-ui-enabled` (default: false)
2. Enable for beta users (10%)
3. Monitor for 48 hours
4. Gradually roll out to 50%, then 100%
5. Remove feature flag after 1 week

---

## Future Enhancements (Not in this feature)

- Data entry forms (Feature 004, 005)
- Charts and visualizations (Feature 005)
- Real-time updates with SignalR
- Offline support (PWA)
- Export data (CSV, PDF)
- Print-friendly views

---

## Questions & Decisions

### Q: Should we use MudBlazor or build custom components?
**A**: Use MudBlazor. It's already integrated, provides accessibility, and reduces development time.

### Q: Should we show demo data if user has no data?
**A**: No. Show clear empty states with instructions to add data (in future feature).

### Q: Should we cache API responses?
**A**: Not in this feature. Focus on basic functionality. Caching can be added in a future performance optimization feature.

---

**Spec Owner**: Development Team  
**Created**: October 28, 2025  
**Last Updated**: October 28, 2025  
**Next Review**: After implementation starts
