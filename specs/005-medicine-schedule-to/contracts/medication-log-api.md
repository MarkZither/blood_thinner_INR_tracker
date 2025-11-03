# API Contract: Enhanced Medication Log

**Feature**: 005-medicine-schedule-to  
**Base URL**: `/api/medication-logs`  
**Date**: 2025-11-03

## Overview

This document describes enhancements to the existing Medication Log API to support variance tracking between expected (pattern-calculated) and actual (logged) dosages.

---

## Changes from Existing API

### Breaking Changes
None - all enhancements are backward compatible.

### New Fields in Response
- `expectedDosage` (decimal?, optional): Expected dosage from pattern on log date
- `patternDayNumber` (int?, optional): Position in pattern cycle
- `hasVariance` (boolean): True if actual differs from expected
- `varianceAmount` (decimal?, optional): Actual - Expected
- `variancePercentage` (decimal?, optional): Percentage difference

### New Query Parameters
- `includeVariance` (boolean): Filter logs with variance only
- `varianceThreshold` (decimal): Minimum variance amount to include

---

## Enhanced Endpoints

### 1. Create Medication Log (POST)

**Request** (unchanged from existing API):
```http
POST /api/medication-logs
Content-Type: application/json
Authorization: Bearer {jwt_token}

{
  "medicationId": 123,
  "takenAt": "2025-11-04T20:00:00Z",
  "dosage": 3.0,
  "notes": "Took with dinner"
}
```

**Response** (enhanced with new fields):
```json
{
  "id": 456,
  "medicationId": 123,
  "medicationName": "Warfarin",
  "takenAt": "2025-11-04T20:00:00Z",
  "actualDosage": 3.0,
  "expectedDosage": 4.0,
  "dosageUnit": "mg",
  "hasVariance": true,
  "varianceAmount": -1.0,
  "variancePercentage": -25.0,
  "patternDayNumber": 1,
  "patternId": 42,
  "notes": "Took with dinner",
  "createdDate": "2025-11-04T20:05:00Z"
}
```

**New Response Fields**:
- `expectedDosage`: Calculated from active pattern on `takenAt` date
  - `null` if no pattern was active
  - Auto-populated by server (client doesn't send this)
- `actualDosage`: Value from request body `dosage` field
- `hasVariance`: `true` if `|actualDosage - expectedDosage| > 0.01`
- `varianceAmount`: `actualDosage - expectedDosage`
  - Negative = took less than expected
  - Positive = took more than expected
- `variancePercentage`: `((actualDosage - expectedDosage) / expectedDosage) * 100`
- `patternDayNumber`: Position in pattern (1-based)
- `patternId`: ID of pattern that was active on `takenAt` date

**Backward Compatibility**:
- Existing clients can continue using `dosage` field in request
- Server internally maps to `actualDosage`
- Response includes both `dosage` and `actualDosage` for transition period

---

### 2. Get Medication Logs (GET)

**Request** (with new filter options):
```http
GET /api/medication-logs?medicationId=123&includeVariance=true&varianceThreshold=0.5&limit=10
Authorization: Bearer {jwt_token}
```

**New Query Parameters**:
- `includeVariance` (boolean, optional): If `true`, returns only logs with variance
- `varianceThreshold` (decimal, optional): Minimum absolute variance to include
  - Example: `0.5` returns logs where `|actualDosage - expectedDosage| >= 0.5`
- `startDate` (date, optional): Filter logs from this date forward
- `endDate` (date, optional): Filter logs up to this date

**Existing Parameters** (unchanged):
- `medicationId` (integer, optional): Filter by medication
- `limit` (integer, optional, default: 20): Number of logs to return
- `offset` (integer, optional, default: 0): Pagination offset

**Response**:
```json
{
  "totalCount": 3,
  "logs": [
    {
      "id": 456,
      "medicationId": 123,
      "medicationName": "Warfarin",
      "takenAt": "2025-11-04T20:00:00Z",
      "actualDosage": 3.0,
      "expectedDosage": 4.0,
      "dosageUnit": "mg",
      "hasVariance": true,
      "varianceAmount": -1.0,
      "variancePercentage": -25.0,
      "patternDayNumber": 1,
      "patternId": 42,
      "notes": "Took with dinner",
      "createdDate": "2025-11-04T20:05:00Z"
    },
    {
      "id": 457,
      "medicationId": 123,
      "medicationName": "Warfarin",
      "takenAt": "2025-11-05T20:00:00Z",
      "actualDosage": 5.0,
      "expectedDosage": 4.0,
      "dosageUnit": "mg",
      "hasVariance": true,
      "varianceAmount": 1.0,
      "variancePercentage": 25.0,
      "patternDayNumber": 2,
      "patternId": 42,
      "notes": "Felt I needed more",
      "createdDate": "2025-11-05T20:10:00Z"
    },
    {
      "id": 458,
      "medicationId": 123,
      "medicationName": "Warfarin",
      "takenAt": "2025-11-06T20:00:00Z",
      "actualDosage": 2.5,
      "expectedDosage": 3.0,
      "dosageUnit": "mg",
      "hasVariance": true,
      "varianceAmount": -0.5,
      "variancePercentage": -16.67,
      "patternDayNumber": 3,
      "patternId": 42,
      "notes": "Pill splitter miscalculation",
      "createdDate": "2025-11-06T20:15:00Z"
    }
  ]
}
```

---

### 3. Get Variance Report (NEW)

**Request**:
```http
GET /api/medication-logs/variance-report?medicationId=123&startDate=2025-10-01&endDate=2025-11-01
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
- `medicationId` (integer, required): Medication to analyze
- `startDate` (date, optional, default: 30 days ago): Report start date
- `endDate` (date, optional, default: today): Report end date

**Response**:
```json
{
  "medicationId": 123,
  "medicationName": "Warfarin",
  "reportPeriod": {
    "startDate": "2025-10-01",
    "endDate": "2025-11-01",
    "totalDays": 32
  },
  "summary": {
    "totalLogs": 30,
    "logsWithVariance": 8,
    "varianceRate": 26.67,
    "averageVariance": -0.42,
    "largestPositiveVariance": 1.5,
    "largestNegativeVariance": -2.0,
    "totalExpectedDosage": 108.0,
    "totalActualDosage": 104.5,
    "complianceRate": 96.76
  },
  "varianceBreakdown": {
    "tookMore": 3,
    "tookLess": 5,
    "tookCorrect": 22
  },
  "varianceLogs": [
    {
      "id": 456,
      "takenAt": "2025-11-04T20:00:00Z",
      "expectedDosage": 4.0,
      "actualDosage": 3.0,
      "varianceAmount": -1.0,
      "notes": "Took with dinner"
    }
    // ... remaining variance logs
  ]
}
```

**Use Case**: Medical dashboard showing dosing accuracy over time.

---

## DTOs

### MedicationLogDto (Enhanced)

```csharp
public class MedicationLogDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public DateTime TakenAt { get; set; }
    
    // NEW: Actual dosage (replaces old "Dosage" field)
    public decimal ActualDosage { get; set; }
    
    // NEW: Expected dosage from pattern
    public decimal? ExpectedDosage { get; set; }
    
    public string DosageUnit { get; set; } = "mg";
    
    // NEW: Variance tracking
    public bool HasVariance { get; set; }
    public decimal? VarianceAmount { get; set; }
    public decimal? VariancePercentage { get; set; }
    
    // NEW: Pattern tracking
    public int? PatternDayNumber { get; set; }
    public int? PatternId { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    // DEPRECATED (keep for backward compatibility)
    [Obsolete("Use ActualDosage instead")]
    public decimal Dosage => ActualDosage;
}
```

### VarianceReportDto (NEW)

```csharp
public class VarianceReportDto
{
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public ReportPeriod ReportPeriod { get; set; } = null!;
    public VarianceSummary Summary { get; set; } = null!;
    public VarianceBreakdown VarianceBreakdown { get; set; } = null!;
    public List<MedicationLogDto> VarianceLogs { get; set; } = new();
}

public class ReportPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
}

public class VarianceSummary
{
    public int TotalLogs { get; set; }
    public int LogsWithVariance { get; set; }
    public decimal VarianceRate { get; set; } // Percentage
    public decimal AverageVariance { get; set; }
    public decimal LargestPositiveVariance { get; set; }
    public decimal LargestNegativeVariance { get; set; }
    public decimal TotalExpectedDosage { get; set; }
    public decimal TotalActualDosage { get; set; }
    public decimal ComplianceRate { get; set; } // (Actual / Expected) * 100
}

public class VarianceBreakdown
{
    public int TookMore { get; set; }
    public int TookLess { get; set; }
    public int TookCorrect { get; set; }
}
```

---

## Validation Rules

### Expected Dosage Calculation

When creating a medication log:

1. **Check for active pattern**:
   ```csharp
   var pattern = medication.GetPatternForDate(takenAt);
   if (pattern != null)
   {
       log.ExpectedDosage = pattern.GetDosageForDate(takenAt);
       log.DosagePatternId = pattern.Id;
       // Calculate pattern day number
       int daysSinceStart = (takenAt.Date - pattern.StartDate.Date).Days;
       log.PatternDayNumber = (daysSinceStart % pattern.PatternLength) + 1;
   }
   ```

2. **Fallback to fixed dosage**:
   ```csharp
   else if (medication.IsActive)
   {
       log.ExpectedDosage = medication.Dosage;
   }
   ```

3. **No expected dosage**:
   ```csharp
   else
   {
       log.ExpectedDosage = null;
       log.HasVariance = false;
   }
   ```

### Variance Threshold

Default variance threshold: `0.01mg` (accounts for floating-point rounding).

```csharp
public bool HasVariance => ExpectedDosage.HasValue && 
                           Math.Abs(ActualDosage - ExpectedDosage.Value) > 0.01m;
```

---

## UI Integration Scenarios

### Scenario 1: Log Dose Screen

**User Flow**:
1. User opens "Log Dose" screen for Warfarin
2. API call: `GET /api/medications/123/schedule/date?date=today`
3. UI pre-fills dosage field with `expectedDosage: 4.0mg`
4. User confirms or edits dosage (e.g., changes to `3.0mg`)
5. API call: `POST /api/medication-logs` with `dosage: 3.0`
6. Response includes `hasVariance: true`, `varianceAmount: -1.0`
7. UI shows warning: "⚠️ You logged 3mg, but 4mg was expected (Day 1 of pattern)"

### Scenario 2: Log History with Variance Indicators

**User Flow**:
1. User opens "Log History" screen
2. API call: `GET /api/medication-logs?medicationId=123&limit=20`
3. UI displays list with variance icons:
   ```
   Nov 4, 8:00 PM → 3mg ⚠️ Expected: 4mg (-1mg, -25%)
   Nov 5, 8:00 PM → 4mg ✓ Correct dose
   Nov 6, 8:00 PM → 2.5mg ⚠️ Expected: 3mg (-0.5mg, -17%)
   ```

### Scenario 3: Variance Report Dashboard

**User Flow**:
1. User opens "Insights" dashboard
2. API call: `GET /api/medication-logs/variance-report?medicationId=123&startDate=30daysago`
3. UI displays:
   - **Compliance Rate**: 96.8% (29 of 30 doses correct)
   - **Variance Breakdown**: 
     - Took more: 3 times
     - Took less: 5 times
     - Correct: 22 times
   - **Chart**: Line graph showing expected vs. actual over time

---

## Performance Considerations

### Expected Dosage Calculation Performance

- **Pattern lookup**: < 5ms (indexed query)
- **Dosage calculation**: < 1ms (modulo arithmetic)
- **Total log creation**: < 100ms (includes DB insert)

### Variance Report Performance

- **90-day report**: < 500ms (< 100 log records)
- **365-day report**: < 2s (< 400 log records)
- Uses aggregation queries for summary statistics

---

## Migration Strategy

### Phase 1: Add Columns (Non-Breaking)
1. Add `ExpectedDosage`, `PatternDayNumber`, `DosagePatternId` columns to `MedicationLogs` table
2. All new columns nullable (backward compatible)
3. Existing logs have `NULL` for new fields

### Phase 2: Backfill Historical Data (Optional)
```csharp
// One-time migration script
foreach (var log in existingLogs)
{
    var medication = await _context.Medications
        .Include(m => m.DosagePatterns)
        .FirstAsync(m => m.Id == log.MedicationId);
    
    log.SetExpectedDosageFromMedication(medication);
}
await _context.SaveChangesAsync();
```

### Phase 3: Deprecate Old Field Name
- Mark `Dosage` field as obsolete in API documentation
- Redirect clients to use `ActualDosage`
- Maintain backward compatibility for 2 major versions

---

## Authorization

Same as existing Medication Log API:
- ✅ Valid JWT bearer token required
- ✅ User can only access their own medication logs

---

## Testing Examples

### cURL: Create Log with Auto-Calculated Expected Dosage

```bash
curl -X POST https://api.bloodthinner.app/api/medication-logs \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "medicationId": 123,
    "takenAt": "2025-11-04T20:00:00Z",
    "dosage": 3.0,
    "notes": "Took with dinner"
  }'

# Response includes:
# "expectedDosage": 4.0 (auto-calculated)
# "hasVariance": true
# "varianceAmount": -1.0
```

### PowerShell: Get Variance Report

```powershell
$headers = @{
    "Authorization" = "Bearer $env:JWT_TOKEN"
}

$startDate = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
$endDate = (Get-Date).ToString("yyyy-MM-dd")

$report = Invoke-RestMethod `
    -Uri "https://api.bloodthinner.app/api/medication-logs/variance-report?medicationId=123&startDate=$startDate&endDate=$endDate" `
    -Method GET `
    -Headers $headers

Write-Host "Compliance Rate: $($report.summary.complianceRate)%"
Write-Host "Logs with Variance: $($report.summary.logsWithVariance) of $($report.summary.totalLogs)"
```

### C# HttpClient: Filter Variance Logs

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var response = await client.GetAsync(
    $"https://api.bloodthinner.app/api/medication-logs?medicationId=123&includeVariance=true&varianceThreshold=0.5");

var result = await response.Content.ReadFromJsonAsync<MedicationLogsResponse>();

foreach (var log in result.Logs)
{
    if (log.HasVariance)
    {
        Console.WriteLine($"{log.TakenAt:yyyy-MM-dd}: Took {log.ActualDosage}mg, expected {log.ExpectedDosage}mg (Variance: {log.VarianceAmount:+0.0;-0.0}mg)");
    }
}
```

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-11-03 | 2.0 | Added variance tracking fields |
| 2025-11-03 | 2.0 | Added variance report endpoint |
| 2025-11-03 | 2.0 | Auto-calculate expected dosage on log creation |

---

## Backward Compatibility Checklist

- ✅ All new fields are optional in responses (nullable)
- ✅ Existing `dosage` field maintained (aliased to `actualDosage`)
- ✅ No changes to required request fields
- ✅ Existing query parameters continue to work
- ✅ New query parameters are optional (default behavior unchanged)
- ✅ Existing logs with `NULL` variance fields handled gracefully
- ✅ API version unchanged (enhancements, not breaking changes)
