```markdown
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

```
