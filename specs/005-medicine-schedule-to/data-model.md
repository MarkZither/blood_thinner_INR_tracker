# Data Model: Complex Medication Dosage Patterns

**Feature**: 005-medicine-schedule-to  
**Date**: 2025-11-03  
**Status**: Complete

## Executive Summary

This document defines the entity model enhancements required to support complex medication dosage patterns with temporal tracking. The design follows model-first EF Core principles, maintains backward compatibility, and enables historical pattern comparison.

---

## New Entity: MedicationDosagePattern

### Purpose
Stores variable-dosage schedules with temporal validity, enabling pattern history tracking and date-based dosage calculation.

### Entity Definition

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using BloodThinnerTracker.Shared.Models.Base;

namespace BloodThinnerTracker.Shared.Models;

[Table("MedicationDosagePatterns")]
public class MedicationDosagePattern : MedicalEntityBase
{
    /// <summary>
    /// Foreign key to parent Medication.
    /// </summary>
    [Required]
    public int MedicationId { get; set; }

    /// <summary>
    /// Repeating dosage pattern stored as JSON array of decimals.
    /// Example: [4.0, 4.0, 3.0, 4.0, 3.0, 3.0] for 6-day cycle.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")] // PostgreSQL JSONB, SQLite will use TEXT
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Date when this pattern becomes active (inclusive).
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when this pattern ends (inclusive). 
    /// NULL indicates currently active pattern.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional user-provided description (e.g., "Reduced winter dosing").
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual Medication Medication { get; set; } = null!;

    // Computed Properties
    [NotMapped]
    public int PatternLength => PatternSequence?.Count ?? 0;

    [NotMapped]
    public bool IsActive => EndDate == null || EndDate >= DateTime.UtcNow.Date;

    [NotMapped]
    public decimal AverageDosage => PatternSequence?.Count > 0 
        ? PatternSequence.Average() 
        : 0;

    /// <summary>
    /// Calculates the dosage for a specific day number in the pattern (1-based).
    /// </summary>
    /// <param name="dayNumber">Day number starting from 1.</param>
    /// <returns>Dosage for that day in the pattern cycle.</returns>
    public decimal GetDosageForDay(int dayNumber)
    {
        if (PatternSequence == null || PatternSequence.Count == 0)
            throw new InvalidOperationException("Pattern sequence is empty");

        if (dayNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(dayNumber), "Day number must be >= 1");

        // Convert to zero-based index using modulo
        int zeroBasedIndex = (dayNumber - 1) % PatternSequence.Count;
        return PatternSequence[zeroBasedIndex];
    }

    /// <summary>
    /// Calculates the dosage for a specific date.
    /// </summary>
    /// <param name="targetDate">Date to calculate dosage for.</param>
    /// <returns>Dosage for that date, or null if date is outside pattern validity.</returns>
    public decimal? GetDosageForDate(DateTime targetDate)
    {
        // Check if date is within pattern validity period
        if (targetDate.Date < StartDate.Date)
            return null;

        if (EndDate.HasValue && targetDate.Date > EndDate.Value.Date)
            return null;

        // Calculate days since pattern start
        int daysSinceStart = (targetDate.Date - StartDate.Date).Days;
        int dayNumber = (daysSinceStart % PatternLength) + 1;

        return GetDosageForDay(dayNumber);
    }

    /// <summary>
    /// Gets a human-readable pattern representation.
    /// Example: "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"
    /// </summary>
    public string GetDisplayPattern(string unit = "mg")
    {
        if (PatternSequence == null || PatternSequence.Count == 0)
            return "Empty pattern";

        var values = string.Join(", ", PatternSequence.Select(d => $"{d:0.##}{unit}"));
        return $"{values} ({PatternLength}-day cycle)";
    }
}
```

### Database Schema

**Table**: `MedicationDosagePatterns`

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| `Id` | INT | No | Identity | Primary key |
| `MedicationId` | INT | No | - | FK to Medications |
| `PatternSequence` | JSONB/TEXT | No | - | Dosage array |
| `StartDate` | DATE | No | - | Pattern start date |
| `EndDate` | DATE | Yes | NULL | Pattern end date (NULL = active) |
| `Notes` | NVARCHAR(500) | Yes | NULL | User notes |
| `CreatedDate` | DATETIME | No | GETUTCDATE() | Audit field |
| `ModifiedDate` | DATETIME | No | GETUTCDATE() | Audit field |
| `CreatedBy` | NVARCHAR(100) | Yes | NULL | Audit field |
| `ModifiedBy` | NVARCHAR(100) | Yes | NULL | Audit field |

### Indexes

```csharp
// In ApplicationDbContext.OnModelCreating:

modelBuilder.Entity<MedicationDosagePattern>(entity =>
{
    // Configure JSON column for different providers
    entity.Property(p => p.PatternSequence)
        .HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<decimal>>(v, (JsonSerializerOptions?)null) ?? new List<decimal>()
        )
        .HasColumnType("jsonb") // PostgreSQL
        .IsRequired();

    // Temporal query index (most common: find active pattern for medication)
    entity.HasIndex(p => new { p.MedicationId, p.StartDate, p.EndDate })
        .HasDatabaseName("IX_MedicationDosagePattern_Temporal");

    // Active patterns index (filter NULL EndDate)
    entity.HasIndex(p => new { p.MedicationId, p.EndDate })
        .HasDatabaseName("IX_MedicationDosagePattern_Active")
        .HasFilter("EndDate IS NULL");

    // Foreign key
    entity.HasOne(p => p.Medication)
        .WithMany(m => m.DosagePatterns)
        .HasForeignKey(p => p.MedicationId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### Validation Rules

```csharp
using FluentValidation;

public class MedicationDosagePatternValidator : AbstractValidator<MedicationDosagePattern>
{
    public MedicationDosagePatternValidator()
    {
        RuleFor(p => p.MedicationId)
            .GreaterThan(0)
            .WithMessage("Medication ID is required");

        RuleFor(p => p.PatternSequence)
            .NotNull().WithMessage("Pattern sequence is required")
            .Must(seq => seq != null && seq.Count >= 1 && seq.Count <= 365)
            .WithMessage("Pattern must contain 1-365 dosages");

        RuleForEach(p => p.PatternSequence)
            .GreaterThan(0m).WithMessage("Each dosage must be greater than 0")
            .LessThanOrEqualTo(1000m).WithMessage("Each dosage must be ≤ 1000mg")
            .PrecisionScale(10, 3, true).WithMessage("Dosages support max 3 decimal places");

        RuleFor(p => p.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(p => p.EndDate)
            .GreaterThanOrEqualTo(p => p.StartDate)
            .When(p => p.EndDate.HasValue)
            .WithMessage("End date must be >= start date");

        RuleFor(p => p.Notes)
            .MaximumLength(500)
            .When(p => !string.IsNullOrEmpty(p.Notes))
            .WithMessage("Notes cannot exceed 500 characters");

        // Medication-specific validation (requires medication reference)
        RuleFor(p => p.PatternSequence)
            .Must((pattern, seq) => seq.All(d => d <= 20m))
            .When(p => p.Medication?.Type == MedicationType.VitKAntagonist)
            .WithMessage("Warfarin dosages must be ≤ 20mg per safety guidelines");
    }
}
```

---

## Enhanced Entity: Medication

### Changes Required

Add navigation property and helper methods to support pattern-based dosing.

```csharp
public partial class Medication : MedicalEntityBase
{
    // ... existing properties ...

    /// <summary>
    /// Collection of dosage patterns for this medication.
    /// Multiple patterns enable temporal tracking of pattern changes.
    /// </summary>
    public virtual ICollection<MedicationDosagePattern> DosagePatterns { get; set; } 
        = new List<MedicationDosagePattern>();

    /// <summary>
    /// Gets the currently active dosage pattern (where EndDate is NULL).
    /// </summary>
    [NotMapped]
    public MedicationDosagePattern? ActivePattern => DosagePatterns
        .Where(p => p.EndDate == null)
        .OrderByDescending(p => p.StartDate)
        .FirstOrDefault();

    /// <summary>
    /// Indicates whether this medication uses pattern-based dosing.
    /// True if any patterns exist, false if using single fixed dosage.
    /// </summary>
    [NotMapped]
    public bool HasPatternSchedule => DosagePatterns?.Any() ?? false;

    /// <summary>
    /// Gets the expected dosage for a specific date, considering active patterns.
    /// Falls back to single Dosage property if no patterns exist.
    /// </summary>
    /// <param name="targetDate">Date to calculate dosage for.</param>
    /// <returns>Expected dosage, or null if no pattern/dosage is defined.</returns>
    public decimal? GetExpectedDosageForDate(DateTime targetDate)
    {
        // Find the pattern that was active on the target date
        var activePattern = DosagePatterns?
            .Where(p => p.StartDate.Date <= targetDate.Date && 
                       (p.EndDate == null || p.EndDate.Value.Date >= targetDate.Date))
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();

        if (activePattern != null)
        {
            return activePattern.GetDosageForDate(targetDate);
        }

        // Fallback to single fixed dosage (backward compatibility)
        if (StartDate.Date <= targetDate.Date && 
            (!EndDate.HasValue || EndDate.Value.Date >= targetDate.Date) &&
            IsActive)
        {
            return Dosage;
        }

        return null;
    }

    /// <summary>
    /// Gets the pattern that was active on a specific date (historical query).
    /// </summary>
    /// <param name="targetDate">Date to find active pattern for.</param>
    /// <returns>The pattern active on that date, or null if none found.</returns>
    public MedicationDosagePattern? GetPatternForDate(DateTime targetDate)
    {
        return DosagePatterns?
            .Where(p => p.StartDate.Date <= targetDate.Date && 
                       (p.EndDate == null || p.EndDate.Value.Date >= targetDate.Date))
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Generates a future dosage schedule for display.
    /// </summary>
    /// <param name="startDate">Starting date for schedule.</param>
    /// <param name="days">Number of days to generate.</param>
    /// <returns>List of date/dosage pairs.</returns>
    public List<DosageScheduleEntry> GetFutureSchedule(DateTime startDate, int days)
    {
        var schedule = new List<DosageScheduleEntry>();

        for (int i = 0; i < days; i++)
        {
            var date = startDate.Date.AddDays(i);
            var dosage = GetExpectedDosageForDate(date);
            var pattern = GetPatternForDate(date);

            if (dosage.HasValue)
            {
                int? patternDay = null;
                if (pattern != null)
                {
                    int daysSinceStart = (date - pattern.StartDate.Date).Days;
                    patternDay = (daysSinceStart % pattern.PatternLength) + 1;
                }

                schedule.Add(new DosageScheduleEntry
                {
                    Date = date,
                    Dosage = dosage.Value,
                    DosageUnit = DosageUnit,
                    PatternDay = patternDay,
                    PatternLength = pattern?.PatternLength,
                    IsPatternChange = i > 0 && pattern?.StartDate.Date == date
                });
            }
        }

        return schedule;
    }
}

/// <summary>
/// DTO for dosage schedule display.
/// </summary>
public class DosageScheduleEntry
{
    public DateTime Date { get; set; }
    public decimal Dosage { get; set; }
    public string DosageUnit { get; set; } = "mg";
    public int? PatternDay { get; set; }
    public int? PatternLength { get; set; }
    public bool IsPatternChange { get; set; }

    public string DisplayText => PatternDay.HasValue 
        ? $"{Dosage:0.##}{DosageUnit} (Day {PatternDay}/{PatternLength})"
        : $"{Dosage:0.##}{DosageUnit}";
}
```

### Database Schema Changes

No schema changes to `Medications` table - all enhancements are navigation properties and computed methods.

---

## Enhanced Entity: MedicationLog

### Changes Required

Add columns to track expected dosage (from pattern) and variance from actual dosage.

```csharp
public partial class MedicationLog : MedicalEntityBase
{
    // ... existing properties ...

    /// <summary>
    /// Actual dosage taken/logged by user.
    /// (Renamed from existing Dosage property for clarity)
    /// </summary>
    [Required]
    [Range(0.1, 1000.0, ErrorMessage = "Dosage must be between 0.1 and 1000 mg")]
    [Column(TypeName = "decimal(10,3)")]
    public decimal ActualDosage { get; set; }

    /// <summary>
    /// Expected dosage from the active pattern on TakenAt date.
    /// NULL if no pattern was active or medication doesn't use patterns.
    /// </summary>
    [Column(TypeName = "decimal(10,3)")]
    public decimal? ExpectedDosage { get; set; }

    /// <summary>
    /// Position in the dosage pattern cycle (1-based).
    /// Example: Day 3 of a 6-day pattern.
    /// </summary>
    public int? PatternDayNumber { get; set; }

    /// <summary>
    /// Reference to the dosage pattern that was active on TakenAt date.
    /// Enables historical pattern lookup.
    /// </summary>
    public int? DosagePatternId { get; set; }

    // Navigation Property
    public virtual MedicationDosagePattern? DosagePattern { get; set; }

    /// <summary>
    /// Computed property: True if actual dosage differs from expected.
    /// Threshold: > 0.01mg difference to account for rounding.
    /// </summary>
    [NotMapped]
    public bool HasVariance => ExpectedDosage.HasValue && 
                               Math.Abs(ActualDosage - ExpectedDosage.Value) > 0.01m;

    /// <summary>
    /// Variance amount (actual - expected). Positive = took more, negative = took less.
    /// </summary>
    [NotMapped]
    public decimal? VarianceAmount => ExpectedDosage.HasValue 
        ? ActualDosage - ExpectedDosage.Value 
        : null;

    /// <summary>
    /// Variance percentage. Example: -25% means took 25% less than expected.
    /// </summary>
    [NotMapped]
    public decimal? VariancePercentage => ExpectedDosage.HasValue && ExpectedDosage.Value > 0
        ? ((ActualDosage - ExpectedDosage.Value) / ExpectedDosage.Value) * 100
        : null;

    /// <summary>
    /// Helper to populate pattern-related fields from medication.
    /// </summary>
    public void SetExpectedDosageFromMedication(Medication medication)
    {
        if (medication == null)
            throw new ArgumentNullException(nameof(medication));

        ExpectedDosage = medication.GetExpectedDosageForDate(TakenAt);

        var activePattern = medication.GetPatternForDate(TakenAt);
        if (activePattern != null)
        {
            DosagePatternId = activePattern.Id;
            int daysSinceStart = (TakenAt.Date - activePattern.StartDate.Date).Days;
            PatternDayNumber = (daysSinceStart % activePattern.PatternLength) + 1;
        }
    }
}
```

### Database Schema Changes

**Alter Table**: `MedicationLogs`

| New Column | Type | Nullable | Default | Description |
|------------|------|----------|---------|-------------|
| `ExpectedDosage` | DECIMAL(10,3) | Yes | NULL | Pattern-calculated dosage |
| `PatternDayNumber` | INT | Yes | NULL | Position in pattern cycle |
| `DosagePatternId` | INT | Yes | NULL | FK to active pattern |

### Migration Code

```csharp
public partial class AddVarianceTrackingToMedicationLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "ExpectedDosage",
            table: "MedicationLogs",
            type: "decimal(10,3)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PatternDayNumber",
            table: "MedicationLogs",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DosagePatternId",
            table: "MedicationLogs",
            type: "integer",
            nullable: true);

        // Add foreign key constraint
        migrationBuilder.CreateIndex(
            name: "IX_MedicationLogs_DosagePatternId",
            table: "MedicationLogs",
            column: "DosagePatternId");

        migrationBuilder.AddForeignKey(
            name: "FK_MedicationLogs_MedicationDosagePatterns_DosagePatternId",
            table: "MedicationLogs",
            column: "DosagePatternId",
            principalTable: "MedicationDosagePatterns",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        // Note: ActualDosage rename from Dosage is handled in separate migration
        // to avoid breaking existing code during transition
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MedicationLogs_MedicationDosagePatterns_DosagePatternId",
            table: "MedicationLogs");

        migrationBuilder.DropIndex(
            name: "IX_MedicationLogs_DosagePatternId",
            table: "MedicationLogs");

        migrationBuilder.DropColumn(name: "ExpectedDosage", table: "MedicationLogs");
        migrationBuilder.DropColumn(name: "PatternDayNumber", table: "MedicationLogs");
        migrationBuilder.DropColumn(name: "DosagePatternId", table: "MedicationLogs");
    }
}
```

---

## Entity Relationships Diagram

```
┌─────────────────────────────────────────────────┐
│ Medication                                      │
│ ─────────────────────────────────────────────── │
│ + Id: int                                       │
│ + Name: string                                  │
│ + Dosage: decimal (legacy single dose)          │
│ + DosageUnit: string                            │
│ + StartDate: DateTime                           │
│ + EndDate: DateTime?                            │
│ ─────────────────────────────────────────────── │
│ + DosagePatterns: ICollection<Pattern>          │ 1 ────┐
│ + MedicationLogs: ICollection<Log>              │       │
│ ─────────────────────────────────────────────── │       │
│ + GetExpectedDosageForDate(date): decimal?      │       │
│ + GetPatternForDate(date): Pattern?             │       │
│ + GetFutureSchedule(date, days): List<Entry>    │       │
└─────────────────────────────────────────────────┘       │
                                                           │
                                                           │ *
┌─────────────────────────────────────────────────┐       │
│ MedicationDosagePattern                         │◄──────┘
│ ─────────────────────────────────────────────── │
│ + Id: int                                       │
│ + MedicationId: int (FK)                        │
│ + PatternSequence: List<decimal> (JSON)         │
│ + StartDate: DateTime                           │
│ + EndDate: DateTime? (NULL = active)            │
│ + Notes: string?                                │
│ ─────────────────────────────────────────────── │
│ + PatternLength: int (computed)                 │ 1 ────┐
│ + IsActive: bool (computed)                     │       │
│ ─────────────────────────────────────────────── │       │
│ + GetDosageForDay(dayNumber): decimal           │       │
│ + GetDosageForDate(date): decimal?              │       │
│ + GetDisplayPattern(unit): string               │       │
└─────────────────────────────────────────────────┘       │
                                                           │
                                                           │ *
┌─────────────────────────────────────────────────┐       │
│ MedicationLog                                   │       │
│ ─────────────────────────────────────────────── │       │
│ + Id: int                                       │       │
│ + MedicationId: int (FK)                        │       │
│ + TakenAt: DateTime                             │       │
│ + ActualDosage: decimal (what user logged)      │       │
│ + ExpectedDosage: decimal? (from pattern)       │       │
│ + PatternDayNumber: int?                        │       │
│ + DosagePatternId: int? (FK)                    │◄──────┘
│ ─────────────────────────────────────────────── │
│ + HasVariance: bool (computed)                  │
│ + VarianceAmount: decimal? (computed)           │
│ + VariancePercentage: decimal? (computed)       │
│ ─────────────────────────────────────────────── │
│ + SetExpectedDosageFromMedication(med): void    │
└─────────────────────────────────────────────────┘
```

---

## Temporal Query Examples

### 1. Get Currently Active Pattern

```csharp
var activePattern = await _context.MedicationDosagePatterns
    .Where(p => p.MedicationId == medicationId && p.EndDate == null)
    .OrderByDescending(p => p.StartDate)
    .FirstOrDefaultAsync();
```

**SQL Generated**:
```sql
SELECT * FROM MedicationDosagePatterns
WHERE MedicationId = @medicationId 
  AND EndDate IS NULL
ORDER BY StartDate DESC
LIMIT 1;
```

### 2. Get Pattern for Specific Historical Date

```csharp
var historicalDate = new DateTime(2024, 10, 15);
var pattern = await _context.MedicationDosagePatterns
    .Where(p => p.MedicationId == medicationId &&
                p.StartDate <= historicalDate &&
                (p.EndDate == null || p.EndDate >= historicalDate))
    .OrderByDescending(p => p.StartDate)
    .FirstOrDefaultAsync();
```

**SQL Generated**:
```sql
SELECT * FROM MedicationDosagePatterns
WHERE MedicationId = @medicationId
  AND StartDate <= '2024-10-15'
  AND (EndDate IS NULL OR EndDate >= '2024-10-15')
ORDER BY StartDate DESC
LIMIT 1;
```

### 3. Get All Pattern Changes in Date Range

```csharp
var startDate = DateTime.Today.AddDays(-90);
var endDate = DateTime.Today;

var patternHistory = await _context.MedicationDosagePatterns
    .Where(p => p.MedicationId == medicationId &&
                p.StartDate <= endDate &&
                (p.EndDate == null || p.EndDate >= startDate))
    .OrderBy(p => p.StartDate)
    .ToListAsync();
```

### 4. Find Logs with Variance

```csharp
var varianceLogs = await _context.MedicationLogs
    .Include(l => l.Medication)
    .Include(l => l.DosagePattern)
    .Where(l => l.MedicationId == medicationId &&
                l.ExpectedDosage != null &&
                Math.Abs(l.ActualDosage - l.ExpectedDosage.Value) > 0.01m)
    .OrderByDescending(l => l.TakenAt)
    .ToListAsync();
```

---

## Performance Considerations

### Indexing Strategy

1. **Primary temporal query** (IX_MedicationDosagePattern_Temporal):
   - Covers: `(MedicationId, StartDate, EndDate)`
   - Used for: 90% of pattern lookups
   - Expected QPS: ~100 (pattern calculation on log creation)

2. **Active pattern query** (IX_MedicationDosagePattern_Active):
   - Covers: `(MedicationId, EndDate)` with filter `EndDate IS NULL`
   - Used for: Current pattern retrieval
   - Expected QPS: ~50 (UI displays)

3. **Log pattern reference** (IX_MedicationLogs_DosagePatternId):
   - Covers: `DosagePatternId`
   - Used for: Navigating from logs to patterns
   - Expected QPS: ~20 (variance reports)

### Query Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Get active pattern | < 5ms | Indexed query, ~1 row |
| Calculate 28-day schedule | < 50ms | In-memory calculation |
| Log dose with pattern lookup | < 100ms | Pattern lookup + insert |
| Variance report (90 days) | < 500ms | Filtered query, ~90 rows |

---

## Migration Sequence

To maintain backward compatibility and zero downtime, migrations are applied in this order:

1. **Migration 1**: Create `MedicationDosagePatterns` table
2. **Migration 2**: Seed initial patterns from existing medications (optional)
3. **Migration 3**: Add variance columns to `MedicationLogs`
4. **Migration 4**: Add indexes for temporal queries
5. **Migration 5** (Future): Deprecate `Medication.Dosage` (after full pattern migration)

---

## Summary

| Entity | Change Type | Purpose |
|--------|-------------|---------|
| **MedicationDosagePattern** | NEW | Store temporal patterns with JSON dosage arrays |
| **Medication** | ENHANCED | Add pattern navigation and calculation methods |
| **MedicationLog** | ENHANCED | Add variance tracking fields |

**Key Design Principles**:
- ✅ Model-first EF Core (constitution compliant)
- ✅ Backward compatible (existing single-dosage continues to work)
- ✅ Temporal querying (accurate historical pattern lookups)
- ✅ Performance optimized (indexes for common queries)
- ✅ Medical safety (variance detection, pattern validation)
