# data-model.md

## Entities

### INRTest
- Id: GUID
- UserId: GUID (owner)
- MedicationId: GUID? (optional)
- Value: decimal (precision 3, scale 2) - validation 0.5..8.0
- Units: string (e.g., "INR")
- TestDateTimeUtc: DateTime (UTC)
- Notes: string (nullable)
- CreatedAt: DateTimeUtc
- UpdatedAt: DateTimeUtc (nullable)
- UpdatedBy: GUID (nullable)
- IsDeleted: bool (default false)
- DeletedAt: DateTimeUtc? (nullable)
- DeletedBy: GUID? (nullable)

Indexes:
- PK on Id
- Index on UserId
- Index on TestDateTimeUtc (for trend queries)

### AuditRecord
- Id: GUID
- TargetEntity: string (e.g., "INRTest")
- TargetId: GUID
- ActionType: string (Edit, SoftDelete, Purge)
- ActorId: GUID (nullable)
- TimestampUtc: DateTime
- BeforeJson: nvarchar(max)
- AfterJson: nvarchar(max)
- Metadata: nvarchar(max) (optional, for reason, correlation id)

Indexes:
- PK on Id
- Index on TargetEntity + TargetId

Validation rules:
- Value must be numeric and between 0.5 and 8.0
- TestDateTimeUtc must not be in the future by more than 24 hours (configurable)

State transitions:
- Normal -> Updated (UpdatedAt/UpdatedBy set)
- Normal -> SoftDeleted (IsDeleted=true, DeletedAt, DeletedBy set)
- SoftDeleted -> Purged (delete row by admin process)
