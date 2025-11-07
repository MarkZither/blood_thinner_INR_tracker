# Developer Quickstart: Complex Dosage Patterns

**Feature**: 005-medicine-schedule-to  
**Date**: 2025-11-03  
**Estimated Setup Time**: 15 minutes

## Prerequisites

Ensure you have the following installed:

- ✅ **.NET 10 SDK** (version 10.0.x or later)
  ```powershell
  dotnet --version  # Should show 10.0.x
  ```

- ✅ **EF Core CLI Tools**
  ```powershell
  dotnet tool install --global dotnet-ef
  dotnet ef --version  # Should show 9.0.x or later
  ```

- ✅ **PostgreSQL** (for cloud deployment) or **SQLite** (for local development)
  ```powershell
  # SQLite is embedded, no separate install needed
  ```

- ✅ **Git** (for version control)
  ```powershell
  git --version
  ```

---

## Quick Setup (5 Minutes)

### 1. Clone and Navigate

```powershell
cd C:\Source\github\blood_thinner_INR_tracker
git checkout 005-medicine-schedule-to
git pull origin 005-medicine-schedule-to
```

### 2. Restore Dependencies

```powershell
dotnet restore
```

### 3. Build Solution

```powershell
dotnet build
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Database Setup (5 Minutes)

### 1. Create Migration

```powershell
cd src\BloodThinnerTracker.Api

dotnet ef migrations add AddMedicationDosagePatterns `
    --context ApplicationDbContext `
    --output-dir Migrations
```

**What This Does**:
- Creates `MedicationDosagePatterns` table
- Adds variance tracking columns to `MedicationLogs`
- Adds temporal query indexes

**Expected Output**:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

### 2. Apply Migration (Local SQLite)

```powershell
# For local development database
dotnet ef database update --context ApplicationDbContext
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '20251103_AddMedicationDosagePatterns'.
Done.
```

### 3. Verify Migration

```powershell
# List all migrations
dotnet ef migrations list --context ApplicationDbContext
```

**Expected Output** (should include new migration):
```
20251020_InitialCreate
20251025_AddINRTracking
20251103_AddMedicationDosagePatterns (Pending)
```

---

## Testing Setup (3 Minutes)

### 1. Run Unit Tests

```powershell
cd ..\..\tests\BloodThinnerTracker.Api.Tests
dotnet test --filter "FullyQualifiedName~MedicationPattern"
```

**What This Tests**:
- MedicationDosagePattern entity validation
- Pattern calculation logic (GetDosageForDay, GetDosageForDate)
- Temporal querying (GetPatternForDate)
- Variance tracking (HasVariance, VarianceAmount)

**Expected Output**:
```
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 1.2s
```

### 2. Run Integration Tests

```powershell
cd ..\BloodThinnerTracker.Integration.Tests
dotnet test --filter "FullyQualifiedName~PatternIntegration"
```

**What This Tests**:
- API endpoints: POST/GET/PUT patterns
- Schedule calculation endpoint
- Medication log variance tracking
- Database constraints and foreign keys

**Expected Output**:
```
Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8, Duration: 3.5s
```

### 3. Run Blazor Component Tests (bUnit)

```powershell
cd ..\BloodThinnerTracker.Web.Tests
dotnet test --filter "FullyQualifiedName~PatternEntry"
```

**What This Tests**:
- Pattern entry UI component (MudBlazor)
- Pattern display in medication list
- Variance indicators in log history

**Expected Output**:
```
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 2.1s
```

---

## Running the Application (2 Minutes)

### Option 1: .NET Aspire (Recommended)

```powershell
cd ..\..\src\BloodThinnerTracker.AppHost
dotnet run
```

**What This Does**:
- Starts API on `https://localhost:7001`
- Starts Web UI on `https://localhost:7002`
- Opens Aspire Dashboard on `http://localhost:15000`

**Expected Output**:
```
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: http://localhost:15000
      Application started. Press Ctrl+C to shut down.
```

### Option 2: API Only

```powershell
cd ..\BloodThinnerTracker.Api
dotnet run
```

**API Endpoints**:
- Swagger UI: `https://localhost:7001/swagger`
- Health Check: `https://localhost:7001/health`

### Option 3: Web UI Only

```powershell
cd ..\BloodThinnerTracker.Web
dotnet run
```

**Web UI**:
- Home: `https://localhost:7002`
- Medications: `https://localhost:7002/medications`

---

## Verify Feature Works

### Test 1: Create a Dosage Pattern (API)

```powershell
# Set your JWT token (from login)
$token = "your_jwt_token_here"

# Create a new pattern
$pattern = @{
    patternSequence = @(4.0, 4.0, 3.0, 4.0, 3.0, 3.0)
    startDate = (Get-Date).ToString("yyyy-MM-dd")
    notes = "Test pattern from quickstart"
    closePreviousPattern = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7001/api/medications/1/patterns" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $token" } `
    -ContentType "application/json" `
    -Body $pattern
```

**Expected Response**:
```json
{
  "id": 1,
  "medicationId": 1,
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-11-04",
  "isActive": true,
  "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"
}
```

### Test 2: Get Future Schedule (API)

```powershell
Invoke-RestMethod -Uri "https://localhost:7001/api/medications/1/schedule?days=14" `
    -Method GET `
    -Headers @{ Authorization = "Bearer $token" }
```

**Expected Response**:
```json
{
  "medicationId": 1,
  "medicationName": "Warfarin",
  "totalDays": 14,
  "schedule": [
    { "date": "2025-11-04", "dosage": 4.0, "patternDay": 1 },
    { "date": "2025-11-05", "dosage": 4.0, "patternDay": 2 },
    { "date": "2025-11-06", "dosage": 3.0, "patternDay": 3 }
    // ... 11 more days
  ]
}
```

### Test 3: Log Dose with Auto-Population (API)

```powershell
$log = @{
    medicationId = 1
    takenAt = (Get-Date).ToString("o")  # ISO 8601 format
    dosage = 3.0
    notes = "Test log from quickstart"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7001/api/medication-logs" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $token" } `
    -ContentType "application/json" `
    -Body $log
```

**Expected Response**:
```json
{
  "id": 1,
  "medicationId": 1,
  "takenAt": "2025-11-04T20:00:00Z",
  "actualDosage": 3.0,
  "expectedDosage": 4.0,
  "hasVariance": true,
  "varianceAmount": -1.0,
  "variancePercentage": -25.0,
  "patternDayNumber": 1
}
```

### Test 4: View in Web UI

1. Navigate to `https://localhost:7002/medications`
2. Click on "Warfarin" (or your test medication)
3. Click "Add Pattern" button
4. Enter pattern: `4, 4, 3, 4, 3, 3`
5. Set start date to today
6. Click "Save"
7. View "Future Schedule" tab showing 14-day calendar

**Expected UI**:
```
┌─────────────────────────────────────┐
│ Future Schedule (Next 14 Days)     │
├─────────────────────────────────────┤
│ Mon 11/04 → 4mg  (Day 1 of 6)      │
│ Tue 11/05 → 4mg  (Day 2 of 6)      │
│ Wed 11/06 → 3mg  (Day 3 of 6)      │
│ Thu 11/07 → 4mg  (Day 4 of 6)      │
│ ...                                 │
└─────────────────────────────────────┘
```

---

## Troubleshooting

### Issue: Migration Already Exists

**Error**:
```
The migration '20251103_AddMedicationDosagePatterns' has already been applied to the database.
```

**Solution**:
```powershell
# Skip to next step - migration already applied
dotnet ef database update --context ApplicationDbContext
```

---

### Issue: Database Connection Failed

**Error**:
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server.
```

**Solution** (for local development):
```powershell
# Check connection string in appsettings.Development.json
cat src\BloodThinnerTracker.Api\appsettings.Development.json

# For SQLite (default local), connection string should be:
"ConnectionStrings": {
  "DefaultConnection": "Data Source=bloodtracker_dev.db"
}
```

---

### Issue: JWT Token Expired

**Error** (API returns 401):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

**Solution**:
```powershell
# Log in again to get new token
$credentials = @{
    email = "test@example.com"
    password = "Test123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $credentials

$token = $response.token
```

---

### Issue: Pattern Validation Error

**Error**:
```json
{
  "errors": {
    "PatternSequence[0]": ["Warfarin dosage must be ≤ 20mg"]
  }
}
```

**Solution**:
- Ensure all dosages in pattern are ≤ 20mg for Warfarin
- Ensure pattern has 1-365 dosages
- Ensure all dosages are > 0

---

## Development Workflow

### 1. Create Feature Branch

```powershell
git checkout -b feature/your-feature-name
```

### 2. Make Changes

Edit files in:
- `src/BloodThinnerTracker.Shared/Models/` (entity changes)
- `src/BloodThinnerTracker.Api/Controllers/` (API endpoints)
- `src/BloodThinnerTracker.Web/Components/Pages/` (UI components)

### 3. Add Migration (if entity changed)

```powershell
cd src\BloodThinnerTracker.Api
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### 4. Run Tests

```powershell
dotnet test
```

**Expected**: 90%+ code coverage (constitutional requirement)

### 5. Commit Changes

```powershell
git add .
git commit -m "feat(patterns): Add your feature description

- Bullet point 1
- Bullet point 2"
```

**Important**: Keep commit message under 72 characters for summary line to avoid terminal crashes.

### 6. Push and Create PR

```powershell
git push origin feature/your-feature-name
```

---

## Useful Commands

### Database Commands

```powershell
# Create migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Generate SQL script
dotnet ef migrations script

# Remove last migration (not applied)
dotnet ef migrations remove

# Drop database (CAUTION: Deletes all data)
dotnet ef database drop
```

### Testing Commands

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MedicationPatternTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~GetDosageForDay_CalculatesCorrectly"

# Run tests in parallel
dotnet test --parallel
```

### Build Commands

```powershell
# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Build solution
dotnet build

# Build specific project
dotnet build src/BloodThinnerTracker.Api

# Build in Release mode
dotnet build --configuration Release

# Publish for deployment
dotnet publish --configuration Release --output ./publish
```

---

## Code Examples

### Example 1: Create Pattern in Service

```csharp
// In MedicationPatternService.cs
public async Task<DosagePatternDto> CreatePatternAsync(
    int medicationId, 
    CreateDosagePatternRequest request)
{
    var medication = await _context.Medications
        .Include(m => m.DosagePatterns)
        .FirstOrDefaultAsync(m => m.Id == medicationId);

    if (medication == null)
        throw new NotFoundException($"Medication {medicationId} not found");

    // Close previous active pattern if requested
    if (request.ClosePreviousPattern)
    {
        var activePattern = medication.DosagePatterns
            .FirstOrDefault(p => p.EndDate == null);
        
        if (activePattern != null)
        {
            activePattern.EndDate = request.StartDate.AddDays(-1);
        }
    }

    // Create new pattern
    var pattern = new MedicationDosagePattern
    {
        MedicationId = medicationId,
        PatternSequence = request.PatternSequence,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        Notes = request.Notes
    };

    _context.MedicationDosagePatterns.Add(pattern);
    await _context.SaveChangesAsync();

    return _mapper.Map<DosagePatternDto>(pattern);
}
```

### Example 2: Calculate Schedule in Controller

```csharp
// In MedicationsController.cs
[HttpGet("{id}/schedule")]
public async Task<ActionResult<ScheduleResponse>> GetSchedule(
    int id,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] int days = 14)
{
    var medication = await _context.Medications
        .Include(m => m.DosagePatterns)
        .FirstOrDefaultAsync(m => m.Id == id);

    if (medication == null)
        return NotFound();

    var start = startDate ?? DateTime.Today;
    var schedule = medication.GetFutureSchedule(start, days);

    return Ok(new ScheduleResponse
    {
        MedicationId = medication.Id,
        MedicationName = medication.Name,
        DosageUnit = medication.DosageUnit,
        StartDate = start,
        EndDate = start.AddDays(days - 1),
        TotalDays = days,
        CurrentPattern = _mapper.Map<PatternSummaryDto>(medication.ActivePattern),
        Schedule = schedule.Select(e => new ScheduleEntry
        {
            Date = e.Date,
            DayOfWeek = e.Date.DayOfWeek.ToString(),
            Dosage = e.Dosage,
            PatternDay = e.PatternDay ?? 0,
            PatternLength = e.PatternLength ?? 0
        }).ToList()
    });
}
```

### Example 3: Blazor Pattern Entry Component

```razor
@* In PatternEntry.razor *@
@using MudBlazor
@inject IMedicationService MedicationService

<MudPaper Class="pa-4">
    <MudText Typo="Typo.h6">Add Dosage Pattern</MudText>
    
    <MudTextField @bind-Value="@_patternInput"
                  Label="Pattern (comma-separated)"
                  HelperText="Example: 4, 4, 3, 4, 3, 3"
                  Variant="Variant.Outlined"
                  Margin="Margin.Dense" />
    
    <MudChipSet>
        @foreach (var dose in _parsedPattern)
        {
            <MudChip Color="Color.Primary" 
                     OnClose="@(() => RemoveDose(dose))">
                @dose mg
            </MudChip>
        }
    </MudChipSet>
    
    <MudDatePicker @bind-Date="@_startDate"
                   Label="Start Date"
                   Variant="Variant.Outlined" />
    
    <MudButton Variant="Variant.Filled" 
               Color="Color.Primary"
               OnClick="@SavePattern">
        Save Pattern
    </MudButton>
</MudPaper>

@code {
    [Parameter] public int MedicationId { get; set; }
    
    private string _patternInput = "";
    private List<decimal> _parsedPattern = new();
    private DateTime? _startDate = DateTime.Today;

    private void RemoveDose(decimal dose)
    {
        _parsedPattern.Remove(dose);
        _patternInput = string.Join(", ", _parsedPattern);
    }

    private async Task SavePattern()
    {
        _parsedPattern = _patternInput
            .Split(',')
            .Select(s => decimal.Parse(s.Trim()))
            .ToList();

        var request = new CreateDosagePatternRequest
        {
            PatternSequence = _parsedPattern,
            StartDate = _startDate ?? DateTime.Today,
            ClosePreviousPattern = true
        };

        await MedicationService.CreatePatternAsync(MedicationId, request);
        // Navigate or refresh
    }
}
```

---

## Next Steps

1. ✅ **Read the specification**: `specs/005-medicine-schedule-to/spec.md`
2. ✅ **Review data model**: `specs/005-medicine-schedule-to/data-model.md`
3. ✅ **Check API contracts**: `specs/005-medicine-schedule-to/contracts/`
4. ✅ **Start development**: Implement your assigned task
5. ✅ **Write tests**: Maintain 90%+ coverage
6. ✅ **Create PR**: Follow commit message guidelines

---

## Resources

- **Project Documentation**: `docs/`
- **Constitution**: `.specify/memory/constitution.md`
- **Copilot Instructions**: `.github/copilot-instructions.md`
- **Feature Spec**: `specs/005-medicine-schedule-to/spec.md`
- **API Contracts**: `specs/005-medicine-schedule-to/contracts/`

---

## Support

For questions or issues:
1. Check this quickstart guide
2. Review the feature specification
3. Check existing tests for examples
4. Ask in team chat or create an issue

---

**Happy Coding! 🚀**
