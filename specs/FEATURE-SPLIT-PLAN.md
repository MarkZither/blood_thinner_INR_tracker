# Blood Thinner Tracker - Feature Split Plan

**Date**: October 28, 2025  
**Status**: In Progress  
**Reason**: Original feature scope grew beyond manageable size

## Overview

The original `feature/blood-thinner-medication-tracker` branch combined authentication, deployment infrastructure, and core application features. This has been split into focused, independently deliverable features.

## Completed Work

### Feature 001: Authentication Foundation ✅
**Status**: Completed and Merged  
**Scope**:
- Azure AD OAuth integration
- Google OAuth integration  
- JWT token handling
- User secrets configuration
- Options pattern for configuration
- Login/logout flows

**Outcome**: Authentication is working in production

---

## Active Work

### Feature 002: Docker Deployment Infrastructure 🔄
**Branch**: `feature/002-docker-deployment-infrastructure`  
**Priority**: P0 (Infrastructure)  
**Status**: In Progress

**Scope**:
- .NET 10 RC2 Dockerfile configuration
- Azure Container Apps deployment
- Managed registry integration
- HTTP-only container configuration (HTTPS at ingress)
- Health checks
- Production deployment verification

**Remaining Tasks**:
- [ ] Complete technical debt items (T051)
- [ ] Verify production deployment stability
- [ ] Update deployment documentation
- [ ] Create deployment runbook

**Success Criteria**:
- API deploys successfully to Azure Container Apps
- Health checks pass
- HTTP traffic works (HTTPS at Azure ingress)
- Zero-downtime deployments
- Documentation is complete

**Estimated Completion**: 1 week

---

## Planned Features

### Feature 003: Core UI Foundation
**Branch**: `feature/003-core-ui-foundation`  
**Priority**: P1  
**Status**: Not Started

**Scope**:
- Authentication-protected Blazor pages
- MudBlazor component library integration
- Medication list page (read-only view)
- INR reading list page (read-only view)
- Mobile-responsive navigation
- Basic routing and layout

**Why This First**: Establishes UI foundation without complex data entry. Users can see their data.

**Success Criteria**:
- User can log in and navigate to medication list
- User can view INR reading history
- All pages require authentication
- Responsive design works on mobile
- No JavaScript errors

**Estimated Effort**: 2 weeks

---

### Feature 004: Medication Logging
**Branch**: `feature/004-medication-logging`  
**Priority**: P2  
**Status**: Not Started

**Dependencies**: Feature 003 (Core UI)

**Scope**:
- Add new medication dose form
- 12-hour safety window validation
- Medication history timeline
- Edit medication dose entries
- Delete medication dose entries
- Local-first data with SignalR sync

**Why This Next**: Most critical user feature. Builds on read-only UI.

**Success Criteria**:
- User can log a medication dose
- System prevents double-dosing within 12 hours
- Dose history displays in timeline
- Edit/delete operations work
- Changes sync across devices within 5 seconds
- 90%+ test coverage

**Estimated Effort**: 3 weeks

---

### Feature 005: INR Test Recording
**Branch**: `feature/005-inr-test-recording`  
**Priority**: P2  
**Status**: Not Started

**Dependencies**: Feature 003 (Core UI)

**Scope**:
- Add new INR test result form
- INR range validation (0.5-8.0)
- Test history with trend visualization
- Edit INR test entries
- Delete INR test entries
- Link test results to medication adjustments

**Why Independent**: Can be developed in parallel with Feature 004.

**Success Criteria**:
- User can record INR test results
- System validates INR ranges
- Test history shows trend chart
- Can link test to medication changes
- Chart displays correctly on mobile
- 90%+ test coverage

**Estimated Effort**: 3 weeks

---

### Feature 006: Medication Reminders
**Branch**: `feature/006-medication-reminders`  
**Priority**: P3  
**Status**: Not Started

**Dependencies**: Feature 004 (Medication Logging)

**Scope**:
- Configurable reminder schedules
- Push notifications (MAUI mobile)
- Browser notifications (Blazor web)
- Snooze/dismiss functionality
- Reminder history and analytics
- Background job processing

**Why Later**: Complex, platform-specific. Requires stable medication logging.

**Success Criteria**:
- User receives timely reminders on all devices
- Push notifications work on iOS/Android
- Browser notifications work in Chrome/Edge/Safari
- User can snooze or dismiss reminders
- 99.9% reminder delivery rate
- Background jobs handle failure gracefully

**Estimated Effort**: 4 weeks

---

### Feature 007: INR Test Scheduling
**Branch**: `feature/007-inr-test-scheduling`  
**Priority**: P3  
**Status**: Not Started

**Dependencies**: Feature 005 (INR Test Recording), Feature 006 (Reminders)

**Scope**:
- Configurable test schedule (weekly, bi-weekly, monthly)
- Next test date calculation based on INR stability
- Test reminders (reuses reminder infrastructure)
- Schedule adjustment recommendations
- Calendar integration (iCal export)

**Why Last**: Complex business logic. Builds on reminders infrastructure.

**Success Criteria**:
- System suggests next test date based on INR history
- User receives test reminders 1 day before
- Schedule adapts to INR stability patterns
- Calendar export works with major calendar apps
- Recommendations align with medical guidelines

**Estimated Effort**: 3 weeks

---

## Migration Strategy

### Phase 1: Complete Feature 002 ✅ (Current)
1. Finish technical debt items
2. Verify production deployment
3. Merge to main
4. Tag release: `v1.0-deployment-infrastructure`

### Phase 2: Feature 003 (Next)
1. Create branch from main: `feature/003-core-ui-foundation`
2. Run `speckit specify` for detailed spec
3. Run `speckit tasks` for task breakdown
4. Implement and test
5. Merge to main
6. Tag release: `v1.1-core-ui`

### Phase 3: Features 004-005 (Parallel Development Possible)
1. Both features depend on Feature 003
2. Can be developed independently by different developers
3. Feature 004 is higher priority (medication safety)
4. Both merge to main when complete
5. Tag releases independently

### Phase 4: Features 006-007 (Sequential)
1. Feature 006 must complete first (reminders infrastructure)
2. Feature 007 builds on Feature 006
3. Both are lower priority (nice-to-have vs. must-have)

---

## Benefits of Split

### Development Velocity
- **Smaller PRs**: 200-400 lines vs. 2000+ lines
- **Faster Reviews**: 1-2 hours vs. 1-2 days
- **Quicker Feedback**: Weekly releases vs. monthly

### Risk Management
- **Isolated Changes**: Problems affect one feature, not entire system
- **Easier Rollback**: Can revert one feature without losing others
- **Incremental Testing**: Each feature fully tested before next starts

### Team Collaboration
- **Parallel Work**: Multiple features can progress simultaneously
- **Clear Ownership**: Each feature has a clear scope and owner
- **Better Estimation**: Smaller features are easier to estimate accurately

### User Value
- **Frequent Releases**: Users see progress every 1-2 weeks
- **Earlier Feedback**: Can validate features before building dependent features
- **Prioritization**: Can adjust priorities based on user feedback

---

## Lessons Learned

### What Went Wrong
1. **Scope Creep**: Original feature kept expanding without formal split
2. **Mixed Concerns**: Authentication + Deployment + UI all in one branch
3. **Large PRs**: Hard to review, slow to merge
4. **Testing Debt**: Difficult to test everything at once

### What to Do Differently
1. **Define Clear Boundaries**: Each feature spec should be ≤2 weeks of work
2. **One Concern per Feature**: Authentication OR Deployment OR UI, not all three
3. **Continuous Integration**: Merge frequently (weekly if possible)
4. **Test-First**: Write tests before implementation for clear acceptance criteria

### Updated Guidelines (Constitution Amendment)
- Maximum feature size: 2-3 weeks of effort
- Maximum PR size: 500 lines changed (excluding generated code)
- Features must have clear dependencies documented
- Features must be independently deployable
- Specs must define success criteria upfront

---

## Tracking

### Progress Dashboard
| Feature | Priority | Status | Completion | Release |
|---------|----------|--------|------------|---------|
| 001: Authentication | P0 | ✅ Done | 100% | v1.0 |
| 002: Deployment | P0 | 🔄 In Progress | 90% | v1.0.1 |
| 003: Core UI | P1 | ⏳ Planned | 0% | v1.1 |
| 004: Medication Log | P2 | ⏳ Planned | 0% | v1.2 |
| 005: INR Recording | P2 | ⏳ Planned | 0% | v1.3 |
| 006: Reminders | P3 | ⏳ Planned | 0% | v1.4 |
| 007: Scheduling | P3 | ⏳ Planned | 0% | v1.5 |

### Velocity Metrics
- **Original Estimate**: 4-6 weeks for all features
- **Actual Time (so far)**: 3 weeks for Features 001-002
- **Revised Estimate**: 16-20 weeks for all features
- **Lesson**: Original estimate was 3-4x too optimistic

---

## Next Steps

1. **Complete Feature 002** (this week)
   - Finish T051 technical debt
   - Merge to main
   - Create deployment runbook

2. **Spec Feature 003** (next week)
   - Run: `speckit specify "Create authentication-protected core UI pages for viewing medication list and INR reading history with mobile-responsive MudBlazor design"`
   - Run: `speckit tasks` to generate task breakdown
   - Review and refine spec

3. **Start Feature 003** (in 2 weeks)
   - Create branch from main
   - Implement core UI foundation
   - Focus on read-only views first

---

## Questions & Decisions

### Q: Can Features 004 and 005 be developed in parallel?
**A**: Yes, both depend only on Feature 003 and have no dependencies on each other. However, Feature 004 (Medication Logging) is higher priority for patient safety.

### Q: Why is Feature 006 (Reminders) P3 instead of P1?
**A**: While important for user experience, the app is usable without reminders. Users can manually check their medication schedule. Reminders are valuable but not critical for MVP.

### Q: Should we use feature flags for gradual rollout?
**A**: Yes. Each feature should be behind a feature flag that can be toggled per-user or per-environment. This allows:
- Testing in production with limited users
- Quick rollback without deployment
- A/B testing different implementations

### Q: How do we handle database migrations across features?
**A**: Each feature includes its own migration scripts. Migrations are forward-only and backward-compatible for at least one release cycle.

---

**Document Owner**: Development Team  
**Last Updated**: October 28, 2025  
**Next Review**: After Feature 002 completion
