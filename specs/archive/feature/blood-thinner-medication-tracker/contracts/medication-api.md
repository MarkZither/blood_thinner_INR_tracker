# API Contracts: Medication Service

**Version**: 1.0.0  
**Base URL**: `/api/v1/medications`  
**Security**: JWT Bearer token required

---

## Medication Management

### 1. List User Medications
```http
GET /api/v1/medications
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `active`: `true` | `false` (filter by active status)
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

**Response**:
```json
{
  "medications": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Warfarin",
      "dosageAmount": 5.0,
      "dosageUnit": "mg",
      "instructions": "Take with food at the same time daily",
      "isActive": true,
      "createdAt": "2024-10-15T10:30:00Z",
      "discontinuedAt": null,
      "schedules": [
        {
          "id": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
          "scheduledTime": "08:00",
          "scheduledDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
          "reminderMinutesBefore": 15,
          "enableReminders": true,
          "isActive": true
        }
      ]
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalItems": 3,
    "totalPages": 1
  }
}
```

---

### 2. Get Medication Details
```http
GET /api/v1/medications/{medicationId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: Same structure as individual medication in list response

**Error Responses**:
- `404 Not Found`: Medication doesn't exist or doesn't belong to user

---

### 3. Create Medication
```http
POST /api/v1/medications
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "name": "Warfarin",
  "dosageAmount": 5.0,
  "dosageUnit": "mg",
  "instructions": "Take with food at the same time daily"
}
```

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Warfarin",
  "dosageAmount": 5.0,
  "dosageUnit": "mg", 
  "instructions": "Take with food at the same time daily",
  "isActive": true,
  "createdAt": "2024-10-15T10:30:00Z",
  "discontinuedAt": null,
  "schedules": []
}
```

**Validation Rules**:
- `name`: Required, 1-100 characters, letters/numbers/spaces only
- `dosageAmount`: Required, > 0, max 4 decimal places
- `dosageUnit`: Required, enum: ["mg", "mcg", "g", "units", "ml"]
- `instructions`: Optional, max 500 characters

---

### 4. Update Medication
```http
PUT /api/v1/medications/{medicationId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "name": "Warfarin Sodium",
  "dosageAmount": 7.5,
  "dosageUnit": "mg",
  "instructions": "Take with food. Avoid alcohol and vitamin K foods."
}
```

**Response**: Updated medication object (same structure as create response)

---

### 5. Discontinue Medication
```http
DELETE /api/v1/medications/{medicationId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: `204 No Content`

**Side Effects**:
- Sets `isActive = false` and `discontinuedAt = now()`
- Deactivates all associated schedules
- Cancels future scheduled reminders
- Preserves historical logs for audit purposes

---

## Medication Schedules

### 1. Create Schedule
```http
POST /api/v1/medications/{medicationId}/schedules
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "scheduledTime": "08:00",
  "scheduledDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
  "reminderMinutesBefore": 15,
  "enableReminders": true
}
```

**Response**:
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "scheduledTime": "08:00",
  "scheduledDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
  "reminderMinutesBefore": 15,
  "enableReminders": true,
  "maxReminderAttempts": 3,
  "isActive": true,
  "createdAt": "2024-10-15T10:30:00Z"
}
```

**Validation Rules**:
- `scheduledTime`: Required, format "HH:mm" (24-hour)
- `scheduledDays`: Required, array of valid day names
- `reminderMinutesBefore`: Optional, 0-120 minutes (default: 15)
- `enableReminders`: Optional, boolean (default: true)

---

### 2. Update Schedule
```http
PUT /api/v1/medications/{medicationId}/schedules/{scheduleId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "scheduledTime": "20:00",
  "reminderMinutesBefore": 30,
  "enableReminders": true
}
```

**Response**: Updated schedule object

---

### 3. Delete Schedule
```http
DELETE /api/v1/medications/{medicationId}/schedules/{scheduleId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: `204 No Content`

**Side Effects**:
- Sets `isActive = false`
- Cancels future scheduled reminders for this schedule
- Preserves existing logs that reference this schedule

---

## Medication Logs

### 1. Get Medication History
```http
GET /api/v1/medications/{medicationId}/logs
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `startDate`: ISO 8601 date (filter from date)
- `endDate`: ISO 8601 date (filter to date)
- `status`: Filter by log status (`Scheduled`, `Taken`, `Missed`, `Skipped`, `Unknown`)
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 50, max: 200)

**Response**:
```json
{
  "logs": [
    {
      "id": "5fa85f64-5717-4562-b3fc-2c963f66afa6",
      "medicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "scheduleId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
      "scheduledDateTime": "2024-10-15T08:00:00Z",
      "takenDateTime": "2024-10-15T08:05:00Z",
      "status": "Taken",
      "notes": "Taken with breakfast",
      "wasReminded": true,
      "reminderAttempts": 1,
      "deviceId": "user-iphone-15",
      "createdAt": "2024-10-15T08:05:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 127,
    "totalPages": 3
  },
  "summary": {
    "totalScheduled": 127,
    "taken": 115,
    "missed": 8,
    "skipped": 2,
    "unknown": 2,
    "adherenceRate": 0.906
  }
}
```

---

### 2. Log Medication Taken
```http
POST /api/v1/medications/{medicationId}/logs
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "scheduleId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
  "takenDateTime": "2024-10-15T08:05:00Z",
  "status": "Taken",
  "notes": "Taken with breakfast"
}
```

**Request Options**:
- `scheduleId`: Optional (null for unscheduled/PRN doses)
- `status`: Required, enum: ["Taken", "Missed", "Skipped"]
- `takenDateTime`: Required for "Taken" status
- `notes`: Optional, max 500 characters

**Response**:
```json
{
  "id": "5fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "scheduleId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
  "scheduledDateTime": "2024-10-15T08:00:00Z",
  "takenDateTime": "2024-10-15T08:05:00Z",
  "status": "Taken",
  "notes": "Taken with breakfast",
  "deviceId": "user-iphone-15",
  "createdAt": "2024-10-15T08:05:00Z"
}
```

**Safety Validations**:
- Cannot log doses more than 12 hours early or 24 hours late
- Cannot log duplicate "Taken" status for same scheduled time
- Warns if dose timing is outside typical adherence window

---

### 3. Update Medication Log
```http
PUT /api/v1/medications/{medicationId}/logs/{logId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "status": "Missed",
  "notes": "Forgot to take - was traveling"
}
```

**Response**: Updated log object

**Constraints**:
- Can only update logs from last 7 days
- Cannot change `scheduleId` or `scheduledDateTime`
- Status changes create audit log entries

---

### 4. Get Today's Schedule
```http
GET /api/v1/medications/schedule/today
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "date": "2024-10-15",
  "scheduledMedications": [
    {
      "medicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "medicationName": "Warfarin",
      "dosageAmount": 5.0,
      "dosageUnit": "mg",
      "scheduleId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
      "scheduledTime": "08:00",
      "status": "Taken",
      "takenAt": "2024-10-15T08:05:00Z",
      "isOverdue": false,
      "reminderSent": true
    }
  ],
  "summary": {
    "totalScheduled": 2,
    "taken": 1,
    "pending": 1,
    "overdue": 0
  }
}
```

---

## Adherence Analytics

### 1. Get Adherence Report
```http
GET /api/v1/medications/adherence
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Query Parameters**:
- `medicationId`: Filter by specific medication (optional)
- `period`: `7d` | `30d` | `90d` | `365d` (default: 30d)
- `groupBy`: `day` | `week` | `month` (default: day)

**Response**:
```json
{
  "period": "30d",
  "medications": [
    {
      "medicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "medicationName": "Warfarin",
      "adherenceRate": 0.906,
      "totalScheduled": 30,
      "taken": 27,
      "missed": 2,
      "skipped": 1,
      "trends": [
        {
          "date": "2024-10-01",
          "scheduled": 1,
          "taken": 1,
          "adherenceRate": 1.0
        }
      ]
    }
  ],
  "overallAdherence": 0.906,
  "insights": [
    {
      "type": "positive",
      "message": "Great job! Your adherence improved by 5% this month."
    },
    {
      "type": "warning", 
      "message": "You missed 2 doses on weekends. Consider setting weekend reminders."
    }
  ]
}
```

---

## Error Handling

### Validation Error Example
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more validation errors occurred",
    "details": [
      {
        "field": "dosageAmount",
        "message": "Dosage amount must be greater than 0"
      },
      {
        "field": "scheduledDays",
        "message": "At least one day must be selected"
      }
    ],
    "timestamp": "2024-10-15T10:30:00Z",
    "traceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

### Common Error Codes
- `MEDICATION_NOT_FOUND`: Medication doesn't exist or doesn't belong to user
- `SCHEDULE_CONFLICT`: Overlapping schedules for same medication
- `SAFETY_WINDOW_VIOLATION`: Attempting to log dose outside safety window
- `DUPLICATE_LOG_ENTRY`: Attempting to log duplicate dose for same schedule
- `INVALID_TIME_FORMAT`: Time format doesn't match HH:mm pattern