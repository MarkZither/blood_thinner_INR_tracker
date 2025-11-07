# API Contract: Medication Dosage Patterns

**Feature**: 005-medicine-schedule-to  
**Base URL**: `/api/medications/{medicationId}/patterns`  
**Date**: 2025-11-03

## Overview

This API enables CRUD operations on medication dosage patterns, supporting temporal tracking of variable-dosage schedules (e.g., "4mg, 4mg, 3mg" repeating cycles).

---

## Endpoints

### 1. Create New Dosage Pattern

Creates a new dosage pattern for a medication. Optionally closes the previous active pattern by setting its `EndDate`.

**Request**:
```http
POST /api/medications/{medicationId}/patterns
Content-Type: application/json
Authorization: Bearer {jwt_token}

{
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "startDate": "2025-11-04",
  "endDate": null,
  "notes": "Reduced winter dosing pattern",
  "closePreviousPattern": true
}
```

**Path Parameters**:
- `medicationId` (integer, required): ID of the medication

**Request Body** (`CreateDosagePatternRequest`):
```csharp
public class CreateDosagePatternRequest
{
    /// <summary>
    /// Array of dosages forming the repeating pattern.
    /// Example: [4.0, 4.0, 3.0] for 3-day cycle.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Pattern must have at least 1 dosage")]
    [MaxLength(365, ErrorMessage = "Pattern cannot exceed 365 dosages")]
    public List<decimal> PatternSequence { get; set; } = new();

    /// <summary>
    /// Date when this pattern becomes active.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Optional end date. Leave null for ongoing pattern.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional user notes about this pattern.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// If true, sets the EndDate of the current active pattern to the day before this pattern's StartDate.
    /// </summary>
    public bool ClosePreviousPattern { get; set; } = true;
}
```

**Validation Rules**:
- Each dosage: `0.1 <= value <= 1000` (general)
- Warfarin medications: Each dosage `<= 20mg`
- StartDate: Cannot be more than 1 year in the past
- EndDate: Must be `>= StartDate` if provided
- Pattern must not overlap with other active patterns unless `closePreviousPattern = true`

**Success Response** (201 Created):
```json
{
  "id": 42,
  "medicationId": 123,
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-11-04",
  "endDate": null,
  "notes": "Reduced winter dosing pattern",
  "isActive": true,
  "averageDosage": 3.67,
  "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
  "createdDate": "2025-11-03T10:30:00Z",
  "modifiedDate": "2025-11-03T10:30:00Z"
}
```

**Error Responses**:

```json
// 400 Bad Request - Validation failure
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "PatternSequence[2]": ["Warfarin dosage must be ≤ 20mg"],
    "StartDate": ["Start date cannot overlap with existing active pattern"]
  }
}

// 404 Not Found - Medication doesn't exist
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Medication not found",
  "status": 404,
  "detail": "Medication with ID 123 does not exist or you don't have access."
}

// 403 Forbidden - Not user's medication
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Access denied",
  "status": 403,
  "detail": "You do not have permission to modify this medication."
}
```

---

### 2. Get Dosage Pattern History

Retrieves all dosage patterns for a medication, ordered by start date (newest first).

**Request**:
```http
GET /api/medications/{medicationId}/patterns?activeOnly=false&limit=10&offset=0
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
- `activeOnly` (boolean, optional, default: false): If true, returns only patterns where `EndDate` is null
- `limit` (integer, optional, default: 10, max: 100): Number of patterns to return
- `offset` (integer, optional, default: 0): Pagination offset

**Success Response** (200 OK):
```json
{
  "medicationId": 123,
  "medicationName": "Warfarin",
  "totalCount": 3,
  "patterns": [
    {
      "id": 42,
      "medicationId": 123,
      "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
      "patternLength": 6,
      "startDate": "2025-11-04",
      "endDate": null,
      "notes": "Reduced winter dosing pattern",
      "isActive": true,
      "averageDosage": 3.67,
      "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
      "createdDate": "2025-11-03T10:30:00Z",
      "modifiedDate": "2025-11-03T10:30:00Z"
    },
    {
      "id": 41,
      "medicationId": 123,
      "patternSequence": [5.0, 5.0, 4.0],
      "patternLength": 3,
      "startDate": "2025-09-01",
      "endDate": "2025-11-03",
      "notes": "Summer dosing pattern",
      "isActive": false,
      "averageDosage": 4.67,
      "displayPattern": "5mg, 5mg, 4mg (3-day cycle)",
      "createdDate": "2025-09-01T08:00:00Z",
      "modifiedDate": "2025-11-03T10:30:00Z"
    }
  ]
}
```

**Error Responses**:
```json
// 404 Not Found
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Medication not found",
  "status": 404
}
```

---

### 3. Get Active Dosage Pattern

Retrieves the currently active dosage pattern (where `EndDate` is null).

**Request**:
```http
GET /api/medications/{medicationId}/patterns/active
Authorization: Bearer {jwt_token}
```

**Success Response** (200 OK):
```json
{
  "id": 42,
  "medicationId": 123,
  "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-11-04",
  "endDate": null,
  "notes": "Reduced winter dosing pattern",
  "isActive": true,
  "averageDosage": 3.67,
  "displayPattern": "4mg, 4mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
  "todaysDosage": 4.0,
  "todaysPatternDay": 3,
  "createdDate": "2025-11-03T10:30:00Z",
  "modifiedDate": "2025-11-03T10:30:00Z"
}
```

**Response Includes Extra Fields**:
- `todaysDosage` (decimal): Calculated dosage for today
- `todaysPatternDay` (integer): Today's position in the pattern cycle (1-based)

**Error Responses**:
```json
// 404 Not Found - No active pattern
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "No active pattern found",
  "status": 404,
  "detail": "This medication does not have an active dosage pattern."
}
```

---

### 4. Update Dosage Pattern

Updates an existing dosage pattern. Can only update patterns with `EndDate = null` (active patterns).

**Request**:
```http
PUT /api/medications/{medicationId}/patterns/{patternId}
Content-Type: application/json
Authorization: Bearer {jwt_token}

{
  "patternSequence": [4.0, 3.0, 3.0, 4.0, 3.0, 3.0],
  "notes": "Adjusted pattern based on INR results",
  "endDate": "2025-12-31"
}
```

**Path Parameters**:
- `medicationId` (integer, required)
- `patternId` (integer, required): ID of the pattern to update

**Request Body** (`UpdateDosagePatternRequest`):
```csharp
public class UpdateDosagePatternRequest
{
    /// <summary>
    /// Updated pattern sequence. If null, existing sequence is retained.
    /// </summary>
    [MinLength(1)]
    [MaxLength(365)]
    public List<decimal>? PatternSequence { get; set; }

    /// <summary>
    /// Updated notes. If null, existing notes are retained.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Set or update the end date. Use "9999-12-31" to keep pattern ongoing.
    /// </summary>
    public DateTime? EndDate { get; set; }
}
```

**Success Response** (200 OK):
```json
{
  "id": 42,
  "medicationId": 123,
  "patternSequence": [4.0, 3.0, 3.0, 4.0, 3.0, 3.0],
  "patternLength": 6,
  "startDate": "2025-11-04",
  "endDate": "2025-12-31",
  "notes": "Adjusted pattern based on INR results",
  "isActive": true,
  "averageDosage": 3.5,
  "displayPattern": "4mg, 3mg, 3mg, 4mg, 3mg, 3mg (6-day cycle)",
  "createdDate": "2025-11-03T10:30:00Z",
  "modifiedDate": "2025-11-03T14:15:00Z"
}
```

**Error Responses**:
```json
// 400 Bad Request - Cannot update closed pattern
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Cannot update closed pattern",
  "status": 400,
  "detail": "This pattern has already ended (EndDate is set). Create a new pattern instead."
}

// 404 Not Found - Pattern doesn't exist
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Pattern not found",
  "status": 404
}

// 409 Conflict - EndDate would overlap with existing pattern
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Date conflict",
  "status": 409,
  "detail": "The requested EndDate conflicts with another pattern starting on 2026-01-01."
}
```

---

### 5. Delete (Close) Dosage Pattern

Soft-deletes a pattern by setting its `EndDate` to today. Hard deletion is not allowed to preserve historical data.

**Request**:
```http
DELETE /api/medications/{medicationId}/patterns/{patternId}?endDate=2025-11-03
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
- `endDate` (date, optional, default: today): Date to set as the pattern's end date

**Success Response** (204 No Content):
```
(Empty body)
```

**Error Responses**:
```json
// 400 Bad Request - Pattern already closed
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Pattern already closed",
  "status": 400,
  "detail": "This pattern has already ended on 2025-10-01."
}

// 404 Not Found
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Pattern not found",
  "status": 404
}
```

---

## DTOs

### DosagePatternDto

Used in all responses.

```csharp
public class DosagePatternDto
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public List<decimal> PatternSequence { get; set; } = new();
    public int PatternLength { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public decimal AverageDosage { get; set; }
    public string DisplayPattern { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    // Extra fields for /active endpoint
    public decimal? TodaysDosage { get; set; }
    public int? TodaysPatternDay { get; set; }
}
```

---

## Authorization

All endpoints require:
- ✅ Valid JWT bearer token
- ✅ User must own the medication (checked via `Medication.UserId`)

---

## Rate Limiting

- Standard tier: 100 requests/minute per user
- Premium tier: 1000 requests/minute per user

---

## Versioning

Current version: `v1` (implicit, no version prefix)

Future breaking changes will use: `/api/v2/medications/{id}/patterns`

---

## OpenAPI Schema

```yaml
paths:
  /api/medications/{medicationId}/patterns:
    post:
      summary: Create new dosage pattern
      tags: [Medication Patterns]
      security:
        - BearerAuth: []
      parameters:
        - name: medicationId
          in: path
          required: true
          schema:
            type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateDosagePatternRequest'
      responses:
        '201':
          description: Pattern created successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DosagePatternDto'
        '400':
          $ref: '#/components/responses/ValidationError'
        '404':
          $ref: '#/components/responses/NotFound'
    get:
      summary: Get dosage pattern history
      tags: [Medication Patterns]
      security:
        - BearerAuth: []
      parameters:
        - name: medicationId
          in: path
          required: true
          schema:
            type: integer
        - name: activeOnly
          in: query
          schema:
            type: boolean
            default: false
        - name: limit
          in: query
          schema:
            type: integer
            default: 10
            maximum: 100
        - name: offset
          in: query
          schema:
            type: integer
            default: 0
      responses:
        '200':
          description: Pattern history retrieved
          content:
            application/json:
              schema:
                type: object
                properties:
                  medicationId:
                    type: integer
                  medicationName:
                    type: string
                  totalCount:
                    type: integer
                  patterns:
                    type: array
                    items:
                      $ref: '#/components/schemas/DosagePatternDto'
```

---

## Testing Examples

### cURL: Create Pattern

```bash
curl -X POST https://api.bloodthinner.app/api/medications/123/patterns \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "patternSequence": [4.0, 4.0, 3.0, 4.0, 3.0, 3.0],
    "startDate": "2025-11-04",
    "notes": "Winter pattern",
    "closePreviousPattern": true
  }'
```

### PowerShell: Get Active Pattern

```powershell
$headers = @{
    "Authorization" = "Bearer $env:JWT_TOKEN"
}

Invoke-RestMethod -Uri "https://api.bloodthinner.app/api/medications/123/patterns/active" `
    -Method GET `
    -Headers $headers
```

### C# HttpClient: Update Pattern

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var request = new UpdateDosagePatternRequest
{
    PatternSequence = new List<decimal> { 4.0, 3.0, 3.0 },
    Notes = "Adjusted based on INR results"
};

var response = await client.PutAsJsonAsync(
    $"https://api.bloodthinner.app/api/medications/123/patterns/42",
    request);

var pattern = await response.Content.ReadFromJsonAsync<DosagePatternDto>();
```

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-11-03 | 1.0 | Initial API specification |
