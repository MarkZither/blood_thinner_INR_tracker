# Feature 003: Core UI Foundation - Plan

**Note**: This feature uses a combined spec+plan approach. See `spec.md` for comprehensive technical design including:

- **Architecture**: Lines 182-215 (component hierarchy, data flow)
- **Implementation Phases**: Lines 217-418 (6 phases with detailed steps)
- **Technology Stack**: MudBlazor 8.13.0+, .NET 10, Blazor Server
- **API Integration**: Lines 419-477 (endpoint specifications)

This approach eliminates duplication and keeps all technical decisions in one authoritative document.

## Quick Reference

### Tech Stack
- **Frontend**: Blazor Server (Interactive Server render mode)
- **UI Framework**: MudBlazor 8.13.0+ (Material Design)
- **Icons**: Material Icons (via MudBlazor)
- **State Management**: Blazor component state + services
- **API Communication**: HttpClient with AuthorizationMessageHandler
- **Authentication**: JWT tokens via OAuth 2.0 (Azure AD + Google)

### Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| UI Framework | MudBlazor only | Constitution Principle III - pure .NET, no JavaScript |
| Service Layer | HttpClient wrapper services | Separation of concerns, testability |
| Authentication | CustomAuthenticationStateProvider | Browser storage, works with SSR/CSR hybrid |
| Layout System | MudLayout + MudDrawer | Responsive, accessible, Material Design |
| Data Grid | MudDataGrid | Advanced features, sortable, filterable |

### Component Structure

```
src/BloodThinnerTracker.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor (MudLayout, MudAppBar, MudDrawer)
│   │   └── NavMenu.razor (MudNavMenu with MudNavLinks)
│   ├── Pages/
│   │   ├── Dashboard.razor (stats cards, MudCard)
│   │   ├── Medications.razor (MudDataGrid, search, filter)
│   │   ├── INRTracking.razor (MudTable, color-coded values)
│   │   ├── Login.razor (OAuth buttons)
│   │   └── Profile.razor (MudTextField forms)
│   └── Shared/
│       ├── LoadingSpinner.razor (MudProgressCircular wrapper)
│       ├── ErrorMessage.razor (MudAlert wrapper)
│       └── EmptyState.razor (no data display)
├── Services/
│   ├── IMedicationService.cs / MedicationService.cs
│   ├── IINRService.cs / INRService.cs
│   ├── CustomAuthenticationStateProvider.cs
│   └── AuthorizationMessageHandler.cs
```

### Data Flow Patterns

**Read Pattern** (existing, for display-only pages):
```
User → Page Component → Service.GetAsync() → HttpClient → API → JSON Response → Model → MudBlazor Component
```

**Authentication Pattern**:
```
User → Login Page → OAuth Provider → Callback Handler → JWT Token → LocalStorage → AuthorizationMessageHandler → API (with Bearer token)
```

**Error Handling Pattern**:
```
Service throws exception → Page catches → ErrorMessage component displays → User sees friendly message
```

### Performance Targets

- Initial page load: <1 second
- API data load: <2 seconds total
- Navigation transition: <200ms
- Memory usage: <100MB
- Lighthouse score: 95+

### Security Measures

- All medical data behind authentication
- JWT tokens in localStorage (not cookies)
- Token expiry checked on every API call
- 401 responses trigger auto-logout
- No medical data in browser cache post-logout

---

**For detailed implementation steps, see `spec.md` Phases 1-6.**
