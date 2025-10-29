# Feature Roadmap - Quick Reference

**Last Updated**: October 28, 2025

## Feature Summary

| # | Feature | Priority | Status | Effort | Release |
|---|---------|----------|--------|--------|---------|
| 001 | Authentication Foundation | P0 | ✅ Complete | - | v1.0 |
| 002 | Docker Deployment Infrastructure | P0 | 🔄 In Progress (90%) | 1 week | v1.0.1 |
| 003 | Core UI Foundation | P1 | ⏳ Planned | 2 weeks | v1.1 |
| 004 | Medication Logging | P2 | ⏳ Planned | 3 weeks | v1.2 |
| 005 | INR Test Recording | P2 | ⏳ Planned | 3 weeks | v1.3 |
| 006 | Medication Reminders | P3 | ⏳ Planned | 4 weeks | v1.4 |
| 007 | INR Test Scheduling | P3 | ⏳ Planned | 3 weeks | v1.5 |

**Total Estimated Effort**: 16-20 weeks (excluding Feature 001-002 which are done/nearly done)

---

## Feature Dependency Graph

```
001: Authentication (✅)
         ↓
002: Deployment (🔄)
         ↓
003: Core UI
         ↓
    ┌────┴────┐
    ↓         ↓
004: Med Log  005: INR Record
    ↓         ↓
    └────┬────┘
         ↓
006: Reminders
         ↓
007: Scheduling
```

---

## Feature 002: Docker Deployment Infrastructure
**Branch**: `feature/002-docker-deployment-infrastructure`  
**Status**: 90% Complete

### Remaining Work
- [ ] Complete T051 technical debt items
- [ ] Verify production deployment stability
- [ ] Update deployment documentation
- [ ] Create deployment runbook

### When to Start Next Feature
After Feature 002 is merged to main and tagged as v1.0.1.

---

## Feature 003: Core UI Foundation
**Branch**: `feature/003-core-ui-foundation` (create when ready)  
**Spec**: `specs/features/003-core-ui-foundation.md`

### What It Includes
- Authentication-protected Blazor pages
- MudBlazor component library integration
- Medication list page (read-only)
- INR reading history page (read-only)
- Mobile-responsive navigation

### What It Doesn't Include
- Data entry forms (Feature 004, 005)
- Charts/visualizations (Feature 005)
- Reminders (Feature 006)

### How to Start
```bash
# After Feature 002 is merged
git checkout main
git pull origin main
git checkout -b feature/003-core-ui-foundation

# Generate detailed tasks
speckit tasks --spec specs/features/003-core-ui-foundation.md
```

---

## Feature 004: Medication Logging
**Branch**: `feature/004-medication-logging` (create when ready)  
**Dependencies**: Feature 003

### What It Includes
- Add medication dose form
- 12-hour safety window validation
- Medication history timeline
- Edit/delete dose entries
- Real-time sync with SignalR

### Key User Stories
- US-004-01: Log a new medication dose
- US-004-02: View medication dose history
- US-004-03: Edit a dose entry
- US-004-04: Delete a dose entry
- US-004-05: Prevent double-dosing within 12 hours

### Technical Highlights
- Entity Framework Core (Medication, MedicationLog)
- FluentValidation for 12-hour window
- SignalR for real-time sync
- Local-first architecture (offline support)

---

## Feature 005: INR Test Recording
**Branch**: `feature/005-inr-test-recording` (create when ready)  
**Dependencies**: Feature 003

### What It Includes
- Add INR test result form
- INR range validation (0.5-8.0)
- Test history with trend chart
- Edit/delete test entries
- Link tests to medication adjustments

### Key User Stories
- US-005-01: Record an INR test result
- US-005-02: View INR test history with trend
- US-005-03: Edit a test entry
- US-005-04: Delete a test entry
- US-005-05: See visual trend chart

### Technical Highlights
- Chart.js or MudBlazor charts for visualization
- INR range validation with warnings
- Target range customization per user
- Color-coded out-of-range indicators

---

## Feature 006: Medication Reminders
**Branch**: `feature/006-medication-reminders` (create when ready)  
**Dependencies**: Feature 004

### What It Includes
- Configurable reminder schedules
- Push notifications (iOS/Android)
- Browser notifications (web)
- Snooze/dismiss functionality
- Reminder history and analytics

### Key User Stories
- US-006-01: Set up medication reminder schedule
- US-006-02: Receive reminder notification
- US-006-03: Snooze a reminder
- US-006-04: Dismiss a reminder
- US-006-05: View reminder history

### Technical Highlights
- Background jobs with Hangfire or Quartz
- Firebase Cloud Messaging for mobile
- Web Push API for browser notifications
- Reliability: 99.9% delivery target
- Notification preferences per medication

---

## Feature 007: INR Test Scheduling
**Branch**: `feature/007-inr-test-scheduling` (create when ready)  
**Dependencies**: Features 005, 006

### What It Includes
- Configurable test schedules (weekly, bi-weekly, monthly)
- Next test date calculation based on INR stability
- Test reminders (reuses Feature 006 infrastructure)
- Schedule adjustment recommendations
- Calendar integration (iCal export)

### Key User Stories
- US-007-01: Set up INR test schedule
- US-007-02: Get reminder for upcoming test
- US-007-03: View next scheduled test date
- US-007-04: Adjust schedule based on INR stability
- US-007-05: Export schedule to calendar app

### Technical Highlights
- Business logic: Stable INR = less frequent tests
- Integration with reminder system from Feature 006
- iCalendar format export
- Recommendation engine based on INR history

---

## Branch Naming Convention

All feature branches follow the pattern: `feature/NNN-short-description`

Where:
- `NNN` = Feature number (001, 002, etc.)
- `short-description` = Kebab-case description

Examples:
- ✅ `feature/003-core-ui-foundation`
- ✅ `feature/004-medication-logging`
- ❌ `feature/medications` (no number)
- ❌ `feature/003_core_ui` (underscore instead of dash)

---

## Commit Message Convention

Follow Conventional Commits:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style (formatting, no logic change)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples**:
```
feat(ui): add medication list page with MudBlazor table

Implements read-only medication list using MudTable component.
Includes loading state, error handling, and mobile responsiveness.

Relates to US-003-01
```

```
fix(auth): correct redirect after logout

Users were being redirected to /login instead of / after logout.
Changed redirect target to home page.

Fixes #123
```

---

## Pull Request Template

When creating a PR, include:

```markdown
## Feature
Feature 003: Core UI Foundation

## Description
Implements read-only medication list and INR history pages with MudBlazor components.

## Changes
- Added MedicationList.razor page
- Added INRHistory.razor page
- Created MedicationService and INRService
- Updated NavMenu with new links
- Added unit tests for services

## Testing
- [x] Unit tests pass (90% coverage)
- [x] Integration tests pass
- [x] Tested on mobile (iPhone, Android)
- [x] Accessibility audit passed (WCAG 2.1 AA)
- [x] Code review completed

## Screenshots
[Attach screenshots of medication list and INR history pages]

## Deployment Notes
- No database migrations
- No configuration changes
- Feature flag: `core-ui-enabled` (default: false)

## Related Issues
Closes #45, #67

## Checklist
- [x] Code follows project conventions
- [x] Tests added/updated
- [x] Documentation updated
- [x] No console errors
- [x] Responsive design verified
```

---

## Definition of Done

A feature is "Done" when:

### Code Quality
- ✅ All code reviewed and approved
- ✅ Follows .NET coding conventions
- ✅ No StyleCop violations
- ✅ No compiler warnings

### Testing
- ✅ 90%+ test coverage
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ E2E tests pass (if applicable)
- ✅ Accessibility tests pass

### Documentation
- ✅ User documentation updated
- ✅ Developer documentation updated
- ✅ API documentation updated (if applicable)
- ✅ README updated (if needed)

### Deployment
- ✅ Deployed to staging
- ✅ QA testing completed
- ✅ Performance benchmarks met
- ✅ Security scan passed

### Release
- ✅ Merged to main
- ✅ Tagged with version number
- ✅ Release notes created
- ✅ Feature flag removed (after stable)

---

## Communication

### Weekly Status Update Template

```markdown
## Feature [NNN]: [Name]
**Week of**: [Date]
**Status**: [On Track | At Risk | Blocked]

### This Week
- Completed: [List completed tasks]
- In Progress: [List current tasks]

### Next Week
- Planned: [List upcoming tasks]

### Risks/Blockers
- [List any issues that need attention]

### Questions
- [List any questions for the team]
```

---

## Questions?

If you have questions about:
- **Feature scope**: Review the spec in `specs/features/`
- **Technical approach**: Check the spec's Technical Design section
- **Dependencies**: Review the dependency graph above
- **Priorities**: Features are ordered by priority (002 → 003 → 004 → ...)

**Need help?** Contact the development team or review the constitution at `.specify/memory/constitution.md`

---

**Document Owner**: Development Team  
**Created**: October 28, 2025  
**Next Review**: After each feature completion
