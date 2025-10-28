# API Contracts: INR Service

**Version**: 1.0.0  
**Base URL**: `/api/v1/inr`  
**Security**: JWT Bearer token required

---

## INR Test Management

### 1. List INR Tests
```http
GET /api/v1/inr/tests
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `startDate`: ISO 8601 date (filter from date)
- `endDate`: ISO 8601 date (filter to date)
- `inRange`: `true` | `false` (filter by target range compliance)
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

**Response**:
```json
{
  "tests": [
    {
      "id": "6fa85f64-5717-4562-b3fc-2c963f66afa6",
      "inrValue": 2.3,
      "targetINRMin": 2.0,
      "targetINRMax": 3.0,
      "isInRange": true,
      "testDate": "2024-10-15T09:30:00Z",
      "testLocation": "Home",
      "notes": "Feeling well, no bleeding issues",
      "createdAt": "2024-10-15T10:00:00Z",
      "modifiedAt": null
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalItems": 52,
    "totalPages": 3
  },
  "summary": {
    "averageINR": 2.4,
    "inRangePercentage": 0.846,
    "lastTestDate": "2024-10-15T09:30:00Z",
    "nextScheduledTest": "2024-10-22T09:00:00Z"
  }
}
```

---

### 2. Get INR Test Details
```http
GET /api/v1/inr/tests/{testId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: Same structure as individual test in list response

**Error Responses**:
- `404 Not Found`: Test doesn't exist or doesn't belong to user

---

### 3. Record INR Test
```http
POST /api/v1/inr/tests
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "inrValue": 2.3,
  "targetINRMin": 2.0,
  "targetINRMax": 3.0,
  "testDate": "2024-10-15T09:30:00Z",
  "testLocation": "Home",
  "notes": "Feeling well, no bleeding issues"
}
```

**Response**:
```json
{
  "id": "6fa85f64-5717-4562-b3fc-2c963f66afa6",
  "inrValue": 2.3,
  "targetINRMin": 2.0,
  "targetINRMax": 3.0,
  "isInRange": true,
  "testDate": "2024-10-15T09:30:00Z",
  "testLocation": "Home",
  "notes": "Feeling well, no bleeding issues",
  "createdAt": "2024-10-15T10:00:00Z",
  "modifiedAt": null,
  "trends": {
    "previousValue": 2.1,
    "changeFromPrevious": 0.2,
    "trend": "stable",
    "daysFromLastTest": 7
  }
}
```

**Validation Rules**:
- `inrValue`: Required, 0.5 ≤ value ≤ 10.0 (physiologically reasonable)
- `targetINRMin`: Optional, 1.0 ≤ value ≤ 4.0
- `targetINRMax`: Optional, 1.0 ≤ value ≤ 4.0, must be > targetINRMin
- `testDate`: Required, cannot be future date, max 30 days old for new entries
- `testLocation`: Optional, enum: ["Home", "Lab", "Doctor's Office", "Hospital", "Other"]
- `notes`: Optional, max 1000 characters

**Safety Alerts**:
- INR < 1.5 or > 5.0 triggers safety warning in response
- Significant changes (>0.5 from target) include dosage adjustment recommendations

---

### 4. Update INR Test
```http
PUT /api/v1/inr/tests/{testId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "inrValue": 2.4,
  "targetINRMin": 2.0,
  "targetINRMax": 3.0,
  "notes": "Updated after consultation with doctor"
}
```

**Response**: Updated test object with modification timestamp

**Constraints**:
- Can only update tests from last 30 days
- Cannot change `testDate` (create new entry instead)
- All changes create audit log entries

---

### 5. Delete INR Test
```http
DELETE /api/v1/inr/tests/{testId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: `204 No Content`

**Constraints**:
- Can only delete tests from last 7 days
- Soft delete preserves data for audit purposes
- Recalculates trends for remaining tests

---

## INR Scheduling

### 1. Get INR Schedule
```http
GET /api/v1/inr/schedule
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "frequencyDays": 7,
  "preferredTime": "09:00",
  "nextTestDate": "2024-10-22",
  "isActive": true,
  "enableReminders": true,
  "reminderDaysBefore": 1,
  "createdAt": "2024-10-01T10:00:00Z",
  "modifiedAt": "2024-10-15T10:30:00Z"
}
```

---

### 2. Create/Update INR Schedule
```http
PUT /api/v1/inr/schedule
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "frequencyDays": 14,
  "preferredTime": "09:00",
  "nextTestDate": "2024-10-29",
  "enableReminders": true,
  "reminderDaysBefore": 2
}
```

**Response**: Updated schedule object

**Validation Rules**:
- `frequencyDays`: Required, 3-90 days (typical range: 7-28)
- `preferredTime`: Required, format "HH:mm" (24-hour)
- `nextTestDate`: Required, must be future date
- `enableReminders`: Optional, boolean (default: true)
- `reminderDaysBefore`: Optional, 0-7 days (default: 1)

**Side Effects**:
- Automatically calculates future test dates based on frequency
- Schedules reminder notifications
- Updates existing reminders if schedule changes

---

### 3. Deactivate INR Schedule
```http
DELETE /api/v1/inr/schedule
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: `204 No Content`

**Side Effects**:
- Sets `isActive = false`
- Cancels all future scheduled reminders
- Preserves schedule history for reference

---

## INR Analytics & Trends

### 1. Get INR Trend Analysis
```http
GET /api/v1/inr/trends
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `period`: `30d` | `90d` | `180d` | `365d` (default: 90d)
- `includeTargetRange`: `true` | `false` (default: true)

**Response**:
```json
{
  "period": "90d",
  "dataPoints": [
    {
      "testDate": "2024-10-15",
      "inrValue": 2.3,
      "targetMin": 2.0,
      "targetMax": 3.0,
      "isInRange": true,
      "daysFromPrevious": 7
    }
  ],
  "statistics": {
    "averageINR": 2.4,
    "medianINR": 2.3,
    "standardDeviation": 0.3,
    "coefficientOfVariation": 0.125,
    "inRangePercentage": 0.846,
    "totalTests": 13,
    "averageTestInterval": 7.2
  },
  "trends": {
    "overall": "stable",
    "recentTrend": "improving",
    "volatility": "low",
    "consistencyScore": 0.85
  },
  "insights": [
    {
      "type": "positive",
      "title": "Excellent Control",
      "message": "Your INR has been in therapeutic range 85% of the time over the last 3 months."
    },
    {
      "type": "recommendation",
      "title": "Test Timing",
      "message": "Consider testing every 14 days instead of weekly due to stable values."
    }
  ]
}
```

---

### 2. Get Time in Therapeutic Range (TTR)
```http
GET /api/v1/inr/ttr
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `startDate`: ISO 8601 date (required)
- `endDate`: ISO 8601 date (required, max 365 days from start)
- `method`: `linear` | `discrete` (interpolation method, default: linear)

**Response**:
```json
{
  "timeInTherapeuticRange": {
    "percentage": 72.3,
    "totalDays": 90,
    "daysInRange": 65,
    "daysAboveRange": 15,
    "daysBelowRange": 10
  },
  "targetRange": {
    "minimum": 2.0,
    "maximum": 3.0
  },
  "calculationMethod": "linear",
  "qualityIndicators": {
    "excellentControl": false,
    "goodControl": true,
    "poorControl": false,
    "grade": "B"
  },
  "recommendations": [
    {
      "priority": "high",
      "message": "TTR of 72% indicates good control. Target >70% is achieved."
    },
    {
      "priority": "medium", 
      "message": "Consider more frequent testing during periods of instability."
    }
  ]
}
```

**TTR Quality Grades**:
- **A (Excellent)**: TTR ≥ 80%
- **B (Good)**: TTR 65-79%
- **C (Fair)**: TTR 50-64%
- **D (Poor)**: TTR < 50%

---

### 3. Get Dosage Adjustment Recommendations
```http
GET /api/v1/inr/dosage-recommendations
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "currentINR": 2.3,
  "targetRange": {
    "minimum": 2.0,
    "maximum": 3.0
  },
  "recommendations": [
    {
      "type": "maintain",
      "message": "Current INR is within target range. Continue current dosage.",
      "confidence": "high"
    }
  ],
  "riskFactors": [
    {
      "factor": "diet_change",
      "description": "Recent changes in vitamin K intake",
      "impact": "medium",
      "recommendation": "Monitor closely for next 2 weeks"
    }
  ],
  "nextTestRecommendation": {
    "suggestedDate": "2024-10-22",
    "reasoning": "Stable INR allows for weekly testing interval"
  },
  "disclaimer": "These recommendations are for educational purposes only. Always consult your healthcare provider before making dosage changes."
}
```

---

## Export & Integration

### 1. Export INR Data
```http
GET /api/v1/inr/export
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `format`: `json` | `csv` | `pdf` (default: json)
- `startDate`: ISO 8601 date (optional, defaults to all data)
- `endDate`: ISO 8601 date (optional, defaults to current date)
- `includeTrends`: `true` | `false` (default: false)

**Response (JSON format)**:
```json
{
  "exportMetadata": {
    "generatedAt": "2024-10-15T10:30:00Z",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "period": {
      "startDate": "2024-01-01",
      "endDate": "2024-10-15"
    },
    "totalTests": 52
  },
  "tests": [
    {
      "testDate": "2024-10-15T09:30:00Z",
      "inrValue": 2.3,
      "targetMin": 2.0,
      "targetMax": 3.0,
      "isInRange": true,
      "testLocation": "Home",
      "notes": "Feeling well"
    }
  ],
  "summary": {
    "averageINR": 2.4,
    "timeInTherapeuticRange": 72.3,
    "totalTestsInRange": 38,
    "lastTestDate": "2024-10-15"
  }
}
```

**Response Headers**:
- JSON: `Content-Type: application/json`
- CSV: `Content-Type: text/csv; Content-Disposition: attachment; filename="inr-data.csv"`
- PDF: `Content-Type: application/pdf; Content-Disposition: attachment; filename="inr-report.pdf"`

---

### 2. Healthcare Provider Summary
```http
GET /api/v1/inr/provider-summary
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `period`: `30d` | `90d` | `180d` (default: 90d)
- `includeAdherence`: `true` | `false` (default: true)

**Response**:
```json
{
  "patientSummary": {
    "name": "John D.",
    "reportPeriod": "90 days",
    "generatedDate": "2024-10-15"
  },
  "inrControl": {
    "timeInTherapeuticRange": 72.3,
    "averageINR": 2.4,
    "standardDeviation": 0.3,
    "totalTests": 13,
    "testsInRange": 9,
    "testsAboveRange": 2,
    "testsBelowRange": 2
  },
  "medicationAdherence": {
    "adherenceRate": 90.6,
    "missedDoses": 8,
    "totalScheduledDoses": 85
  },
  "riskAssessment": {
    "overallRisk": "low",
    "bleedingRisk": "low",
    "thrombosisRisk": "low",
    "factors": [
      "Stable INR control",
      "Good medication adherence",
      "Regular testing schedule"
    ]
  },
  "recommendations": [
    {
      "category": "testing",
      "recommendation": "Continue weekly INR testing due to stable control"
    },
    {
      "category": "dosage",
      "recommendation": "Current warfarin dosage appears appropriate"
    }
  ]
}
```

---

## Error Handling

### Safety Alert Example
```json
{
  "warning": {
    "type": "CRITICAL_INR_VALUE",
    "severity": "high",
    "message": "INR value of 5.2 is significantly above therapeutic range",
    "recommendations": [
      "Contact healthcare provider immediately",
      "Consider dose adjustment or temporary discontinuation",
      "Monitor for signs of bleeding"
    ],
    "urgency": "immediate"
  }
}
```

### Common Error Codes
- `INR_OUT_OF_RANGE`: INR value outside physiologically reasonable bounds
- `INVALID_TARGET_RANGE`: Target min/max values are invalid or illogical
- `TEST_TOO_OLD`: Attempting to enter test older than 30 days
- `DUPLICATE_TEST_DATE`: Test already exists for the same date
- `SCHEDULE_CONFLICT`: INR schedule conflicts with existing schedule