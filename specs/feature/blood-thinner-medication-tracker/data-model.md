# Data Model: Blood Thinner Medication & INR Tracker

**Created**: 2025-10-15  
**Purpose**: Entity definitions, relationships, and data architecture

---

## Core Entities

### User
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public string TimeZone { get; set; }
    
    // Navigation Properties
    public ICollection<Medication> Medications { get; set; }
    public ICollection<INRTest> INRTests { get; set; }
    public ICollection<UserDevice> Devices { get; set; }
    public UserPreferences Preferences { get; set; }
}
```

### Medication
```csharp
public class Medication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }           // e.g., "Warfarin", "Apixaban"
    public decimal DosageAmount { get; set; }  // e.g., 5.0
    public string DosageUnit { get; set; }     // e.g., "mg"
    public string Instructions { get; set; }   // e.g., "Take with food"
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DiscontinuedAt { get; set; }
    
    // Navigation Properties  
    public User User { get; set; }
    public ICollection<MedicationSchedule> Schedules { get; set; }
    public ICollection<MedicationLog> Logs { get; set; }
}
```

### MedicationSchedule  
```csharp
public class MedicationSchedule
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }
    public TimeOnly ScheduledTime { get; set; }    // Daily time (e.g., 8:00 AM)
    public string TimeZoneId { get; set; } = "UTC"; // IANA timezone (e.g., "America/New_York")
    public DayOfWeek[] ScheduledDays { get; set; }  // JSON array of days
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    
    // Reminder Settings
    public int ReminderMinutesBefore { get; set; } = 15;
    public bool EnableReminders { get; set; } = true;
    public int MaxReminderAttempts { get; set; } = 3;
    
    // Navigation Properties
    public Medication Medication { get; set; }
    public ICollection<MedicationLog> Logs { get; set; }
}
```

### MedicationLog
```csharp
public class MedicationLog  
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }
    public Guid? ScheduleId { get; set; }       // Null for unscheduled doses
    public DateTimeOffset ScheduledDateTime { get; set; }
    public DateTimeOffset? TakenDateTime { get; set; }
    public MedicationLogStatus Status { get; set; }
    public string Notes { get; set; }
    public bool WasReminded { get; set; }
    public int ReminderAttempts { get; set; }
    
    // Metadata
    public string DeviceId { get; set; }        // Which device logged the entry
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    
    // Navigation Properties
    public Medication Medication { get; set; }
    public MedicationSchedule Schedule { get; set; }
}

public enum MedicationLogStatus
{
    Scheduled = 0,    // Reminder created, not yet taken
    Taken = 1,        // Confirmed taken by user
    Missed = 2,       // User confirmed missed dose
    Skipped = 3,      // User intentionally skipped
    Unknown = 4       // No user response after reminders
}
```

### INRTest
```csharp
public class INRTest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal INRValue { get; set; }       // e.g., 2.5
    public decimal? TargetINRMin { get; set; }  // e.g., 2.0  
    public decimal? TargetINRMax { get; set; }  // e.g., 3.0
    public DateTimeOffset TestDate { get; set; }
    public string TestLocation { get; set; }    // e.g., "Home", "Lab", "Doctor's Office"
    public string Notes { get; set; }
    public bool IsInRange { get; set; }         // Calculated field
    
    // Metadata
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    
    // Navigation Properties
    public User User { get; set; }
}
```

### INRSchedule
```csharp
public class INRSchedule
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int FrequencyDays { get; set; }      // e.g., 7 for weekly, 14 for bi-weekly
    public TimeOnly PreferredTime { get; set; } // e.g., 9:00 AM
    public DateOnly NextTestDate { get; set; }
    public bool IsActive { get; set; }
    
    // Reminder Settings
    public bool EnableReminders { get; set; } = true;
    public int ReminderDaysBefore { get; set; } = 1;
    
    // Metadata
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    
    // Navigation Properties
    public User User { get; set; }
}
```

---

## Supporting Entities

### UserDevice
```csharp
public class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }        // Platform-specific device identifier
    public string DeviceName { get; set; }      // e.g., "iPhone 15", "Chrome Browser"  
    public string Platform { get; set; }        // e.g., "iOS", "Android", "Web", "Console"
    public string PushToken { get; set; }       // For notifications
    public DateTimeOffset LastSyncAt { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation Properties
    public User User { get; set; }
}
```

### UserPreferences
```csharp
public class UserPreferences  
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Notification Preferences
    public bool EnablePushNotifications { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = false;
    public bool EnableSMSNotifications { get; set; } = false;
    public TimeOnly QuietHoursStart { get; set; } = new(22, 0);  // 10:00 PM
    public TimeOnly QuietHoursEnd { get; set; } = new(8, 0);     // 8:00 AM
    
    // Display Preferences
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public string TimeFormat { get; set; } = "12h";             // "12h" or "24h"
    public string Theme { get; set; } = "System";               // "Light", "Dark", "System"
    
    // Safety Preferences
    public int MedicationSafetyWindowHours { get; set; } = 12;  // 12-hour safety window
    public bool RequireConfirmationForSkip { get; set; } = true;
    public bool ShowINRTrends { get; set; } = true;
    
    // Data & Privacy
    public bool AllowAnalytics { get; set; } = false;
    public bool AllowDataExport { get; set; } = true;
    public int DataRetentionMonths { get; set; } = 24;
    
    // Navigation Properties
    public User User { get; set; }
}
```

### AuditLog
```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; }     // e.g., "Medication", "INRTest"
    public string EntityId { get; set; }       // ID of the entity being audited
    public string Action { get; set; }         // e.g., "Created", "Updated", "Deleted"
    public string OldValues { get; set; }      // JSON of previous values
    public string NewValues { get; set; }      // JSON of new values  
    public string DeviceId { get; set; }       // Which device made the change
    public string IPAddress { get; set; }      // For web/API access
    public DateTimeOffset Timestamp { get; set; }
    
    // Navigation Properties
    public User User { get; set; }
}
```

### SyncMetadata
```csharp
public class SyncMetadata
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }
    public string EntityType { get; set; }     // e.g., "MedicationLog", "INRTest"
    public string EntityId { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string ChangeHash { get; set; }     // For conflict detection
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }
    
    // Navigation Properties  
    public User User { get; set; }
}
```

---

## Data Relationships

### Entity Relationship Diagram (Conceptual)

```
User (1) ────────────── (*) Medication
 │                           │
 │                           │
 │                           └── (*) MedicationSchedule ── (*) MedicationLog
 │                           
 ├── (*) INRTest
 ├── (1) INRSchedule  
 ├── (*) UserDevice
 ├── (1) UserPreferences
 ├── (*) AuditLog
 └── (*) SyncMetadata
```

### Key Constraints

**User ↔ Medication**: One-to-Many  
- Each medication belongs to exactly one user
- Users can have multiple medications
- Cascade delete: Remove user → Remove all medications

**Medication ↔ MedicationSchedule**: One-to-Many  
- Each medication can have multiple schedules (e.g., morning/evening doses)
- Each schedule belongs to exactly one medication
- Cascade delete: Remove medication → Remove all schedules

**MedicationSchedule ↔ MedicationLog**: One-to-Many  
- Each log entry can optionally reference a schedule
- Unscheduled doses (PRN/as-needed) have null ScheduleId
- No cascade delete: Keep logs for audit purposes

**User ↔ INRTest**: One-to-Many  
- Each INR test belongs to exactly one user
- Users can have multiple INR tests over time
- No cascade delete: Preserve medical history

---

## Data Validation Rules

### Medication Validation
- **Name**: Required, 1-100 characters
- **DosageAmount**: Required, > 0, max 4 decimal places
- **DosageUnit**: Required, from enum (mg, mcg, units)
- **UserId**: Required, must exist in User table

### INRTest Validation  
- **INRValue**: Required, 0.5 ≤ value ≤ 10.0 (physiologically reasonable)
- **TestDate**: Required, cannot be future date, max 1 year old for new entries
- **TargetINR**: If provided, Min < Max, both between 1.0-4.0

### MedicationLog Validation
- **ScheduledDateTime**: Required
- **TakenDateTime**: If Status = Taken, must be within 24 hours of ScheduledDateTime  
- **Status**: Required, valid enum value
- **DeviceId**: Required for audit purposes

### Security Constraints
- All entities except AuditLog support soft deletes (IsActive/IsDeleted flags)
- User data isolation enforced at repository level
- Encrypted fields: Notes (medication), Notes (INR), UserPreferences
- PII fields: FirstName, LastName, Email (encrypted at rest)

---

## Database Indexes

### Performance Indexes
```sql
-- User lookups
CREATE INDEX IX_User_Email ON Users (Email);
CREATE INDEX IX_User_IsActive ON Users (IsActive);

-- Medication queries  
CREATE INDEX IX_Medication_UserId_IsActive ON Medications (UserId, IsActive);
CREATE INDEX IX_MedicationSchedule_MedicationId_IsActive ON MedicationSchedules (MedicationId, IsActive);

-- Medication logs (frequently queried)
CREATE INDEX IX_MedicationLog_UserId_ScheduledDateTime ON MedicationLogs (UserId, ScheduledDateTime DESC);
CREATE INDEX IX_MedicationLog_ScheduleId_Status ON MedicationLogs (ScheduleId, Status);

-- INR queries
CREATE INDEX IX_INRTest_UserId_TestDate ON INRTests (UserId, TestDate DESC);
CREATE INDEX IX_INRSchedule_UserId_IsActive ON INRSchedules (UserId, IsActive);

-- Sync and audit  
CREATE INDEX IX_SyncMetadata_UserId_IsSynced ON SyncMetadata (UserId, IsSynced);
CREATE INDEX IX_AuditLog_UserId_Timestamp ON AuditLogs (UserId, Timestamp DESC);
```

### Unique Constraints
```sql
-- Prevent duplicate email addresses
ALTER TABLE Users ADD CONSTRAINT UQ_User_Email UNIQUE (Email);

-- Prevent duplicate device registrations
ALTER TABLE UserDevices ADD CONSTRAINT UQ_UserDevice_UserId_DeviceId UNIQUE (UserId, DeviceId);

-- One preferences record per user
ALTER TABLE UserPreferences ADD CONSTRAINT UQ_UserPreferences_UserId UNIQUE (UserId);

-- One active INR schedule per user
CREATE UNIQUE INDEX IX_INRSchedule_UserId_Active ON INRSchedules (UserId) WHERE IsActive = 1;
```

---

## Data Migration Strategy

### Version 1.0.0 → 1.1.0 Example
```csharp
public partial class AddINRTargetRanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TargetINRMin", 
            table: "INRTests", 
            type: "decimal(3,1)", 
            nullable: true);
            
        migrationBuilder.AddColumn<decimal>(
            name: "TargetINRMax", 
            table: "INRTests", 
            type: "decimal(3,1)", 
            nullable: true);
            
        // Set default ranges for existing data
        migrationBuilder.Sql(@"
            UPDATE INRTests 
            SET TargetINRMin = 2.0, TargetINRMax = 3.0 
            WHERE TargetINRMin IS NULL AND TargetINRMax IS NULL");
    }
}
```

### Cross-Platform Considerations
- **SQLite (Mobile)**: Uses TEXT for JSON columns, manual foreign key enforcement
- **PostgreSQL (Cloud)**: Native JSON support, full referential integrity
- **Entity Framework**: Handles provider differences through conventions and configurations

---

**Phase 1 Data Model**: Complete. Ready for API contract design and quickstart guide creation.