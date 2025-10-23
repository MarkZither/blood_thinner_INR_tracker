# Feature Specification: Blood Thinner Medication & INR Tracker

**Feature Branch**: `feature/blood-thinner-medication-tracker`  
**Created**: 2025-10-15  
**Status**: Draft  
**Input**: User description: "Build an application that can help to remind me to take my blood thinners on time, daily at the same time each day with a 12 hour maximum error window after which it should warn against taking up until next dose is due, log the dosage and remind me do my blood test on time, first thing in the morning on a schedule I can configure, and log the INR level. I should be able to log and the values should be available across my devices. This is not a medically approved app and it should not be considered to be giving any medical advice."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cross-Device Account Setup & Data Sync (Priority: P1)

As a blood thinner patient, I need to create an account and have my medication and INR data synchronized across all my devices so I can access my health information anywhere and never lose my tracking history.

**Why this priority**: Foundation requirement - without account management and data sync, users cannot access their data across devices, making the app less reliable for daily medication management.

**Independent Test**: Can be fully tested by creating an account on one device, logging data, then accessing the same data from a different device. Delivers core value of persistent, accessible health tracking.

**Acceptance Scenarios**:

1. **Given** I am a new user on web, **When** I click "Sign in with Microsoft/Google", **Then** I am redirected to the provider's OAuth2 consent page, authenticate, and return to the app logged in (web redirect flow)
2. **Given** I am a new user on mobile, **When** I tap "Sign in with Microsoft/Google", **Then** the platform-native authentication opens, I authenticate, and the app exchanges the ID token for JWT access tokens (mobile ID token exchange flow - see docs/OAUTH_FLOW_REFERENCE.md)
3. **Given** I have an account, **When** I log medication data on my phone, **Then** I can see the same data when I access the app on my tablet
4. **Given** I am logged in on multiple devices, **When** I update data on one device, **Then** the changes appear on other devices within 30 seconds
5. **Given** time changes due to daylight saving or timezone travel, **When** the system calculates my next medication reminder, **Then** it adjusts to maintain the same local time in my current timezone (see T019a)

---

### User Story 2 - Daily Medication Reminders & Logging (Priority: P2)

As a blood thinner patient, I need to receive daily reminders at my scheduled time to take my medication and log the dosage taken so I can maintain consistent medication adherence and track my compliance.

**Why this priority**: Core safety functionality - consistent medication timing is critical for blood thinner effectiveness and patient safety.

**Independent Test**: Can be fully tested by setting a reminder time, receiving the notification, and logging a dose. Delivers immediate value for medication compliance tracking.

**Acceptance Scenarios**:

1. **Given** I have set my daily medication time to 7:00 PM, **When** it's 7:00 PM, **Then** I receive a persistent notification to take my medication
2. **Given** I receive a medication reminder, **When** I take my medication and enter the dosage, **Then** the reminder is dismissed and the dose is logged with timestamp
3. **Given** I haven't taken my medication within 12 hours of the scheduled time, **When** I try to log a dose, **Then** I see a warning advising against taking the medication until the next scheduled dose

---

### User Story 3 - Configurable INR Test Reminders & Logging (Priority: P3)

As a blood thinner patient, I need to set up a custom schedule for INR blood test reminders and log my INR levels so I can track my blood clotting levels according to my doctor's prescribed testing frequency.

**Why this priority**: Important medical monitoring but frequency varies by patient and condition - less urgent than daily medication but essential for long-term management.

**Independent Test**: Can be tested by configuring an INR schedule, receiving morning reminders on scheduled days, and logging INR values. Delivers value for blood level monitoring and trend tracking.

**Acceptance Scenarios**:

1. **Given** I want to configure my INR testing schedule, **When** I access the settings, **Then** I can set testing frequency (daily, weekly, bi-weekly, monthly, or custom days)
2. **Given** today is a scheduled INR test day, **When** it's morning (between 6-9 AM), **Then** I receive a notification to do my blood test
3. **Given** I complete my INR test, **When** I enter my INR level, **Then** the value is logged with the test date and the reminder is dismissed

---

### User Story 4 - Historical Data Visualization & Trends (Priority: P4)

As a blood thinner patient, I need to view my medication dosage history and INR levels over time in charts so I can track trends, identify patterns, and share comprehensive data with my healthcare provider.

**Why this priority**: Analytical feature that provides insights but not critical for day-to-day medication compliance - valuable for medical consultations and trend analysis.

**Independent Test**: Can be tested by logging several doses and INR readings over time, then viewing the historical charts. Delivers value for long-term health monitoring and medical consultations.

**Acceptance Scenarios**:

1. **Given** I have logged medication doses for several days, **When** I view the medication history, **Then** I see a chart showing my dosage over time
2. **Given** I have recorded INR levels over several tests, **When** I view the INR history, **Then** I see a line chart showing my INR trends with target ranges
3. **Given** I want to share data with my doctor, **When** I select a date range, **Then** I can export or share a summary of my medication and INR data

---

### User Story 5 - Missed Dose Recovery & Safety Warnings (Priority: P5)

As a blood thinner patient, I need clear guidance when I miss doses or am outside the safe dosing window so I can make informed decisions about medication timing and avoid dangerous situations.

**Why this priority**: Safety feature that handles edge cases - important for patient safety but less frequent than daily medication management.

**Independent Test**: Can be tested by simulating missed doses and delayed medication attempts. Delivers safety value by preventing dangerous dosing decisions.

**Acceptance Scenarios**:

1. **Given** I missed my scheduled dose by more than 12 hours, **When** I try to log a late dose, **Then** I see a warning recommending I wait for the next scheduled dose
2. **Given** I haven't logged a dose for my scheduled time, **When** the next day's reminder appears, **Then** the app notes the missed dose in my history
3. **Given** I want to take medication outside my normal schedule, **When** I manually try to log a dose, **Then** the app calculates time since last dose and provides appropriate guidance

---

### Edge Cases

- What happens when user changes time zones while traveling? → **Handled by T019a**: Medication schedules maintain local time in current timezone
- How does the system handle daylight saving time transitions? → **Handled by T019a**: DST transitions preserve medication reminder local times
- What occurs when user tries to log multiple doses for the same day? → **To be specified**: Business rule needed for duplicate dose detection
- How does the app behave when device notifications are disabled? → **Handled by T024a**: Fallback UI warnings when notifications disabled
- What happens if user enters an invalid or dangerous INR value (e.g., negative numbers, extremely high values)? → **Handled by T029a**: Validation enforces 0.5-8.0 range, flags outliers <1.5 or >4.5
- How should the app handle very long periods without logging (weeks or months)? → **To be specified**: Re-engagement strategy and data gap handling needed

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST require OAuth2 authentication (via Azure AD or Google) before accessing any medication or INR data. NO password-based authentication permitted. Web applications use OAuth2 redirect flow; mobile applications use platform-native OAuth with ID token exchange.
- **FR-002**: System MUST send daily notifications at user-configured times for medication reminders with 99.9% delivery reliability (tracked per T044a notification monitoring)
- **FR-003**: System MUST allow users to configure INR test reminder frequency (daily, weekly, bi-weekly, monthly, custom)
- **FR-004**: System MUST log medication doses with timestamps and associate with authenticated user
- **FR-005**: System MUST log INR test results with test dates and associate with authenticated user
- **FR-006**: System MUST synchronize user data across multiple devices within 30 seconds
- **FR-007**: System MUST display a warning (not a hard block) advising against taking medication more than 12 hours after scheduled time, recommending user wait for next scheduled dose (see T025 for UI implementation)
- **FR-008**: System MUST display historical medication doses in chart format
- **FR-009**: System MUST display historical INR levels in chart format with trend indicators
- **FR-010**: System MUST validate INR value ranges (0.5-8.0) and flag outlier values (INR <1.5 or >4.5) for healthcare provider review (see T029a for validation logic)
- **FR-011**: System MUST track missed doses and display them in medication history
- **FR-012**: System MUST allow users to export medication and INR data for sharing with healthcare providers
- **FR-013**: System MUST display medical disclaimer prominently on every screen showing health data or recommendations (all platforms: Web, Mobile, Console - see T014 for implementation tracking)
- **FR-014**: System MUST prevent accidental dismissal of medication reminders by requiring explicit confirmation dialog before dismissing without logging dose (see T024)
- **FR-015**: System MUST maintain user sessions across app restarts using secure OAuth2 refresh token storage to minimize login friction (see T017a)
- **FR-016**: System MUST handle timezone changes (travel) and daylight saving time transitions by maintaining medication schedules in user's current local timezone (see T019a for DST handling implementation)

### Key Entities *(include if feature involves data)*

- **User Account**: OAuth2-authenticated individual (via Azure AD or Google) with ExternalUserId, AuthProvider (Google/AzureAD), email, device registrations, timezone settings. NO password field - authentication handled entirely through OAuth2 providers.
- **Medication Schedule**: User-specific daily reminder time, dosage amount, timezone-aware scheduling
- **Medication Log** (MedicationLog entity): Recorded dose entries with timestamp, dosage amount, user association, missed dose indicators
- **INR Schedule** (INRSchedule entity): User-configured testing frequency (daily/weekly/biweekly/monthly/custom), next test date calculation
- **INR Test** (INRTest entity - formerly "INR Log"): Recorded INR values with test date, result value (validated 0.5-8.0 range), user association, trend calculations, outlier flags
- **Device Registration**: User's registered devices for cross-platform data synchronization
- **Notification Settings**: User preferences for reminder timing, notification types, snooze options

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can set up their medication schedule and receive their first reminder within 5 minutes of account creation
- **SC-002**: 95% of medication reminders result in successful dose logging within 2 hours of notification
- **SC-003**: Data synchronization completes across devices within 30 seconds of logging new information
- **SC-004**: Users can configure INR testing schedules and see updated reminder dates within 1 minute
- **SC-005**: Historical charts display complete data for users with 7+ days of logged information within 3 seconds
- **SC-006**: Safety warnings for missed doses or late medication attempts appear 100% of the time when conditions are met
- **SC-007**: Users can export their complete medication and INR history in under 10 seconds
- **SC-008**: App maintains 99.9% uptime for critical reminder functionality during business hours
- **SC-009**: New users can complete account setup and log their first medication dose within 3 minutes
- **SC-010**: Medical disclaimer is displayed prominently on every screen that shows health data or recommendations

## Assumptions

- Users have smartphones or tablets capable of receiving push notifications
- Users have reliable internet connectivity for data synchronization (offline functionality not required in MVP)
- Users are literate and capable of reading English text and numbers
- Users understand basic concepts of medication dosing and INR testing from their healthcare provider
- Users will have INR values provided by their healthcare provider or home testing device
- Users take the same medication dosage daily (variable dosing not supported in MVP)
- Standard notification permissions will be granted by users for reminder functionality
