# API Contract: Medication Schedule

**Feature**: 005-medicine-schedule-to  
**Base URL**: `/api/medications/{medicationId}/schedule`  
**Date**: 2025-11-03

## Overview

This API calculates future dosage schedules for medications with pattern-based dosing, enabling users to view their upcoming medication plan for 1-4 weeks.

---

## Endpoints

### 1. Get Future Dosage Schedule

Calculates and returns the dosage schedule for a specified number of days, based on the active dosage pattern.

**Request**:
```http
GET /api/medications/{medicationId}/schedule?startDate=2025-11-04&days=28
Authorization: Bearer {jwt_token}
```

**Path Parameters**:
- `medicationId` (integer, required): ID of the medication

**Query Parameters**:
- `startDate` (date, optional, default: today): Starting date for schedule calculation
- `days` (integer, optional, default: 14, min: 1, max: 365): Number of days to generate
- `includePatternChanges` (boolean, optional, default: true): Include metadata about pattern transitions

**Success Response** (200 OK):
```json
{
  "medicationId": 123,
  "medicationName": "Warfarin",
  "dosageUnit": "mg",
  "startDate": "2025-11-04",
  "endDate": "2025-12-01",
  "totalDays": 28,
  "currentPattern": {
    "id": 42,
    "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
    "patternLength": 6,
    "startDate": "2025-11-04",
    "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)"
  },
  "summary": {
    "totalDosage": 101.0,
    "averageDailyDosage": 3.61,
    "minDosage": 3.0,
    "maxDosage": 4.0,
    "patternCycles": 4.67
  },
  "schedule": [
    {
      "date": "2025-11-04",
      "dayOfWeek": "Monday",
      "dosage": 4.0,
      "patternDay": 1,
      "patternLength": 6,
      "isPatternChange": true,
      "patternChangeNote": "New pattern starts: 4mg, 4mg, 3mg, 4mg, 3mg, 3mg"
    },
    {
      "date": "2025-11-05",
      "dayOfWeek": "Tuesday",
      "dosage": 4.0,
      "patternDay": 2,
      "patternLength": 6,
      "isPatternChange": false
    },
    {
      "date": "2025-11-06",
      "dayOfWeek": "Wednesday",
      "dosage": 3.0,
      "patternDay": 3,
      "patternLength": 6,
      "isPatternChange": false
    }
    // ... 25 more days
  ]
}
```

**Response Fields**:

`ScheduleResponse`:
- `medicationId` (integer): Medication ID
- `medicationName` (string): Display name
- `dosageUnit` (string): Unit (e.g., "mg")
- `startDate` (date): Schedule start date
- `endDate` (date): Schedule end date
- `totalDays` (integer): Number of days in schedule
- `currentPattern` (object): Active pattern details
- `summary` (object): Statistical summary
- `schedule` (array): Day-by-day dosage list

`ScheduleSummary`:
- `totalDosage` (decimal): Sum of all dosages in period
- `averageDailyDosage` (decimal): Average per day
- `minDosage` (decimal): Lowest dosage in period
- `maxDosage` (decimal): Highest dosage in period
- `patternCycles` (decimal): Number of complete + partial cycles

`ScheduleEntry`:
- `date` (date): ISO 8601 date
- `dayOfWeek` (string): "Monday", "Tuesday", etc.
- `dosage` (decimal): Dosage for this day
- `patternDay` (integer): Position in pattern cycle (1-based)
- `patternLength` (integer): Total length of pattern
- `isPatternChange` (boolean): True if new pattern starts on this day
- `patternChangeNote` (string, optional): Description of pattern change

**Error Responses**:

```json
// 400 Bad Request - Invalid parameters
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid request parameters",
  "status": 400,
  "errors": {
    "days": ["Days must be between 1 and 365"],
    "startDate": ["Start date cannot be more than 1 year in the past"]
  }
}

// 404 Not Found - No pattern found
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "No dosage pattern found",
  "status": 404,
  "detail": "This medication does not have an active dosage pattern for the requested date range."
}

// 403 Forbidden - Not user's medication
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Access denied",
  "status": 403,
  "detail": "You do not have permission to view this medication's schedule."
}
```

---

### 2. Get Schedule for Specific Date

Retrieves the dosage for a single specific date.

**Request**:
```http
GET /api/medications/{medicationId}/schedule/date?date=2025-12-25
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
- `date` (date, required): Date to calculate dosage for

**Success Response** (200 OK):
```json
{
  "medicationId": 123,
  "medicationName": "Warfarin",
  "date": "2025-12-25",
  "dayOfWeek": "Wednesday",
  "dosage": 3.0,
  "dosageUnit": "mg",
  "patternDay": 4,
  "patternLength": 6,
  "pattern": {
    "id": 42,
    "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
    "startDate": "2025-11-04"
  }
}
```

**Error Responses**:
```json
// 404 Not Found - No pattern active on that date
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "No pattern for date",
  "status": 404,
  "detail": "No dosage pattern was active on 2025-12-25."
}
```

---

### 3. Get Schedule with Pattern Transitions

Retrieves schedule spanning multiple patterns (e.g., when patterns change mid-schedule).

**Request**:
```http
GET /api/medications/{medicationId}/schedule/detailed?startDate=2025-11-01&days=60
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
- `startDate` (date, optional, default: today)
- `days` (integer, optional, default: 14, max: 365)

**Success Response** (200 OK):
```json
{
  "medicationId": 123,
  "medicationName": "Warfarin",
  "startDate": "2025-11-01",
  "endDate": "2025-12-30",
  "totalDays": 60,
  "patternsUsed": [
    {
      "id": 41,
      "displayPattern": "5mg, 5mg, 4mg (3-day cycle)",
      "startDate": "2025-09-01",
      "endDate": "2025-11-03",
      "daysInSchedule": 3
    },
    {
      "id": 42,
      "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
      "startDate": "2025-11-04",
      "endDate": null,
      "daysInSchedule": 57
    }
  ],
  "schedule": [
    {
      "date": "2025-11-01",
      "dosage": 5.0,
      "patternId": 41,
      "patternDay": 1,
      "isPatternChange": false
    },
    {
      "date": "2025-11-04",
      "dosage": 4.0,
      "patternId": 42,
      "patternDay": 1,
      "isPatternChange": true,
      "patternChangeNote": "Pattern changed from '5mg, 5mg, 4mg' to '4mg, 4mg, 3mg, 4mg, 3mg, 3mg'"
    }
    // ... remaining days
  ]
}
```

**Use Case**: Viewing historical + future schedule when patterns have changed recently.

---

## DTOs

### ScheduleResponse

```csharp
public class ScheduleResponse
{
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string DosageUnit { get; set; } = "mg";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public PatternSummaryDto CurrentPattern { get; set; } = null!;
    public ScheduleSummary Summary { get; set; } = null!;
    public List<ScheduleEntry> Schedule { get; set; } = new();
}

public class ScheduleSummary
{
    public decimal TotalDosage { get; set; }
    public decimal AverageDailyDosage { get; set; }
    public decimal MinDosage { get; set; }
    public decimal MaxDosage { get; set; }
    public decimal PatternCycles { get; set; }
}

public class ScheduleEntry
{
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public decimal Dosage { get; set; }
    public int PatternDay { get; set; }
    public int PatternLength { get; set; }
    public bool IsPatternChange { get; set; }
    public string? PatternChangeNote { get; set; }
}

public class PatternSummaryDto
{
    public int Id { get; set; }
    public List<decimal> PatternSequence { get; set; } = new();
    public int PatternLength { get; set; }
    public DateTime StartDate { get; set; }
    public string DisplayPattern { get; set; } = string.Empty;
}
```

---

## Performance Considerations

### Calculation Performance

| Days | Calculation Time | Cache Strategy |
|------|------------------|----------------|
| 1-14 | < 5ms | Client-side cache (1 hour) |
| 15-28 | < 10ms | Client-side cache (1 hour) |
| 29-90 | < 50ms | Client-side cache (4 hours) |
| 91-365 | < 200ms | Server-side cache (24 hours) |

### Caching Headers

Responses include:
```http
Cache-Control: private, max-age=3600
ETag: "pattern-42-20251104"
Last-Modified: Mon, 04 Nov 2025 10:00:00 GMT
```

- Client can cache for 1 hour
- ETag includes pattern ID and start date for cache invalidation
- Modified date changes when pattern is updated

---

## Authorization

- ✅ Valid JWT bearer token required
- ✅ User must own the medication

---

## Rate Limiting

- Standard tier: 200 requests/minute per user
- Premium tier: 2000 requests/minute per user

(Higher limits than pattern CRUD endpoints due to read-only nature)

---

## Use Cases

### Use Case 1: Display Weekly Calendar View

**Scenario**: User wants to see this week's medication schedule.

```http
GET /api/medications/123/schedule?days=7
```

**UI Display**:
```
Monday    11/04 → 4mg  (Day 1 of 6)
Tuesday   11/05 → 4mg  (Day 2 of 6)
Wednesday 11/06 → 3mg  (Day 3 of 6)
Thursday  11/07 → 4mg  (Day 4 of 6)
Friday    11/08 → 3mg  (Day 5 of 6)
Saturday  11/09 → 3mg  (Day 6 of 6)
Sunday    11/10 → 4mg  (Day 1 of 6) ← Pattern repeats
```

---

### Use Case 2: Planning Medication Refill

**Scenario**: User wants to calculate how much medication they need for the next 30 days.

```http
GET /api/medications/123/schedule?days=30
```

**Calculation**:
```
Total Dosage: 108mg
Average: 3.6mg/day
Pattern Cycles: 5 complete cycles
```

---

### Use Case 3: Checking Tomorrow's Dose

**Scenario**: User logs dose tonight and wants to know tomorrow's expected dose.

```http
GET /api/medications/123/schedule/date?date=2025-11-05
```

**Response**:
```json
{
  "date": "2025-11-05",
  "dosage": 4.0,
  "patternDay": 2
}
```

---

## Testing Examples

### cURL: Get 14-day Schedule

```bash
curl -X GET "https://api.bloodthinner.app/api/medications/123/schedule?days=14" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

### PowerShell: Get Specific Date

```powershell
$headers = @{
    "Authorization" = "Bearer $env:JWT_TOKEN"
}

$date = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")

Invoke-RestMethod -Uri "https://api.bloodthinner.app/api/medications/123/schedule/date?date=$date" `
    -Method GET `
    -Headers $headers
```

### C# HttpClient: Get Monthly Schedule

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var response = await client.GetAsync(
    $"https://api.bloodthinner.app/api/medications/123/schedule?days=30");

var schedule = await response.Content.ReadFromJsonAsync<ScheduleResponse>();

Console.WriteLine($"Total dosage for 30 days: {schedule.Summary.TotalDosage}mg");
Console.WriteLine($"Average daily: {schedule.Summary.AverageDailyDosage:F2}mg");

foreach (var entry in schedule.Schedule)
{
    Console.WriteLine($"{entry.Date:yyyy-MM-dd}: {entry.Dosage}mg (Day {entry.PatternDay}/{entry.PatternLength})");
}
```

---

## OpenAPI Schema

```yaml
paths:
  /api/medications/{medicationId}/schedule:
    get:
      summary: Get future dosage schedule
      tags: [Medication Schedule]
      security:
        - BearerAuth: []
      parameters:
        - name: medicationId
          in: path
          required: true
          schema:
            type: integer
        - name: startDate
          in: query
          schema:
            type: string
            format: date
            default: today
        - name: days
          in: query
          schema:
            type: integer
            minimum: 1
            maximum: 365
            default: 14
        - name: includePatternChanges
          in: query
          schema:
            type: boolean
            default: true
      responses:
        '200':
          description: Schedule calculated successfully
          headers:
            Cache-Control:
              schema:
                type: string
                example: "private, max-age=3600"
            ETag:
              schema:
                type: string
                example: "pattern-42-20251104"
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ScheduleResponse'
        '400':
          $ref: '#/components/responses/ValidationError'
        '404':
          $ref: '#/components/responses/NotFound'

  /api/medications/{medicationId}/schedule/date:
    get:
      summary: Get dosage for specific date
      tags: [Medication Schedule]
      security:
        - BearerAuth: []
      parameters:
        - name: medicationId
          in: path
          required: true
          schema:
            type: integer
        - name: date
          in: query
          required: true
          schema:
            type: string
            format: date
      responses:
        '200':
          description: Dosage for date retrieved
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SingleDateScheduleResponse'
        '404':
          $ref: '#/components/responses/NotFound'
```

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-11-03 | 1.0 | Initial API specification |
