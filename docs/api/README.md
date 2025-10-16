# Blood Thinner & INR Tracker - API Documentation

## Overview

The Blood Thinner & INR Tracker provides a comprehensive REST API for managing medication schedules, tracking INR test results, and ensuring medication safety compliance.

**⚠️ MEDICAL DISCLAIMER**: This software is for informational purposes only. Always consult with qualified healthcare professionals for medical decisions.

## Base URL

```
https://api.bloodtracker.com/v1
```

Development: `http://localhost:5000/v1`

## Authentication

All API endpoints require authentication using JWT tokens obtained through OAuth2 providers:

- **Azure AD**: Enterprise/organizational accounts
- **Google**: Personal Google accounts

### Authentication Header

```http
Authorization: Bearer <jwt_token>
```

## Core Endpoints

### Medications

- `GET /medications` - List user's medications
- `POST /medications` - Add new medication
- `PUT /medications/{id}` - Update medication
- `DELETE /medications/{id}` - Remove medication

### Medication Logs

- `GET /medication-logs` - Get medication history
- `POST /medication-logs` - Log medication dose
- `GET /medication-logs/upcoming` - Get upcoming doses

### INR Tests

- `GET /inr-tests` - List INR test results
- `POST /inr-tests` - Record new INR test
- `GET /inr-tests/trends` - Get INR trend analysis

### Reminders

- `GET /reminders` - Get active reminders
- `PUT /reminders/{id}` - Update reminder settings

## Safety Features

### 12-Hour Safety Window

The API enforces a 12-hour safety window to prevent double-dosing:

- Duplicate dose attempts within 12 hours are rejected
- Override available for emergency situations (requires confirmation)

### INR Range Validation

- Validates INR values within medical ranges (0.5 - 8.0)
- Flags dangerous values for immediate attention

## Response Format

```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "timestamp": "2025-10-16T10:30:00Z"
}
```

## Error Handling

```json
{
  "success": false,
  "error": {
    "code": "SAFETY_WINDOW_VIOLATION",
    "message": "Cannot log dose within 12-hour safety window",
    "details": {
      "lastDoseTime": "2025-10-16T08:00:00Z",
      "nextAllowedTime": "2025-10-16T20:00:00Z"
    }
  },
  "timestamp": "2025-10-16T10:30:00Z"
}
```

## Rate Limiting

- **Standard endpoints**: 100 requests per minute
- **Medication logging**: 10 requests per minute (safety measure)
- **Authentication**: 5 requests per minute

## SDK and Client Libraries

- **.NET Client**: `BloodThinnerTracker.Client` NuGet package
- **Mobile App**: Native iOS/Android via .NET MAUI
- **Web Interface**: Blazor Server/WebAssembly
- **CLI Tool**: `dotnet tool install bloodtracker-cli`

## Support

For API questions and support:
- Documentation: https://docs.bloodtracker.com
- Issues: https://github.com/bloodtracker/api/issues
- Email: api-support@bloodtracker.com

---

**Remember**: This API handles sensitive medical data. Always follow healthcare data protection regulations and consult healthcare professionals for medical decisions.