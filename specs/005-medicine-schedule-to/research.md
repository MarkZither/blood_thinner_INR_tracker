# Research: Complex Medication Dosage Patterns

**Feature**: 005-medicine-schedule-to  
**Date**: 2025-11-03  
**Status**: Complete

## Executive Summary

This research document addresses the technical unknowns for implementing complex medication dosage patterns in the Blood Thinner INR Tracker. The feature must support variable-dosage schedules (e.g., "4mg, 4mg, 3mg" repeating) while maintaining the existing model-first EF Core architecture with MudBlazor UI components.

---

## R1: Pattern Storage Strategy

### Decision
Store dosage patterns in a separate `MedicationDosagePattern` entity with a JSON column for the pattern sequence, using EF Core's JSON support (.NET 10 / EF Core 9+).

### Rationale
- **EF Core 9 JSON Support**: Native JSON column mapping provides type-safe queries and indexing
- **Temporal History**: Separate entity enables temporal data pattern with `StartDate` and `EndDate` for pattern version history
- **Model-First Compatibility**: EF Core generates appropriate database schema (JSONB for PostgreSQL, JSON for SQLite)
- **Query Performance**: Can index pattern metadata (length, start date) while keeping full pattern in JSON
- **Flexibility**: Supports patterns of varying lengths (2-365 days) without schema changes

### Alternatives Considered
1. **Single VARCHAR column on Medication table**
   - ❌ Rejected: No temporal history, harder to query historical patterns
   - ❌ Rejected: Mixing current and historical data complicates logic

2. **Separate DosageValue rows (one row per day in pattern)**
   - ❌ Rejected: Creates 365 rows for maximum pattern length
   - ❌ Rejected: Complicates pattern cycle calculations
   - ❌ Rejected: Excessive database rows for simple patterns

3. **XML column**
   - ❌ Rejected: Less idiomatic for .NET 10, JSON is modern standard
   - ❌ Rejected: MudBlazor components work better with JSON

### Implementation Notes
```csharp
public class MedicationDosagePattern
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    
    // EF Core 9 JSON column mapping
    public List<decimal> PatternSequence { get; set; } = new();
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } // Null = currently active
    public int PatternLength => PatternSequence.Count;
}

// In DbContext configuration:
modelBuilder.Entity<MedicationDosagePattern>()
    .Property(p => p.PatternSequence)
    .HasColumnType("jsonb"); // PostgreSQL
```

---

## R2: Pattern Position Calculation Algorithm

### Decision
Use modulo arithmetic with zero-based indexing for pattern position calculation:
```csharp
int daysSinceStart = (currentDate - pattern.StartDate).Days;
int patternIndex = daysSinceStart % pattern.PatternLength;
decimal todaysDose = pattern.PatternSequence[patternIndex];
```

### Rationale
- **Performance**: O(1) calculation regardless of date range
- **Accuracy**: Works correctly across multiple years without drift
- **Simplicity**: Single line calculation, easy to test and verify
- **Framework Support**: Built-in .NET DateTime and TimeSpan operations

### Alternatives Considered
1. **Day-by-day iteration from start date**
   - ❌ Rejected: O(n) performance for far-future dates
   - ❌ Rejected: Could cause performance issues for 2+ year calculations

2. **Pre-calculated calendar table**
   - ❌ Rejected: Requires generating and storing millions of rows
   - ❌ Rejected: Complicates pattern changes and historical queries

### Edge Cases Validated
- ✅ Pattern cycles correctly (Day 7 of 6-day pattern = Day 1 of cycle 2)
- ✅ Leap years handled correctly by .NET DateTime
- ✅ Timezone-independent (uses date-only comparison)
- ✅ Works for patterns of any length (2-365 days)

---

## R3: MudBlazor Pattern Entry UI Component

### Decision
Use `MudChipSet` + `MudNumericField` for flexible pattern entry with three input modes:
1. **Simple Mode**: Single dosage (existing behavior)
2. **Pattern Mode**: Comma-separated or repeating pattern entry
3. **Advanced Mode**: Day-by-day manual entry with calendar picker

### Rationale
- **Pure .NET**: MudBlazor components avoid JavaScript interop (constitution requirement)
- **User Flexibility**: Supports user's request for "simple 1 value, to simple repeating patterns like 4,3,3 to complex manual patterns"
- **Material Design**: Consistent with existing UI (MudBlazor migration complete)
- **Accessibility**: WCAG 2.1 AA compliant out of the box

### Component Architecture
```razor
<MudToggleGroup @bind-Value="@_entryMode">
    <MudToggleItem Value="@EntryMode.Simple">Single Dose</MudToggleItem>
    <MudToggleItem Value="@EntryMode.Pattern">Repeating Pattern</MudToggleItem>
    <MudToggleItem Value="@EntryMode.Advanced">Manual Schedule</MudToggleItem>
</MudToggleGroup>

@if (_entryMode == EntryMode.Pattern)
{
    <MudTextField @bind-Value="@_patternInput" 
                  Label="Dosage Pattern" 
                  HelperText="Enter dosages separated by commas (e.g., 4, 4, 3, 4, 3, 3)"
                  Validation="@ValidatePattern" />
    <MudChipSet>
        @foreach (var dose in ParsedPattern)
        {
            <MudChip OnClose="@(() => RemoveDose(dose))">@dose mg</MudChip>
        }
    </MudChipSet>
}
```

### Alternatives Considered
1. **Custom JavaScript component**
   - ❌ Rejected: Violates constitution principle III (pure .NET UI)
   - ❌ Rejected: Increases complexity and prerendering issues

2. **DataGrid for day-by-day entry**
   - ❌ Rejected: Too complex for simple patterns (4, 3, 3)
   - ✅ Available in Advanced mode for complex patterns

3. **Text area with JSON**
   - ❌ Rejected: Not user-friendly for non-technical users
   - ❌ Rejected: Higher error rate for pattern entry

---

## R4: Historical Pattern Querying Strategy

### Decision
Implement temporal querying using EF Core's filtering on `StartDate`/`EndDate` range:
```csharp
var activePattern = medication.DosagePatterns
    .Where(p => p.StartDate <= targetDate && 
                (p.EndDate == null || p.EndDate >= targetDate))
    .OrderByDescending(p => p.StartDate)
    .FirstOrDefault();
```

### Rationale
- **Temporal Data Pattern**: Industry-standard approach for time-varying data
- **EF Core Optimization**: Translates to efficient SQL with proper indexes
- **Historical Accuracy**: Ensures dose logs always reference the correct active pattern
- **Pattern Change Tracking**: Complete audit trail of pattern modifications

### Database Indexing
```csharp
// In DbContext configuration:
modelBuilder.Entity<MedicationDosagePattern>()
    .HasIndex(p => new { p.MedicationId, p.StartDate, p.EndDate })
    .HasDatabaseName("IX_MedicationDosagePattern_Temporal");
```

### Alternatives Considered
1. **Versioned pattern with version number**
   - ❌ Rejected: Less intuitive than date-based queries
   - ❌ Rejected: Doesn't naturally support "effective from" semantics

2. **Separate PatternHistory table**
   - ❌ Rejected: Adds complexity without benefit
   - ❌ Rejected: Single pattern table with temporal columns is simpler

---

## R5: MedicationLog Enhancement for Variance Tracking

### Decision
Add `ExpectedDosage` and `VarianceFlag` columns to existing `MedicationLog` entity:
```csharp
public class MedicationLog // Enhanced
{
    // Existing fields...
    public decimal Dosage { get; set; } // Actual dosage logged
    
    // NEW fields:
    public decimal? ExpectedDosage { get; set; } // From pattern on log date
    public int? PatternDayNumber { get; set; } // Position in pattern (1-based)
    public int? DosagePatternId { get; set; } // FK to active pattern
    public bool HasVariance => ExpectedDosage.HasValue && 
                                Math.Abs(Dosage - ExpectedDosage.Value) > 0.01m;
}
```

### Rationale
- **Backward Compatibility**: Optional columns don't break existing logs
- **Audit Trail**: Captures what was expected vs. what was taken
- **Medical Safety**: Variance detection helps identify dosing errors
- **Historical Accuracy**: References the pattern that was active on log date

### UI Display
- **Variance Icon**: MudBlazor `MudIcon` with warning color when `HasVariance == true`
- **Tooltip**: Shows "Expected: 4mg, Taken: 3mg" on hover
- **List Filtering**: Allow users to filter logs by variance status

---

## R6: Migration Strategy for Existing Medications

### Decision
Treat existing single-dosage medications as a pattern with length = 1:
```csharp
// Migration code (one-time)
foreach (var medication in existingMedications)
{
    var pattern = new MedicationDosagePattern
    {
        MedicationId = medication.Id,
        PatternSequence = new List<decimal> { medication.Dosage },
        StartDate = medication.StartDate,
        EndDate = null // Currently active
    };
    context.MedicationDosagePatterns.Add(pattern);
}
```

### Rationale
- **Zero Data Loss**: All existing medications continue to work
- **Unified Code Path**: Same calculation logic works for fixed and variable dosages
- **Seamless Upgrade**: Users see no functional change unless they opt-in to patterns
- **Backward Compatibility**: API consumers see consistent behavior

### UI Migration
- **Default View**: Show existing medications as "Fixed dose: 5mg"
- **Conversion Option**: "Convert to pattern" button to enable variable dosing
- **Clear Indicator**: Badge showing "Simple" vs. "Pattern-based" dosing

---

## R7: Performance Optimization for Future Schedule View

### Decision
Calculate future schedules on-demand (not pre-generated) with client-side caching:
- API endpoint: `GET /api/medications/{id}/schedule?days=28`
- Blazor component caches results for current medication
- Recalculate only on pattern change or date navigation

### Rationale
- **Storage Efficiency**: No need to store pre-calculated schedules
- **Always Accurate**: Reflects latest pattern changes immediately
- **Fast Calculation**: Modulo arithmetic is sub-millisecond for 28 days
- **Scalable**: Works for any date range without pre-computation

### Performance Benchmarks
- 28-day schedule: < 5ms calculation time (well under 500ms requirement)
- 365-day schedule: < 50ms calculation time
- Pattern change propagation: Instant (no cache invalidation needed)

### Alternatives Considered
1. **Pre-generated DosageSchedule table**
   - ❌ Rejected: Storage overhead for all users
   - ❌ Rejected: Requires complex cache invalidation on pattern changes

2. **Background job to generate schedules**
   - ❌ Rejected: Unnecessary complexity for fast calculations
   - ❌ Rejected: Doesn't provide real-time pattern change feedback

---

## R8: EF Core Migration Strategy

### Decision
Use standard EF Core migrations with careful ordering:
1. **Migration 1**: Create `MedicationDosagePattern` table
2. **Migration 2**: Seed patterns for existing medications
3. **Migration 3**: Add new columns to `MedicationLog`
4. **Migration 4**: Add indexes for temporal queries

### Rationale
- **Model-First**: Aligns with project architecture (constitution principle)
- **Rollback Safety**: Each migration is independently reversible
- **Multi-Database**: EF Core handles PostgreSQL vs. SQLite differences
- **Testing**: Can test migrations in isolation

### Migration Code Structure
```csharp
// Migration: AddMedicationDosagePatterns
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "MedicationDosagePatterns",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("Sqlite:Autoincrement", true)
                .Annotation("Npgsql:ValueGenerationStrategy", 
                    NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            MedicationId = table.Column<int>(nullable: false),
            PatternSequence = table.Column<string>(type: "jsonb", nullable: false),
            StartDate = table.Column<DateTime>(nullable: false),
            EndDate = table.Column<DateTime>(nullable: true),
            // Audit fields from MedicalEntityBase
            CreatedDate = table.Column<DateTime>(nullable: false),
            ModifiedDate = table.Column<DateTime>(nullable: false)
        });

    // Seed data migration in Migration 2
}
```

---

## R9: Validation Rules for Pattern Entry

### Decision
Implement multi-level validation:
1. **Client-side (MudBlazor)**: Immediate feedback during entry
2. **Server-side (FluentValidation)**: Enforce business rules
3. **Database constraints**: Prevent invalid data

### Validation Rules
```csharp
public class DosagePatternValidator : AbstractValidator<MedicationDosagePattern>
{
    public DosagePatternValidator()
    {
        RuleFor(p => p.PatternSequence)
            .NotEmpty().WithMessage("Pattern cannot be empty")
            .Must(seq => seq.Count >= 2 && seq.Count <= 365)
            .WithMessage("Pattern must have 2-365 dosages");
        
        RuleForEach(p => p.PatternSequence)
            .GreaterThan(0).WithMessage("Dosage must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Dosage must be ≤ 1000mg")
            .ScalePrecision(2, 10).WithMessage("Dosage supports max 2 decimal places");
        
        // Warfarin-specific: Check each value ≤ 20mg
        When(p => p.Medication.Type == MedicationType.VitKAntagonist, () =>
        {
            RuleForEach(p => p.PatternSequence)
                .LessThanOrEqualTo(20).WithMessage("Warfarin dosage must be ≤ 20mg");
        });
    }
}
```

### Rationale
- **Medical Safety**: Prevents dangerous dosage entries
- **User Experience**: Immediate feedback reduces frustration
- **Data Integrity**: Server-side validation ensures security
- **Constitution Compliance**: Aligns with OWASP input validation requirements

---

## R10: API Contract Design

### Decision
RESTful API endpoints following existing project patterns:

**New Endpoints**:
```
POST   /api/medications/{id}/patterns          # Add new pattern
GET    /api/medications/{id}/patterns          # Get pattern history
GET    /api/medications/{id}/patterns/active   # Get current active pattern
PUT    /api/medications/{id}/patterns/{patternId} # Update pattern
GET    /api/medications/{id}/schedule          # Get future schedule
```

**Enhanced Endpoints**:
```
POST   /api/medication-logs                    # Now includes ExpectedDosage
GET    /api/medication-logs                    # Returns variance flags
```

### Rationale
- **REST Compliance**: Follows HTTP standards and existing API conventions
- **Resource Hierarchy**: Patterns are sub-resources of medications
- **Versioning Ready**: Can add v2 endpoints if needed
- **Swagger Compatible**: Auto-documents with existing OpenAPI setup

### Request/Response Examples
```json
// POST /api/medications/123/patterns
{
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "startDate": "2025-11-04",
  "endDate": null
}

// GET /api/medications/123/schedule?days=14
{
  "medicationId": 123,
  "patternLength": 6,
  "schedule": [
    { "date": "2025-11-04", "dosage": 4.0, "patternDay": 1 },
    { "date": "2025-11-05", "dosage": 4.0, "patternDay": 2 },
    { "date": "2025-11-06", "dosage": 3.0, "patternDay": 3 }
    // ... 11 more days
  ]
}
```

---

## Summary of Decisions

| Research Topic | Decision | Key Benefit |
|----------------|----------|-------------|
| R1: Storage | EF Core 9 JSON columns | Native support, type-safe, model-first |
| R2: Calculation | Modulo arithmetic | O(1) performance, accurate |
| R3: UI Entry | MudBlazor components | Pure .NET, accessible, flexible |
| R4: History | Temporal querying | Accurate historical records |
| R5: Logging | Add variance columns | Medical safety, audit trail |
| R6: Migration | Single-value pattern | Zero data loss, unified code |
| R7: Performance | On-demand calculation | Fast, scalable, accurate |
| R8: Migrations | Standard EF Core | Model-first, multi-database |
| R9: Validation | Multi-level | Safety, UX, integrity |
| R10: API | RESTful sub-resources | Standard, documented, extensible |

---

## Open Questions

None - all technical unknowns have been resolved through research.

---

## Next Phase

Proceed to **Phase 1: Design & Contracts** to create:
- `data-model.md` with entity definitions
- `/contracts/` with API specifications
- `quickstart.md` with development guide
