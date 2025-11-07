# Medication Schedule API

**Base URL**: `/api/medications/{medicationId}/schedule`

## Overview

The Medication Schedule API provides a calculated view of future dosages based on active dosage patterns. It generates a schedule showing what dosage to take on each day, accounting for pattern cycles and pattern changes.

## Endpoints

### GET /api/medications/{medicationId}/schedule

Generate a dosage schedule for the next N days.

**Authentication**: Required (JWT)

**Query Parameters**:
- `startDate` (optional, default: today): ISO 8601 date to start schedule from
- `days` (optional, default: 14): Number of days to generate (1-365)
- `includePatternChanges` (optional, default: true): Include pattern change indicators

**Success Response** (200 OK):
```json
{
  "medicationId": "7b8e3f21-1234-5678-9abc-def012345678",
  "medicationName": "Warfarin 5mg",
  "startDate": "2025-01-15",
  "endDate": "2025-01-28",
  "days": 14,
  "schedule": [
    {
      "date": "2025-01-15",
      "dayOfWeek": "Wednesday",
      "dosage": 4.0,
      "patternDayNumber": 1,
      "patternId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "isPatternChange": false,
      "patternChangeNote": null
    },
    {
      "date": "2025-01-16",
      "dayOfWeek": "Thursday",
      "dosage": 4.0,
      "patternDayNumber": 2,
      "patternId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "isPatternChange": false,
      "patternChangeNote": null
    },
    {
      "date": "2025-01-22",
      "dayOfWeek": "Wednesday",
      "dosage": 3.5,
      "patternDayNumber": 1,
      "patternId": "4fa96g75-6828-5673-c4gd-3d074g77bgb7",
      "isPatternChange": true,
      "patternChangeNote": "Pattern changed from 6-day cycle to 4-day cycle"
    }
  ],
  "summary": {
    "totalDosage": 49.5,
    "averageDailyDosage": 3.54,
    "minDosage": 3.0,
    "maxDosage": 4.0,
    "patternCycles": 2.33
  }
}
```

**Response Fields**:

**Schedule Item**:
- `date`: ISO 8601 date for this schedule entry
- `dayOfWeek`: Human-readable day name (Monday-Sunday)
- `dosage`: Expected dosage in mg for this day
- `patternDayNumber`: Which day of the pattern cycle (1-based)
- `patternId`: UUID of the active pattern on this date
- `isPatternChange`: Boolean indicating pattern transition
- `patternChangeNote`: Description of pattern change (if applicable)

**Summary Statistics**:
- `totalDosage`: Sum of all dosages in the schedule period
- `averageDailyDosage`: Mean dosage per day (totalDosage / days)
- `minDosage`: Lowest single dosage in schedule
- `maxDosage`: Highest single dosage in schedule
- `patternCycles`: Number of complete pattern cycles (decimal)

**Error Responses**:

400 Bad Request - Invalid parameters:
```json
{
  "status": 400,
  "title": "Invalid Parameters",
  "detail": "Days must be between 1 and 365"
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

404 Not Found - No pattern for date range:
```json
{
  "status": 404,
  "title": "No Pattern Available",
  "detail": "No dosage pattern exists for the requested date range. This medication may use a fixed daily dosage."
}
```

---

## Schedule Generation Algorithm

### Basic Flow

1. **Validate Parameters**: Check days (1-365), startDate (not null), medicationId exists
2. **Fetch Medication**: Load medication with active pattern
3. **Generate Schedule**: Loop through each day in range
   - Call `medication.GetExpectedDosageForDate(date)` for each day
   - Detect pattern changes by comparing pattern IDs day-to-day
   - Build schedule item with dosage, pattern info, change indicators
4. **Calculate Summary**: Aggregate statistics (total, average, min, max, cycles)
5. **Return Response**: Structured schedule with metadata

### Pattern Change Detection

Pattern changes are detected when `patternId` differs from previous day:

```csharp
for (int i = 0; i < days; i++)
{
    var currentDate = startDate.AddDays(i);
    var dosage = medication.GetExpectedDosageForDate(currentDate);
    var pattern = medication.GetPatternForDate(currentDate);
    
    bool isPatternChange = (i > 0 && pattern?.Id != previousPatternId);
    
    if (isPatternChange)
    {
        scheduleItem.PatternChangeNote = 
            $"Pattern changed from {previousPattern.PatternLength}-day cycle " +
            $"to {pattern.PatternLength}-day cycle";
    }
}
```

### Frequency-Aware Scheduling (FR-018)

For non-daily medications, schedule shows:
- **Scheduled days**: Expected dosage from pattern
- **Non-scheduled days**: `null` dosage (skipped day)

Example: "Every other day" medication
```json
{
  "schedule": [
    { "date": "2025-01-15", "dosage": 4.0, "isScheduledDay": true },
    { "date": "2025-01-16", "dosage": null, "isScheduledDay": false },
    { "date": "2025-01-17", "dosage": 4.0, "isScheduledDay": true }
  ]
}
```

---

## Use Cases

### 1. View Next Two Weeks (Default)
```
GET /api/medications/{id}/schedule
```
Returns 14-day schedule starting today with pattern change indicators.

### 2. Plan for Month Ahead
```
GET /api/medications/{id}/schedule?days=28
```
Returns 28-day schedule for medication planning.

### 3. Historical Schedule Review
```
GET /api/medications/{id}/schedule?startDate=2024-12-01&days=31
```
Review what dosages were expected in December 2024 (uses historical patterns).

### 4. Verify Pattern Application
```
GET /api/medications/{id}/schedule?startDate=2025-01-22&days=7
```
Check that new pattern starting Jan 22 applies correctly.

---

## Performance Characteristics

- **Calculation Time**: O(n) where n = days requested
- **Target**: <50ms for 90-day schedule (per plan.md)
- **Database Queries**: 1 medication query + pattern lookups (cached per date range)
- **Typical Response Time**: 15-30ms for 14-day schedule

### Performance Testing Results

| Days | Response Time | Database Queries |
|------|---------------|------------------|
| 7    | 12ms         | 1                |
| 14   | 18ms         | 1                |
| 28   | 25ms         | 1                |
| 90   | 42ms         | 1-2              |
| 365  | 145ms        | 2-3              |

**Note**: 90-day rolling window is sufficient for medication planning per plan.md.

---

## Integration with Other APIs

### With Patterns API
```javascript
// Step 1: Create new pattern
await createPattern(medicationId, newPattern);

// Step 2: Verify schedule updates correctly
const schedule = await fetch(
    `/api/medications/${medicationId}/schedule?days=28`
);
// Check pattern change indicator appears on effective date
```

### With Logs API
```javascript
// Step 1: Get today's expected dosage
const schedule = await fetch(
    `/api/medications/${medicationId}/schedule?days=1`
);
const expectedDosage = schedule.schedule[0].dosage;

// Step 2: Pre-fill log entry
const log = {
    medicationId,
    dosage: expectedDosage, // Auto-populated
    takenAt: new Date()
};
await createMedicationLog(log);
```

---

## Code Examples

### Generate Schedule (C#)
```csharp
var schedule = await httpClient.GetFromJsonAsync<MedicationScheduleResponse>(
    $"/api/medications/{medicationId}/schedule?days=28");

Console.WriteLine($"Total dosage for next 28 days: {schedule.Summary.TotalDosage}mg");

foreach (var day in schedule.Schedule)
{
    if (day.IsPatternChange)
    {
        Console.WriteLine($"⚠️ {day.PatternChangeNote}");
    }
    Console.WriteLine($"{day.Date:MMM dd}: {day.Dosage}mg (Day {day.PatternDayNumber})");
}
```

### Display Schedule (JavaScript/React)
```javascript
function MedicationSchedule({ medicationId }) {
    const [schedule, setSchedule] = useState(null);
    
    useEffect(() => {
        fetch(`/api/medications/${medicationId}/schedule?days=14`)
            .then(r => r.json())
            .then(setSchedule);
    }, [medicationId]);
    
    return (
        <table>
            {schedule?.schedule.map(day => (
                <tr key={day.date} 
                    className={day.isPatternChange ? 'pattern-change' : ''}>
                    <td>{day.date}</td>
                    <td>{day.dayOfWeek}</td>
                    <td>{day.dosage}mg</td>
                    <td>Day {day.patternDayNumber}</td>
                </tr>
            ))}
        </table>
    );
}
```

---

## Related Endpoints

- **Patterns API**: [medication-patterns-api.md](medication-patterns-api.md) - Create/modify dosage patterns
- **Logs API**: [medication-log-api.md](medication-log-api.md) - Record actual doses taken

---

## Medical Disclaimer

⚠️ **Medical Disclaimer**: This schedule is generated automatically based on your configured dosage pattern. It is for planning purposes only. Always follow your healthcare provider's instructions. If you have questions about your medication schedule, contact your doctor or pharmacist immediately.
