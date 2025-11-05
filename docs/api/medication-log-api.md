# Medication Log API

**Base URL**: `/api/medication-logs`

## Overview

The Medication Log API enables recording of actual medication doses taken, with automatic expected dosage population and variance tracking. When a medication has a dosage pattern, the system automatically calculates the expected dose for the log date and tracks any variance between expected and actual dosages.

## Endpoints

### POST /api/medication-logs

Create a new medication log entry.

**Authentication**: Required (JWT)

**Request Body**:
```json
{
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "dosage": 3.0,
  "takenAt": "2025-01-15T08:30:00Z",
  "notes": "Took with breakfast"
}
```

**Request Fields**:
- `medicationId` (required): UUID of the medication
- `dosage` (required): Actual dosage taken in mg (0.1-1000.0)
- `takenAt` (required): ISO 8601 timestamp when dose was taken
- `notes` (optional): Clinical notes (max 500 characters)

**Auto-Population Behavior**:

When medication has an active dosage pattern:
1. System calls `medication.GetExpectedDosageForDate(takenAt.Date)`
2. Sets `expectedDosage` field automatically
3. Calculates `patternDayNumber` (which day of pattern cycle)
4. Computes variance: `actualDosage - expectedDosage`

**Success Response** (201 Created):
```json
{
  "id": "9c8f4e62-7b3a-4d9f-8e1a-5f2d6c3a9b0e",
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "medicationName": "Warfarin 5mg",
  "dosage": 3.0,
  "expectedDosage": 4.0,
  "takenAt": "2025-01-15T08:30:00Z",
  "patternDayNumber": 1,
  "hasVariance": true,
  "varianceAmount": -1.0,
  "variancePercentage": -25.0,
  "notes": "Took with breakfast",
  "createdAt": "2025-01-15T08:31:00Z"
}
```

**Response Fields**:
- `expectedDosage`: System-calculated expected dose (from pattern or fixed dosage)
- `patternDayNumber`: Which day of pattern cycle (1-based, null if no pattern)
- `hasVariance`: Boolean indicating if actual differs from expected (threshold: >0.01mg)
- `varianceAmount`: Actual - Expected (negative = took less, positive = took more)
- `variancePercentage`: (variance / expected) * 100

**Error Responses**:

400 Bad Request - Validation failed:
```json
{
  "status": 400,
  "title": "Validation failed",
  "detail": "Dosage must be between 0.1 and 1000.0 mg"
}
```

404 Not Found - Medication not found:
```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Medication with ID {id} not found"
}
```

---

### GET /api/medication-logs

Retrieve medication log history with optional filters.

**Authentication**: Required (JWT)

**Query Parameters**:
- `medicationId` (optional): Filter by specific medication UUID
- `startDate` (optional): ISO 8601 date - logs on or after this date
- `endDate` (optional): ISO 8601 date - logs on or before this date
- `includeVariance` (optional, default: false): Filter to logs with variance only
- `varianceThreshold` (optional, default: 0.01): Minimum variance amount to include (mg)
- `page` (optional, default: 1): Page number for pagination
- `pageSize` (optional, default: 20): Results per page (max 100)

**Success Response** (200 OK):
```json
{
  "logs": [
    {
      "id": "9c8f4e62-7b3a-4d9f-8e1a-5f2d6c3a9b0e",
      "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
      "medicationName": "Warfarin 5mg",
      "dosage": 3.0,
      "expectedDosage": 4.0,
      "takenAt": "2025-01-15T08:30:00Z",
      "patternDayNumber": 1,
      "hasVariance": true,
      "varianceAmount": -1.0,
      "variancePercentage": -25.0,
      "notes": "Took with breakfast",
      "createdAt": "2025-01-15T08:31:00Z"
    },
    {
      "id": "8b7e3d51-6a2b-3c8e-7d0f-4e1c5b2a8a9d",
      "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
      "medicationName": "Warfarin 5mg",
      "dosage": 4.0,
      "expectedDosage": 4.0,
      "takenAt": "2025-01-14T08:15:00Z",
      "patternDayNumber": 6,
      "hasVariance": false,
      "varianceAmount": 0.0,
      "variancePercentage": 0.0,
      "createdAt": "2025-01-14T08:16:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 20,
  "hasNextPage": false
}
```

---

### GET /api/medication-logs/{id}

Retrieve a specific medication log entry.

**Authentication**: Required (JWT)

**Success Response** (200 OK):
```json
{
  "id": "9c8f4e62-7b3a-4d9f-8e1a-5f2d6c3a9b0e",
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "medicationName": "Warfarin 5mg",
  "dosage": 3.0,
  "expectedDosage": 4.0,
  "takenAt": "2025-01-15T08:30:00Z",
  "patternDayNumber": 1,
  "hasVariance": true,
  "varianceAmount": -1.0,
  "variancePercentage": -25.0,
  "notes": "Took with breakfast",
  "createdAt": "2025-01-15T08:31:00Z",
  "updatedAt": "2025-01-15T08:31:00Z"
}
```

---

## Variance Tracking

### Variance Calculation

**Formula**:
```
varianceAmount = actualDosage - expectedDosage
variancePercentage = (varianceAmount / expectedDosage) * 100
hasVariance = |varianceAmount| > 0.01
```

**Variance Threshold**: 0.01mg tolerance (prevents false positives from floating-point precision)

**Examples**:

| Expected | Actual | Variance Amount | Variance % | Has Variance |
|----------|--------|----------------|------------|--------------|
| 4.0mg    | 3.0mg  | -1.0mg        | -25%       | ✓ Yes        |
| 4.0mg    | 4.5mg  | +0.5mg        | +12.5%     | ✓ Yes        |
| 4.0mg    | 4.0mg  | 0.0mg         | 0%         | ✗ No         |
| 3.0mg    | 3.01mg | +0.01mg       | +0.33%     | ✗ No         |

### Variance Categories

**Significant Under-Dosing** (< -20%):
- User took substantially less than expected
- May affect INR control
- Healthcare provider should review

**Minor Under-Dosing** (-20% to -5%):
- Small reduction from expected
- Monitor for patterns

**On Target** (-5% to +5%):
- Within acceptable range
- No action needed

**Minor Over-Dosing** (+5% to +20%):
- Small excess from expected
- Monitor for patterns

**Significant Over-Dosing** (> +20%):
- User took substantially more than expected
- Potential safety concern
- Healthcare provider should review immediately

---

## Historical Accuracy (FR-013)

Logs use **temporal pattern lookup** to ensure historical accuracy:

```csharp
// When creating a log entry
var medication = await GetMedicationWithPatterns(log.MedicationId);
var expectedDosage = medication.GetExpectedDosageForDate(log.TakenAt.Date);

// GetExpectedDosageForDate finds the pattern active on THAT specific date
// Not just the current active pattern
```

**Example**:
- Pattern A: Jan 1-15 (4mg, 3mg, 3mg)
- Pattern B: Jan 16+ (4mg, 4mg, 3mg)
- Logging dose for Jan 10 → Uses Pattern A
- Logging dose for Jan 20 → Uses Pattern B

This ensures variance calculations are always relative to the correct expected dosage for that date.

---

## Use Cases

### 1. Log Today's Dose
```
POST /api/medication-logs
{
  "medicationId": "{id}",
  "dosage": 4.0,
  "takenAt": "2025-01-15T08:00:00Z"
}
```
System auto-populates expected dosage from today's pattern.

### 2. Review Adherence History
```
GET /api/medication-logs?medicationId={id}&startDate=2025-01-01&endDate=2025-01-31
```
View all doses taken in January with variance indicators.

### 3. Identify Dosing Errors
```
GET /api/medication-logs?includeVariance=true&varianceThreshold=0.5
```
Find all logs with variance ≥0.5mg (potential dosing mistakes).

### 4. Generate Adherence Report
```
GET /api/medication-logs?medicationId={id}&startDate=2025-01-01&pageSize=90
```
Fetch 90 days of logs to calculate:
- Adherence rate: `(logs without variance / total logs) * 100`
- Average variance: `mean(|varianceAmount|)`
- Consistency score: `1 - (stddev(variance) / mean(expectedDosage))`

---

## Integration with Patterns

### Auto-Population Flow

```javascript
// Frontend: User opens log form
const expectedDosage = await fetch(
    `/api/medications/${medicationId}/patterns/active`
).then(r => r.json()).then(p => p.todaysDosage);

// Frontend: Pre-fill dosage field
form.dosage = expectedDosage;

// Backend: Auto-populate on save
// Even if frontend doesn't send expectedDosage,
// backend calculates it from pattern for historical accuracy
```

### Pattern Change Impact

When a pattern changes:
- **Future logs**: Use new pattern's expected dosage
- **Historical logs**: Still reference old pattern (temporal accuracy)
- **Variance recalculation**: Not needed - variance is stored, not computed on read

---

## Code Examples

### Create Log (C#)
```csharp
var log = new CreateMedicationLogRequest
{
    MedicationId = medicationId,
    Dosage = 3.0m,
    TakenAt = DateTime.Now,
    Notes = "Took with breakfast"
};

var response = await httpClient.PostAsJsonAsync("/api/medication-logs", log);
var created = await response.Content.ReadFromJsonAsync<MedicationLogResponse>();

if (created.HasVariance)
{
    Console.WriteLine($"⚠️ Variance: {created.VarianceAmount}mg ({created.VariancePercentage}%)");
}
```

### Fetch Variance Report (JavaScript)
```javascript
async function getVarianceReport(medicationId, days = 30) {
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - days);
    
    const response = await fetch(
        `/api/medication-logs?` +
        `medicationId=${medicationId}&` +
        `startDate=${startDate.toISOString().split('T')[0]}&` +
        `pageSize=100`
    );
    
    const data = await response.json();
    
    const varianceLogs = data.logs.filter(log => log.hasVariance);
    const adherenceRate = 
        ((data.totalCount - varianceLogs.length) / data.totalCount) * 100;
    
    return {
        totalLogs: data.totalCount,
        varianceLogs: varianceLogs.length,
        adherenceRate: adherenceRate.toFixed(1),
        avgVariance: varianceLogs.reduce((sum, log) => 
            sum + Math.abs(log.varianceAmount), 0) / varianceLogs.length
    };
}
```

---

## Performance Considerations

- **Auto-population**: Single additional query to fetch medication patterns
- **Variance calculation**: Computed on write, not on read (performance optimization)
- **Filtering**: Indexed queries on (MedicationId, TakenAt, HasVariance)
- **Typical response time**: 25-40ms for filtered list queries

---

## Related Endpoints

- **Patterns API**: [medication-patterns-api.md](medication-patterns-api.md) - Define dosage patterns
- **Schedule API**: [medication-schedule-api.md](medication-schedule-api.md) - View expected future dosages

---

## Medical Disclaimer

⚠️ **Medical Disclaimer**: Medication logs are for personal record-keeping only. If you have questions about your medication dosage, missed doses, or notice unexpected variance, contact your healthcare provider immediately. Do not adjust your medication without medical supervision.
