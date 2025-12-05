# Data Model: Offline Sync (read-only phase)

This document describes the local SQLite model used by the mobile app. Per project architecture the mobile client uses the shared EF Core models (`BloodThinnerTracker.Shared`) — the local database schema mirrors the server EF Core model unless an explicit deviation is required. The items below intentionally follow the server-side entities.

All local entities are read-only for this phase.

Entities (mirroring `BloodThinnerTracker.Shared.Models`)

- Medication (table: `Medications`)
  - `Id` (int) — primary key
  - `UserId` (Guid) — owner
  - `Name` (string, nvarchar(100))
  - `GenericName` (string?, nvarchar(100))
  - `BrandName` (string?, nvarchar(100))
  - `Type` (enum `MedicationType`)  
  - `Dosage` (decimal(10,3))
  - `DosageUnit` (string, varchar(20))
  - `Form` (string?, varchar(50))
  - `Color` (string?, varchar(50))
  - `Shape` (string?, varchar(50))
  - `Imprint` (string?, varchar(100))
  - `IsBloodThinner` (bool)
  - `Contraindications` (string?, nvarchar(1000))
  - `StorageInstructions` (string?, nvarchar(500))
  - `MaxDailyDose` (decimal(10,3))
  - `MinHoursBetweenDoses` (int)
  - `RequiresINRMonitoring` (bool)
  - `INRTargetMin` (decimal(3,1)?)
  - `INRTargetMax` (decimal(3,1)?)
  - `Frequency` (enum `MedicationFrequency`)
  - `CustomFrequency` (string?, nvarchar(200))
  - `ScheduledTimes` (string?; JSON nvarchar(500))
  - `StartDate` (DateTime)
  - `EndDate` (DateTime?)
  - `IsActive` (bool)
  - `PrescribedBy` (string?, nvarchar(100))
  - `PrescriptionDate` (DateTime?)
  - `Pharmacy` (string?, nvarchar(100))
  - `PrescriptionNumber` (string?, varchar(50))
  - `Instructions` (string?, nvarchar(500))
  - `FoodInteractions` (string?, nvarchar(500))
  - `DrugInteractions` (string?, nvarchar(500))
  - `SideEffects` (string?, nvarchar(500))
  - `RemindersEnabled` (bool)
  - `ReminderMinutes` (int)
  - `Notes` (string?, nvarchar(1000))
  - Navigation: `MedicationLogs`, `DosagePatterns` (collections)

- MedicationDosagePattern (table: `MedicationDosagePatterns`)
  - `Id` (int) — primary key
  - `MedicationId` (int) — FK to Medication
  - `PatternSequence` (List<decimal>) — stored as JSON
  - `StartDate` (DateTime)
  - `EndDate` (DateTime?)
  - `Notes` (string?, max 500)
  - Computed: `PatternLength` (NotMapped), `IsActive` (NotMapped)

- MedicationLog (table: `MedicationLogs`)
  - `Id` (int) — primary key
  - `MedicationId` (int) — FK to Medication
  - `ScheduledTime` (DateTime)
  - `ActualTime` (DateTime?)
  - `Status` (enum `MedicationLogStatus`)
  - `ActualDosage` (decimal(10,3)?)
  - `ExpectedDosage` (decimal(10,3)?)
  - `PatternDayNumber` (int?)
  - `DosagePatternId` (int?)
  - `ActualDosageUnit` (string?, varchar(20))
  - `Reason` (string?, nvarchar(500))
  - `SideEffects` (string?, nvarchar(500))
  - `Notes` (string?, nvarchar(1000))
  - `EntryMethod` (enum `LogEntryMethod`)
  - `EntryDevice` (string?, varchar(100))
  - `Location` (string?, varchar(100))
  - `ConfirmedByProvider` (bool)
  - `ConfirmedBy` (string?, nvarchar(100))
  - `ConfirmedAt` (DateTime?)
  - `TakenWithFood` (bool?)
  - `FoodDetails` (string?, nvarchar(200))
  - `TimeVarianceMinutes` (int)
  - Computed: `HasVariance`, `VarianceAmount`, `VariancePercentage` (NotMapped)

- INRTest (table: `INRTests`)
  - `Id` (int) — primary key
  - `TestDate` (DateTime)
  - `INRValue` (decimal(4,2))
  - `TargetINRMin` (decimal(3,1)?)
  - `TargetINRMax` (decimal(3,1)?)
  - `ProthrombinTime` (decimal(5,2)?)
  - `PartialThromboplastinTime` (decimal(5,2)?)
  - `Laboratory` (string?, nvarchar(100))
  - `OrderedBy` (string?, nvarchar(100))
  - `TestMethod` (string?, varchar(100))
  - `IsPointOfCare` (bool)
  - `WasFasting` (bool?)
  - `LastMedicationTime` (DateTime?)
  - `MedicationsTaken` (string?, nvarchar(500))
  - `FoodsConsumed` (string?, nvarchar(500))
  - `HealthConditions` (string?, nvarchar(500))
  - `Status` (enum `INRResultStatus`)
  - `RecommendedActions` (string?, nvarchar(1000))
  - `DosageChanges` (string?, nvarchar(500))
  - `NextTestDate` (DateTime?)
  - `Notes` (string?, nvarchar(1000))
  - `ReviewedByProvider` (bool)
  - `ReviewedBy` (string?, nvarchar(100))
  - `ReviewedAt` (DateTime?)
  - `UpdatedBy` (Guid?)
  - `DeletedBy` (Guid?)
  - `PatientNotified` (bool)
  - `NotificationMethod` (string?, varchar(50))

Storage behavior and validation

- Primary schema: local SQLite mirrors the shared EF Core model (`BloodThinnerTracker.Shared`). Keys are integers on server models (not GUIDs) — the mobile app persists the same types to maintain parity and mapping consistency.
- Indexes: follow server indexes where present; the mobile DB should include at least indexes on `Medication.IsActive` and range indexes on `MedicationLog.ScheduledTime` / `INRTests.TestDate` to support queries.
- Retention: local cache retention remains configurable (default 365 days) and can be implemented as a scheduled cleanup job using the same retention policy as server if desired.

Notes

- `SyncQueueItem` and write-queue concepts remain intentionally excluded for this read-only phase; however the local schema otherwise mirrors server entities so future write syncs are simpler to implement.
- If any field in the server set is intentionally unnecessary on-device, document an explicit deviation in this file and justify the change. Otherwise this file mirrors the server EF Core model.
