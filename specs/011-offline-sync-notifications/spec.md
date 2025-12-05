 # Feature Specification: Offline Sync & Notifications
 
 **Feature Branch**: `011-offline-sync-notifications`  
 **Created**: 2025-12-04  
 **Status**: Draft  
 **Input**: User description: "Mobile app users should have an offline sync of their medicine data and notifications to remind the user to take their medicine"
 
 ## User Scenarios & Testing *(mandatory)*
 
 ### User Story 1 - Offline read/write & automatic sync (Priority: P1)
 
 As a mobile user, I want my medication list and logs to be available while offline so I can view my dosing information even without network access.
 
 **Why this priority**: Core reliability and safety — users must be able to rely on the app when away from network coverage. Offline persistence is required before reminders and sync features are useful.
 
 **Independent Test**:
 - Turn device/network to airplane mode. Observe that local data can still be viewed.
 
 **Acceptance Scenarios**:
 1. **Given** the device is offline and the user opens the app, **When** the user views medication list, **Then** the app shows the last known medication list from local storage.

2. **Given** the app is opened while online and the background sync job has not recently completed, **When** the app starts, **Then** the app checks connectivity and triggers a foreground refresh to align data immediately.
 
 ---
 
 ### User Story 2 - Reliable local reminders (Priority: P1)
 
 As a mobile user, I want to receive local notifications that remind me to take my medicine at scheduled times, even when the app is not running and when the device is offline.
 
 **Why this priority**: Medication adherence is a critical user need; reminders must work reliably without network dependency.
 
 **Independent Test**:
 - Configure a medication schedule for the next minute, lock the device or background the app, and confirm a local notification appears at the scheduled time.
 
 **Acceptance Scenarios**:
 1. **Given** a scheduled reminder at time T, **When** the device reaches time T, **Then** a local notification is delivered to the system notification tray.
 2. **Given** the user has disabled notifications for the app, **When** a reminder triggers, **Then** the app does not attempt to post a notification and logs or records that permission is missing.
 
 ---
 
 ### User Story 3 - Manual sync and visibility (Priority: P2)
 
 As a user, I want a manual "Sync Now" action and a visible indicator of sync status so I can force syncs and see whether my data is up to date.
 
 **Why this priority**: Provides control for cases where automatic sync may be delayed or when the user wants immediate confirmation.
 
 **Independent Test**:
 - Trigger "Sync Now" while online and verify the network calls complete and the UI shows a successful sync state; trigger while offline and verify the app queues the request and shows a pending state.
 
 **Acceptance Scenarios**:
 1. **Given** the app is online, **When** the user taps "Sync Now", **Then** the app refreshes the local store from the server.
 2. **Given** the app is offline, **When** the user taps "Sync Now", **Then** the button is disabled and shows an offline indicator.

3. **Given** the app is backgrounded, **When** a periodic background sync runs and network is available, **Then** the app refreshes local data from the server so the next foreground view is aligned.
 
 ---
 
 ### Edge Cases
 
 - Device never regains network for an extended period (e.g., weeks) — access will be lost due to token and session timeout.   
 - Storage constrained (low disk) — app must surface an error and refuse new local writes until space is freed.  
 - Notification permission denied — app must not crash; it should surface guidance and keep reminders in a retry queue.
 
 ## Requirements *(mandatory)*
 
 ### Functional Requirements

 - **FR-001**: The app MUST persist medication lists and medication logs locally so they are available when offline.
 - **FR-002**: The app MUST provide a reliable local notification mechanism to remind users at scheduled times regardless of network state.
 - **FR-003**: The app MUST offer a manual "Sync Now" action and a visible sync status indicator.
 - **FR-004**: The app MUST record sync outcomes (success/failure) for diagnostics.
 - **FR-005**: The app MUST respect user notification permissions and provide a fallback if notifications are blocked.
 - **FR-006**: Data is read-only in this phase: the server is the single source of truth and the client will only read and refresh canonical data. Client-side create/update/delete is out of scope for this feature and will be addressed in a future phase.
 - **FR-007**: The app MUST perform automatic background synchronization on a configurable periodic interval and perform a foreground refresh on app start when needed (if background sync hasn't recently aligned data).
 - **FR-007**: The app MUST perform automatic background synchronization on a configurable periodic interval and perform a foreground refresh on app start when needed (if background sync hasn't recently aligned data). The default interval will be 1 hour; when the device is charging and connected to Wi‑Fi the interval may reduce to 15 minutes to provide faster freshness.
 
 ### Key Entities *(include if feature involves data)*
 
 - **Medication**: identifier, display name, dosage instructions, schedule pattern reference, enabled/disabled flag.  
 - **MedicationDosagePattern**: sequence of dosing values / relative schedule entries and temporal validity window.  
 - **MedicationLog**: timestamp, medication id, expected dosage, actual dosage, createdBy (device/user), synced flag, conflict metadata.  
 - **MedicationLog**: timestamp, medication id, expected dosage, actual dosage, createdBy (device/user).
<!-- SyncQueueItem intentionally removed for this phase; client is read-only. -->
 
 ## Success Criteria *(mandatory)*
 
 ### Measurable Outcomes
 
 - **SC-001**: Offline availability: In airplane-mode tests, 100% of recently-viewed medication list screens load from local storage within 2 seconds.  
 - **SC-002**: Sync reliability: When network returns within 24 hours, local data is updated from the server.  
 - **SC-003**: Notification delivery: 95% of scheduled reminders for active medications are presented to the user within 15 minutes of their scheduled time on supported devices.  
 - **SC-004**: User control: Manual "Sync Now" completes (success or recorded failure) within 60 seconds while online for datasets of typical size (<=100 items).  
 
 ### Assumptions
 
 - The backend API supports idempotent create/update/delete operations and returns canonical state on query.  
 - The app has access to a local persistent store that survives restarts and reboots.  
 - Platform local-notification APIs are available on target devices (Android/iOS/Windows) and the app will prompt for permission where required.
 
 **Spec Ready**: Draft complete and testable; see checklist for quality validation.

## Clarifications

### Session 2025-12-05

- Q: How should conflicts be handled when local edits and server state differ? → A: Server-wins / read-only in this phase. The mobile client is read-only; the server is the canonical source of truth. Conflict resolution will be addressed in a future feature when client-side edits are reintroduced.

- Q: Which sync trigger strategy should be used? → A: Combined (foreground refresh on app open + periodic background sync + manual "Sync Now"). The existing sync implementation already follows this pattern and will be extended so medicine and INR logs sync in alignment.
