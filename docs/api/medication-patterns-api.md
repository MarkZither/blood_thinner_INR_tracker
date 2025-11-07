# Medication Patterns API

**Base URL**: `/api/medications/{medicationId}/patterns`

## Overview

The Medication Patterns API enables management of variable dosage patterns for blood thinner medications. Patterns define cyclical dosage schedules (e.g., "4mg, 4mg, 3mg") that repeat indefinitely until a new pattern is created.

## Endpoints

### POST /api/medications/{medicationId}/patterns

Create a new dosage pattern for a medication.

**Authentication**: Required (JWT)

**Request Body**:
```json
{
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "startDate": "2025-01-15",
  "endDate": null,
  "notes": "Adjusted after INR test showed 2.8",
  "closePreviousPattern": true
}
```

**Request Fields**:
- `patternSequence` (required): Array of decimal dosages (1-365 values, 0.1-1000.0 mg range)
- `startDate` (required): ISO 8601 date when pattern becomes effective (max 1 year in past)
- `endDate` (optional): ISO 8601 date when pattern ends (must be >= startDate)
- `notes` (optional): Clinical notes about the pattern change (max 500 characters)
- `closePreviousPattern` (optional, default: true): Automatically close previous active pattern

**Validation Rules**:
- Pattern length: 1-365 values (FR-002)
- Individual dosage: 0.1-1000.0 mg (FR-016)
- Backdating: Warning for >7 days in past (FR-011)
- Single-value: Warning when count == 1 (suggests fixed dose)
- Long pattern: Warning when count > 20 (unusual)
- Medication-specific: Warfarin max 20mg per dose

**Success Response** (201 Created):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-01-15",
  "endDate": null,
  "notes": "Adjusted after INR test showed 2.8",
  "isActive": true,
  "averageDosage": 3.5,
  "createdAt": "2025-01-14T15:30:00Z"
}
```

**Error Responses**:

400 Bad Request - Validation failed:
```json
{
  "status": 400,
  "title": "Validation failed",
  "detail": "PatternSequence must contain between 1 and 365 values; Dosage 25 is out of range (0.1-1000mg)"
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

409 Conflict - Overlapping pattern dates:
```json
{
  "status": 409,
  "title": "Pattern Overlap Detected",
  "detail": "An active pattern already exists for this medication. Set closePreviousPattern=true to automatically close it."
}
```

---

### GET /api/medications/{medicationId}/patterns/active

Retrieve the currently active dosage pattern.

**Authentication**: Required (JWT)

**Success Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-01-15",
  "endDate": null,
  "notes": "Adjusted after INR test showed 2.8",
  "isActive": true,
  "averageDosage": 3.5,
  "currentDayNumber": 3,
  "todaysDosage": 3.0,
  "createdAt": "2025-01-14T15:30:00Z"
}
```

**Error Responses**:

404 Not Found - No active pattern:
```json
{
  "status": 404,
  "title": "No Active Pattern",
  "detail": "This medication does not have an active dosage pattern"
}
```

---

### GET /api/medications/{medicationId}/patterns

Retrieve pattern history with temporal ordering.

**Authentication**: Required (JWT)

**Query Parameters**:
- `page` (optional, default: 1): Page number for pagination
- `pageSize` (optional, default: 10): Results per page (max 100)
- `activeOnly` (optional, default: false): Filter to active patterns only

**Success Response** (200 OK):
```json
{
  "patterns": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
      "patternLength": 6,
      "startDate": "2025-01-15",
      "endDate": null,
      "isActive": true,
      "averageDosage": 3.5,
      "createdAt": "2025-01-14T15:30:00Z"
    },
    {
      "id": "2ea74e53-4606-3451-a2eb-1c852e55ae95",
      "patternSequence": [4.0, 3.0, 3.0],
      "patternLength": 3,
      "startDate": "2024-12-01",
      "endDate": "2025-01-14",
      "isActive": false,
      "averageDosage": 3.33,
      "createdAt": "2024-11-30T10:00:00Z"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 10,
  "hasNextPage": false
}
```

---

## Pattern Calculation

### Day Number Calculation

Pattern day number is calculated using modulo arithmetic:
```
patternDay = (daysSinceStart % patternLength) + 1
```

**Example**: 6-day pattern starting Jan 15:
- Jan 15: Day 1 (0 days since start → 0 % 6 = 0 → Day 1)
- Jan 20: Day 6 (5 days since start → 5 % 6 = 5 → Day 6)
- Jan 21: Day 1 (6 days since start → 6 % 6 = 0 → Day 1)

### Frequency-Aware Calculation (FR-018)

For non-daily medications (e.g., "Every other day"), pattern applies to **scheduled days only**:

**Example**: Pattern [4, 3, 3] for medication taken every other day
- Day 1 (scheduled): 4mg
- Day 2 (skip): No dose
- Day 3 (scheduled): 3mg
- Day 4 (skip): No dose
- Day 5 (scheduled): 3mg

See `Medication.GetExpectedDosageForDate()` implementation for details.

---

## Business Rules

### Pattern Overlap Prevention (FR-009)

Only one active pattern per medication at any time:
- Setting `closePreviousPattern=true` automatically closes previous pattern
- `EndDate` of old pattern = `StartDate` of new pattern - 1 day
- Manual overlap detection returns 409 Conflict if overlaps exist

### Backdating Support (FR-011)

Patterns can start in the past (max 1 year):
- UI shows confirmation dialog for >7 days backdating
- Historical logs use correct pattern for their date via `GetPatternForDate(date)`
- API accepts any valid past date without rejection

### Medication-Specific Validation (FR-021)

Safety checks based on medication type:
- **Warfarin**: Max 20mg per dose warning
- Extensible via `ValidateMedicationSpecificRules()` method

---

## Related Endpoints

- **Schedule API**: [medication-schedule-api.md](medication-schedule-api.md) - View future dosage schedule
- **Logs API**: [medication-log-api.md](medication-log-api.md) - Record doses with variance tracking

---

## Code Examples

### Create Pattern (C#)
```csharp
var request = new CreateDosagePatternRequest
{
    PatternSequence = new[] { 4.0m, 4.0m, 3.0m, 4.0m, 3.0m, 3.0m },
    StartDate = DateTime.Today.AddDays(1),
    ClosePreviousPattern = true,
    Notes = "Adjusted after INR 2.8"
};

var response = await httpClient.PostAsJsonAsync(
    $"/api/medications/{medicationId}/patterns", 
    request);
```

### Get Active Pattern (JavaScript)
```javascript
const response = await fetch(
    `/api/medications/${medicationId}/patterns/active`,
    {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    }
);
const pattern = await response.json();
console.log(`Today's dosage: ${pattern.todaysDosage}mg`);
```

---

## Performance Considerations

- Pattern calculation: O(1) using modulo arithmetic
- History queries: Indexed on (MedicationId, StartDate, EndDate)
- Active pattern lookup: Filtered index on `EndDate IS NULL`

---

## Medical Disclaimer

⚠️ **Medical Disclaimer**: This API is for informational purposes only. Always consult your healthcare provider before making changes to your medication dosage. Pattern data should be reviewed by a licensed medical professional.
