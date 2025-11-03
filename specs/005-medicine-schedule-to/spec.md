# Feature Specification: Complex Medication Dosage Patterns for INR Management# Feature Specification: [FEATURE NAME]



**Feature Branch**: `005-medicine-schedule-to`  **Feature Branch**: `[###-feature-name]`  

**Created**: 2025-11-03  **Created**: [DATE]  

**Status**: Draft  **Status**: Draft  

**Input**: User description: "Medicine schedule to support complex patterns. The goal of blood thinners is to hit a specific INR, to achieve that the pattern of dosage is not always the same each day, for example my dosage had the pattern 4, 3, 3 but i was trending too low so now it is 4, 4, 3, 4, 3, 3, 4, 3, 4, 4, 4, 3, 3, 4, 3, 3 and will change again, so the schedule needs more flexibility for future planning, edit medication needs more fields to support changing it and log dose should be populated correctly based on that days medication dose"**Input**: User description: "$ARGUMENTS"



## User Scenarios & Testing *(mandatory)*## User Scenarios & Testing *(mandatory)*



### User Story 1 - Define Complex Dosage Pattern (Priority: P1)<!--

  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.

As a blood thinner user, I need to define a repeating pattern of different daily dosages (e.g., 4mg, 4mg, 3mg, 4mg, 3mg, 3mg) so that I can follow my healthcare provider's prescribed schedule to maintain my target INR range.  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,

  you should still have a viable MVP (Minimum Viable Product) that delivers value.

**Why this priority**: This is the core capability required for managing variable-dosage blood thinner regimens. Without this, users cannot accurately track their prescribed medication schedule, which is critical for medication safety and INR control.  

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.

**Independent Test**: User can create or edit a medication with a custom dosage pattern (e.g., "4, 4, 3, 4, 3, 3") that repeats, view the pattern in the medication details, and verify the system shows the correct dosage for today and future dates based on the pattern cycle.  Think of each story as a standalone slice of functionality that can be:

  - Developed independently

**Acceptance Scenarios**:  - Tested independently

  - Deployed independently

1. **Given** a user is editing a blood thinner medication requiring variable dosing, **When** they enter a dosage pattern "4, 4, 3" with a pattern unit of "mg", **Then** the system saves the pattern and displays it as "4mg, 4mg, 3mg repeating every 3 days"  - Demonstrated to users independently

-->

2. **Given** a user has defined a dosage pattern "4, 4, 3, 4, 3, 3", **When** they view today's medication schedule on Day 1 of the pattern, **Then** the system displays "Today's dose: 4mg" and shows the next 6 days with correct dosages

### User Story 1 - [Brief Title] (Priority: P1)

3. **Given** a user is on Day 7 of a 6-day pattern "4, 4, 3, 4, 3, 3", **When** they view their medication schedule, **Then** the system correctly shows Day 7 as "4mg" (Day 1 of next cycle)

[Describe this user journey in plain language]

4. **Given** a user has a medication with a 16-day pattern, **When** they navigate to any future date, **Then** the system calculates and displays the correct dosage for that date based on the pattern position

**Why this priority**: [Explain the value and why it has this priority level]

---

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

### User Story 2 - Log Dose with Auto-Population (Priority: P1)

**Acceptance Scenarios**:

As a blood thinner user, when I log that I took my medication today, the system should automatically populate the dosage field with the correct amount for today based on my defined pattern, so I can quickly confirm I took the right dose without manual lookup.

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

**Why this priority**: This is essential for accurate medication tracking and preventing dosage errors. Users need immediate confirmation they're taking the correct dose for that day in their pattern, reducing the risk of under- or over-dosing.2. **Given** [initial state], **When** [action], **Then** [expected outcome]



**Independent Test**: User opens the "Log Dose" screen, sees the dosage field pre-filled with today's pattern-based amount (e.g., "4mg"), confirms or adjusts if needed, saves the log, and verifies the log entry shows the correct date, time, and dosage.---



**Acceptance Scenarios**:### User Story 2 - [Brief Title] (Priority: P2)



1. **Given** a user has a medication with pattern "4, 3, 3" and today is Day 2 of the pattern, **When** they open the log dose screen, **Then** the dosage field is pre-filled with "3mg" and shows "Expected: 3mg (Day 2 of pattern)"[Describe this user journey in plain language]



2. **Given** a user is pre-filling today's expected dose of "4mg", **When** they manually change it to "3mg" before saving, **Then** the system saves the log with "3mg" and flags it as "Dose adjusted from expected 4mg"**Why this priority**: [Explain the value and why it has this priority level]



3. **Given** a user logs a dose, **When** the log is saved, **Then** the system records the date, time, actual dosage taken, expected dosage from pattern, and any variance**Independent Test**: [Describe how this can be tested independently]



4. **Given** a user views their medication log history, **When** they see entries with dosage variances, **Then** entries are visually indicated (e.g., with a warning icon or highlight) showing "Expected: 4mg, Taken: 3mg"**Acceptance Scenarios**:



---1. **Given** [initial state], **When** [action], **Then** [expected outcome]



### User Story 3 - Modify Active Dosage Pattern (Priority: P2)---



As a blood thinner user whose INR levels require dosage adjustment, I need to update my medication's dosage pattern effective from a specific date, so that my future schedule reflects my healthcare provider's new prescription while preserving historical accuracy.### User Story 3 - [Brief Title] (Priority: P3)



**Why this priority**: INR management requires frequent dosage adjustments based on blood test results. Users must be able to change their pattern when prescribed a new regimen, while maintaining accurate historical records for medical review.[Describe this user journey in plain language]



**Independent Test**: User edits a medication's dosage pattern from "4, 3, 3" to "4, 4, 3, 4, 3, 3", sets the effective date to "Tomorrow", saves the change, and verifies that today's schedule still shows the old pattern while tomorrow and future dates show the new pattern.**Why this priority**: [Explain the value and why it has this priority level]



**Acceptance Scenarios**:**Independent Test**: [Describe how this can be tested independently]



1. **Given** a user has an active medication with pattern "4, 3, 3", **When** they edit the pattern to "4, 4, 3, 4, 3, 3" effective tomorrow, **Then** the system saves the new pattern with tomorrow's date and keeps the old pattern for historical logs**Acceptance Scenarios**:



2. **Given** a user changes a dosage pattern, **When** they view the medication history, **Then** the system displays "Pattern changed on [date]: Old '4, 3, 3' → New '4, 4, 3, 4, 3, 3'"1. **Given** [initial state], **When** [action], **Then** [expected outcome]



3. **Given** a user has logged doses under an old pattern, **When** they change the pattern and view past logs, **Then** historical log entries remain unchanged and are not recalculated with the new pattern---



4. **Given** a user sets a pattern change effective "Today", **When** they view today's expected dose, **Then** the system uses the new pattern starting from today[Add more user stories as needed, each with an assigned priority]



---### Edge Cases



### User Story 4 - View Future Dosage Calendar (Priority: P2)<!--

  ACTION REQUIRED: The content in this section represents placeholders.

As a blood thinner user, I want to view a calendar showing my scheduled dosages for the next 2-4 weeks, so I can plan ahead, prepare medication organizers, and know what to expect when I get my next INR test.  Fill them out with the right edge cases.

-->

**Why this priority**: Users need visibility into their upcoming medication schedule for planning purposes, preparing weekly pill organizers, and understanding their regimen before INR tests. This reduces anxiety and improves medication adherence.

- What happens when [boundary condition]?

**Independent Test**: User navigates to a medication's schedule view, sees a calendar or list showing the next 14-28 days with each day's expected dosage based on the pattern, and can identify which day of the pattern each date represents.- How does system handle [error scenario]?



**Acceptance Scenarios**:## Requirements *(mandatory)*



1. **Given** a user has a medication with a 6-day pattern, **When** they view the 14-day future schedule, **Then** the system displays each date with its dosage and indicates pattern day number (e.g., "Nov 3: 4mg (Day 1)", "Nov 4: 4mg (Day 2)", "Nov 5: 3mg (Day 3)")<!--

  ACTION REQUIRED: The content in this section represents placeholders.

2. **Given** a user has an upcoming pattern change on Day 10, **When** they view the future schedule, **Then** the system clearly indicates "Pattern changes on Day 10" and shows the new pattern's dosages from that date forward  Fill them out with the right functional requirements.

-->

3. **Given** a user views the future schedule, **When** they select a specific date, **Then** the system shows detailed information: date, expected dosage, pattern day number, and any scheduled INR tests on that date

### Functional Requirements

4. **Given** a user has a long pattern (16 days), **When** they view a 28-day schedule, **Then** the system correctly calculates and displays dosages across multiple pattern cycles

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]

---- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  

- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]

### User Story 5 - Validate Pattern Entry (Priority: P3)- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]

- **FR-005**: System MUST [behavior, e.g., "log all security events"]

As a blood thinner user entering a dosage pattern, I need the system to validate my input and provide helpful feedback, so I can be confident the pattern is correct and will calculate properly.

*Example of marking unclear requirements:*

**Why this priority**: Pattern entry errors could lead to incorrect dosages being suggested. Validation ensures data integrity and prevents user errors, though the core functionality works without extensive validation.

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]

**Independent Test**: User attempts various pattern inputs (valid, invalid, edge cases) and verifies the system accepts valid patterns, rejects invalid ones with clear error messages, and provides guidance on correct format.- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]



**Acceptance Scenarios**:### Key Entities *(include if feature involves data)*



1. **Given** a user enters a pattern "4, 3, 3", **When** they save, **Then** the system validates it as a valid pattern and saves successfully- **[Entity 1]**: [What it represents, key attributes without implementation]

- **[Entity 2]**: [What it represents, relationships to other entities]

2. **Given** a user enters a pattern "4, abc, 3", **When** they attempt to save, **Then** the system shows an error "Pattern contains invalid value 'abc'. Use numeric values only (e.g., 4, 3.5, 3)"

## Success Criteria *(mandatory)*

3. **Given** a user enters a pattern "4", **When** they save, **Then** the system asks "This pattern has only one dosage. Did you mean a fixed daily dose of 4mg instead of a pattern?"

<!--

4. **Given** a user enters a very long pattern (20+ values), **When** they save, **Then** the system warns "Long pattern detected (20 days). Please verify this is correct." and requires confirmation  ACTION REQUIRED: Define measurable success criteria.

  These must be technology-agnostic and measurable.

5. **Given** a user enters dosages outside the safe range for their medication type (e.g., 30mg Warfarin), **When** they save, **Then** the system warns "This dosage (30mg) is above the typical maximum (20mg). Confirm this is prescribed." per existing medication safety rules-->



---### Measurable Outcomes



### Edge Cases- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]

- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]

- **What happens when a user has no pattern defined (legacy medication)?** System defaults to a single fixed daily dose and allows user to optionally convert to a pattern-based schedule- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]

- **What happens when a pattern is changed mid-cycle?** System resets to Day 1 of the new pattern starting from the effective date, ensuring clear transition- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]

- **What happens when a user logs a dose for a past date with a different active pattern?** System uses the pattern that was active on that past date, not the current pattern
- **What happens when a pattern has decimal dosages (e.g., 3.5mg)?** System supports decimal values with up to 2 decimal places for precision
- **What happens when the medication frequency is not daily?** Pattern applies to scheduled medication days only (e.g., if frequency is "Every other day", pattern cycles through scheduled days only)
- **What happens when a user deletes a medication with pattern history?** System soft-deletes but retains historical pattern data for medical record integrity
- **What happens when calculating dosages far in the future (e.g., 1 year)?** System calculates pattern position using modulo arithmetic (day_number % pattern_length) to ensure accuracy regardless of date range

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to define a medication dosage pattern as an ordered sequence of numeric values representing dosages for consecutive days (e.g., "4, 4, 3, 4, 3, 3")
- **FR-002**: System MUST support dosage patterns of minimum length 2 and maximum length 365 days
- **FR-003**: System MUST support dosage values with up to 2 decimal places (e.g., 3.5mg, 2.75mg)
- **FR-004**: System MUST associate each dosage pattern with a dosage unit (mg, mcg, units, mL, etc.) from the medication's configured unit
- **FR-005**: System MUST calculate the correct expected dosage for any given date by determining the position within the pattern cycle using: `pattern_position = (days_since_start % pattern_length) + 1`
- **FR-006**: System MUST display the current pattern day number and cycle information when showing today's expected dosage (e.g., "Day 3 of 6-day pattern, Cycle 5")
- **FR-007**: System MUST automatically pre-fill the dosage field in the "Log Dose" screen with today's expected dosage based on the active pattern
- **FR-008**: System MUST allow users to override the pre-filled dosage when logging a dose (for when actual dose differs from expected)
- **FR-009**: System MUST record both the expected dosage (from pattern) and actual dosage (logged) for each dose log entry
- **FR-010**: System MUST visually indicate dose log entries where actual dosage differs from expected dosage
- **FR-011**: System MUST support modifying a medication's dosage pattern with an effective date for when the new pattern begins
- **FR-012**: System MUST preserve historical dosage patterns when a pattern is changed, maintaining accurate records of what pattern was active on any past date
- **FR-013**: System MUST use the historically active pattern (not current pattern) when displaying or editing past dose logs
- **FR-014**: System MUST display a future dosage schedule showing expected dosages for the next 14-28 days based on the current or upcoming pattern
- **FR-015**: System MUST indicate pattern change dates in the future schedule view when a pattern change is scheduled
- **FR-016**: System MUST validate pattern input to ensure all values are numeric and within the medication's safe dosage range per existing safety rules
- **FR-017**: System MUST support converting a fixed-dose medication to a pattern-based medication and vice versa
- **FR-018**: System MUST handle the special case of frequency not being daily by applying the pattern only to scheduled medication days (e.g., for "Every other day" frequency, Day 1 of pattern applies to first scheduled day, Day 2 to second scheduled day, etc.)
- **FR-019**: System MUST display pattern information in the medication details view, showing the complete pattern sequence and current position
- **FR-020**: System MUST support exporting medication history including pattern changes for medical records and healthcare provider review

### Key Entities

- **MedicationDosagePattern**: Represents a specific dosage pattern configuration
  - Pattern sequence (ordered list of dosage values)
  - Pattern unit (mg, mcg, units, mL)
  - Start date (effective date)
  - End date (when replaced by a new pattern, null if currently active)
  - Medication reference
  - Pattern length (calculated from sequence)
  
- **Medication** (enhanced): Existing entity with new pattern-related fields
  - Current active pattern reference
  - Pattern history collection
  - Pattern start date (when the currently active pattern began)
  - Display mode (fixed dose vs. pattern-based)
  
- **MedicationLog** (enhanced): Existing entity with new pattern-tracking fields
  - Expected dosage (from active pattern on log date)
  - Actual dosage (what user logged)
  - Pattern day number (position within pattern cycle when dose was taken)
  - Pattern reference (which pattern was active on this date)
  - Variance flag (indicates if actual differs from expected)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can successfully define and save dosage patterns of various lengths (2-365 days) with validation feedback within 30 seconds
- **SC-002**: The "Log Dose" screen pre-fills the correct expected dosage based on the active pattern with 100% accuracy for any date
- **SC-003**: Users can modify an active dosage pattern and verify the change takes effect on the specified date within 10 seconds
- **SC-004**: The system correctly calculates dosages for any date up to 2 years in the future without performance degradation (< 500ms response time)
- **SC-005**: 95% of users successfully complete pattern entry on first attempt without validation errors (measured via successful saves vs. validation errors)
- **SC-006**: Users can view a 14-day future schedule and identify the correct dosage for any specific date in under 15 seconds
- **SC-007**: Historical dose logs maintain 100% accuracy of expected dosage calculations even after pattern changes (expected dosage uses historically active pattern, not current)
- **SC-008**: Dose log entries with variance between expected and actual dosage are visually identifiable within 2 seconds of viewing the log list
- **SC-009**: Users report 80%+ confidence in taking the correct daily dosage after implementing pattern-based scheduling (measured via user feedback survey)
- **SC-010**: System handles pattern cycles correctly across multiple years without calculation drift (pattern day for any date matches manual calculation)

## Assumptions *(documented)*

- Users typically have dosage patterns ranging from 2 to 16 days in length (based on user example: "4, 4, 3, 4, 3, 3, 4, 3, 4, 4, 4, 3, 3, 4, 3, 3" = 16 days)
- Pattern changes happen infrequently (every few weeks or months) based on INR test results, not daily
- Users follow a pattern continuously once established until their next healthcare provider visit
- The dosage unit (mg, mcg, etc.) remains consistent within a single pattern
- Blood thinner medications requiring pattern-based dosing are typically taken at the same time each day
- Users have access to their medication schedule view daily to confirm dosages
- Healthcare providers communicate pattern changes clearly (e.g., "4, 4, 3 repeating")
- Users are responsible for following their prescribed pattern; the system provides scheduling support but does not replace medical advice
- Pattern history is retained indefinitely for medical record purposes (soft delete only)
- Most users will use the system's calendar view for reference, not memorize long patterns

## Out of Scope *(clarified boundaries)*

- **Automatic INR-based dosage adjustment**: System displays patterns but does not algorithmically calculate new patterns based on INR test results (this requires medical expertise)
- **Pattern templates or suggestions**: System does not suggest common patterns or provide pattern recommendations
- **Multi-drug pattern coordination**: This feature focuses on individual medication patterns, not coordinating patterns across multiple medications
- **Pattern sharing between users**: Each user's patterns are independent; no community pattern library
- **Medication interaction checking with pattern-based dosing**: Standard medication interaction checking continues to work but is not pattern-aware
- **Healthcare provider portal integration**: Pattern changes are user-entered; no direct integration with EHR systems for automatic pattern updates
- **Smart device integration**: No automatic dose reminders via smartwatches or IoT medication dispensers (existing reminder system continues to work)
- **Insurance or prescription management**: Pattern-based dosing does not interact with refill tracking or prescription systems

## Dependencies *(identified)*

- Existing Medication entity and CRUD operations
- Existing MedicationLog entity and logging functionality  
- Existing medication safety validation rules (dosage limits, INR monitoring requirements)
- Date/time handling infrastructure for pattern position calculations
- UI components for list input (entering pattern sequences)
- Calendar or schedule view component for displaying future dosages

## Notes

### Medical Safety Considerations

- All existing medication safety rules continue to apply (e.g., Warfarin 20mg daily maximum still enforced per pattern value)
- Pattern validation must check each dosage value in the pattern against medication-specific safety limits
- Variance warnings (expected vs. actual dose) help users identify potential dosing errors
- Pattern change history is critical for medical record accuracy and cannot be deleted

### Implementation Hints (for planning phase only)

- Pattern storage: Consider storing as JSON array `[4.0, 4.0, 3.0, 4.0, 3.0, 3.0]` for flexibility
- Pattern position calculation: Use modulo arithmetic to handle any date efficiently
- Pattern history: Use temporal data pattern (start_date, end_date) for time-traveling queries
- Migration path: Existing medications can be treated as single-value patterns `[5.0]` for backward compatibility
