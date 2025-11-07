# Database-Agnostic Entity Models

**Date**: 2025-11-04  
**Status**: Implemented for MedicationDosagePattern (Phase 2), Needs Refactoring for Existing Entities  
**Priority**: Medium (Technical Debt Cleanup)

## Problem Statement

The Shared entity models currently contain database-specific attributes like `[Column(TypeName = "nvarchar(...)")]` which violate separation of concerns and couple domain models to database implementation details. This creates several issues:

1. **Tight Coupling**: Domain models are aware of database implementation
2. **Provider-Specific Code in Shared Layer**: Database type names leak into the shared project
3. **Maintenance Overhead**: PostgreSQL context requires complex loop to convert `nvarchar` → `character varying`
4. **Fragility**: Easy to forget provider-specific overrides when adding new entities

## Solution Pattern (Implemented for MedicationDosagePattern)

### Clean Architecture Approach

**1. Shared Models (Domain Layer)**
- Remove ALL database-specific attributes (`[Column(TypeName = ...)]`)
- Keep only domain validation attributes (`[Required]`, `[StringLength]`, `[Range]`)
- Models are pure POCOs with no infrastructure concerns

**Example (MedicationDosagePattern.cs):**
```csharp
[Required]
public List<decimal> PatternSequence { get; set; } = new();
// NO [Column(TypeName = "jsonb")] attribute
```

**2. Data.Shared (Base Configuration)**
- Configure JSON conversion and default column type
- Use provider-neutral defaults (prefer PostgreSQL conventions as baseline)

**Example (ApplicationDbContextBase.cs):**
```csharp
entity.Property(p => p.PatternSequence)
    .HasConversion(
        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
        v => System.Text.Json.JsonSerializer.Deserialize<List<decimal>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<decimal>()
    )
    .HasColumnType("jsonb") // Default: PostgreSQL native type
    .IsRequired();
```

**3. Data.SQLite (Provider-Specific Override)**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // SQLite doesn't support JSONB - override to use TEXT for JSON columns
    modelBuilder.Entity<MedicationDosagePattern>(entity =>
    {
        entity.Property(p => p.PatternSequence)
            .HasColumnType("TEXT");
    });
}
```

**4. Data.SqlServer (Provider-Specific Override)**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // SQL Server doesn't support JSONB - override to use NVARCHAR(MAX) for JSON columns
    modelBuilder.Entity<MedicationDosagePattern>(entity =>
    {
        entity.Property(p => p.PatternSequence)
            .HasColumnType("nvarchar(max)");
    });

    // SQL Server cascade delete restrictions (existing code remains)
    foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
    {
        relationship.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
```

**5. Data.PostgreSQL (No Override Needed)**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ✓ PostgreSQL uses jsonb natively - no override needed
    // ✓ DELETE THIS COMPLEX nvarchar CONVERSION LOOP after refactoring existing entities
}
```

## Benefits

1. **Clean Domain Models**: Shared entities have no database knowledge
2. **Explicit Configuration**: Each provider explicitly states its column types
3. **Maintainability**: Easy to see provider-specific overrides in one place
4. **Type Safety**: Compile-time verification of configuration
5. **Testability**: Domain models can be tested without EF Core
6. **Future-Proof**: Adding new databases requires only new provider-specific context

## Entities Requiring Refactoring

### Phase 1: High Priority (String/Text Columns)
- [ ] **Medication.cs** - Remove `[Column(TypeName = "nvarchar(...)")]` from all string properties
- [ ] **MedicationLog.cs** - Remove `[Column(TypeName = "nvarchar(...)")]` from Notes and other strings
- [ ] **INRTest.cs** - Remove `[Column(TypeName = "nvarchar(...)")]` from Notes
- [ ] **INRSchedule.cs** - Remove `[Column(TypeName = "nvarchar(...)")]` from all string properties
- [ ] **User.cs** - Remove `[Column(TypeName = "nvarchar(...)")]` from Username, Email, etc.

### Phase 2: Medium Priority (Numeric/Decimal Columns)
- [ ] **Medication.cs** - Remove `[Column(TypeName = "decimal(10,3)")]` from Dosage fields
- [ ] **MedicationLog.cs** - Remove `[Column(TypeName = "decimal(10,3)")]` from ActualDosage, ExpectedDosage
- [ ] **INRTest.cs** - Remove `[Column(TypeName = "decimal(3,1)")]` from INRValue

### Phase 3: Cleanup (Remove PostgreSQL Workaround)
- [ ] **ApplicationDbContext.cs (PostgreSQL)** - Delete the `nvarchar → character varying` conversion loop
- [ ] Test migrations generate correctly for all three providers

## Migration Strategy

**Step-by-Step Refactoring (Per Entity):**

1. **Remove Attribute from Shared Model**
   ```csharp
   // Before:
   [Column(TypeName = "nvarchar(100)")]
   public string Name { get; set; } = string.Empty;
   
   // After:
   public string Name { get; set; } = string.Empty;
   ```

2. **Add Configuration to Data.Shared (Base)**
   ```csharp
   entity.Property(e => e.Name)
       .HasMaxLength(100)
       .IsRequired();
   // Let EF Core use provider defaults (nvarchar for SQL Server, varchar for PostgreSQL)
   ```

3. **Add Provider-Specific Overrides (If Needed)**
   - SQLite: Override if SQLite default differs from expected
   - SQL Server: Override if specific type needed (e.g., `nvarchar(max)` vs `nvarchar(100)`)
   - PostgreSQL: No overrides needed (EF Core uses `character varying` by default)

4. **Create New Migration**
   ```bash
   dotnet ef migrations add RefactorEntityName --project src/BloodThinnerTracker.Data.SQLite
   dotnet ef migrations add RefactorEntityName --project src/BloodThinnerTracker.Data.PostgreSQL
   dotnet ef migrations add RefactorEntityName --project src/BloodThinnerTracker.Data.SqlServer
   ```

5. **Verify No Schema Changes**
   - Migrations should show NO changes (types already match)
   - If schema changes appear, adjust base/override configuration
   - This is a refactoring - database schema should remain identical

6. **Apply and Test**
   ```bash
   dotnet ef database update --project src/BloodThinnerTracker.Data.SQLite
   dotnet ef database update --project src/BloodThinnerTracker.Data.PostgreSQL
   dotnet ef database update --project src/BloodThinnerTracker.Data.SqlServer
   ```

## Example: Refactoring Medication.Name

**Before (Current State):**

```csharp
// Medication.cs (Shared)
[Column(TypeName = "nvarchar(100)")]
public string Name { get; set; } = string.Empty;

// PostgreSQL ApplicationDbContext.cs
// Complex loop converts nvarchar(100) → character varying(100)
```

**After (Clean Architecture):**

```csharp
// Medication.cs (Shared) - Clean domain model
[StringLength(100)]
public string Name { get; set; } = string.Empty;

// ApplicationDbContextBase.cs (Data.Shared)
modelBuilder.Entity<Medication>(entity =>
{
    entity.Property(e => e.Name)
        .HasMaxLength(100)
        .IsRequired();
    // EF Core uses provider defaults: nvarchar(100) SQL Server, character varying(100) PostgreSQL
});

// PostgreSQL ApplicationDbContext.cs - No override needed!
// SQL Server ApplicationDbContext.cs - No override needed!
// SQLite ApplicationDbContext.cs - No override needed!
```

## Testing Checklist

For each refactored entity:

- [ ] Shared project builds without warnings
- [ ] All three database projects build successfully
- [ ] Migrations show NO schema changes (empty Up/Down methods)
- [ ] Existing data remains intact after migration
- [ ] Unit tests pass (domain logic unaffected)
- [ ] Integration tests pass (database operations work)
- [ ] API endpoints continue to function correctly

## Success Criteria

1. **Zero Database Attributes in Shared Models** - All entities are pure POCOs
2. **Delete PostgreSQL Conversion Loop** - No more `nvarchar → character varying` workaround
3. **Consistent Configuration** - All entities use same pattern (base + overrides)
4. **No Schema Changes** - Refactoring is transparent to database
5. **Improved Maintainability** - Easier to add new entities or database providers

## Timeline

- **Phase 1**: 2-3 hours (5 entities × 30 min each)
- **Phase 2**: 1-2 hours (numeric column configuration)
- **Phase 3**: 30 minutes (cleanup and testing)
- **Total**: ~4-6 hours for complete refactoring

## References

- **Implemented Example**: `MedicationDosagePattern` entity (Phase 2, T003-T009e)
- **EF Core Docs**: [Provider-Specific Configuration](https://learn.microsoft.com/en-us/ef/core/modeling/providers)
- **Clean Architecture**: [Domain-Driven Design Entities](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)

## Notes

- This refactoring is **non-breaking** - database schema remains identical
- Prefer doing this **before** major releases to avoid migration complexity
- Consider creating a **single PR** for all entity refactoring (easier to review pattern consistency)
- Keep migrations even if they're empty (documents that refactoring occurred)
