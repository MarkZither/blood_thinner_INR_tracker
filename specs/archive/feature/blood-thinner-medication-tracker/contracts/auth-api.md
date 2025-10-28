# API Contracts: Authentication Service

**Version**: 1.0.0  
**Base URL**: `/api/v1/auth`  
**Security**: OAuth 2.0 + JWT Bearer tokens

---

## Authentication Flow

### 1. OAuth Authorization
```http
GET /auth/authorize?provider={provider}&redirect_uri={uri}&state={state}
```

**Parameters**:
- `provider`: `azuread` | `google`
- `redirect_uri`: Client callback URL (URL-encoded)
- `state`: CSRF protection token

**Response**: HTTP 302 redirect to OAuth provider

---

### 2. OAuth Callback  
```http
GET /auth/callback/{provider}?code={code}&state={state}
```

**Response**: 
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "dGVzdC1yZWZyZXNoLXRva2Vu",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com", 
    "firstName": "John",
    "lastName": "Doe",
    "isActive": true,
    "createdAt": "2024-10-15T10:30:00Z"
  }
}
```

---

### 3. Token Refresh
```http
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "dGVzdC1yZWZyZXNoLXRva2Vu"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "bmV3LXJlZnJlc2gtdG9rZW4",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid or expired refresh token
- `401 Unauthorized`: Refresh token revoked or malformed

---

### 4. Token Validation
```http
GET /auth/validate
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "valid": true,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "firstName": "John", 
    "lastName": "Doe"
  },
  "expiresAt": "2024-10-15T11:30:00Z",
  "scopes": ["medication:read", "medication:write", "inr:read", "inr:write"]
}
```

**Error Responses**:
- `401 Unauthorized`: Invalid, expired, or malformed token
- `403 Forbidden`: Token valid but insufficient scopes

---

### 5. User Logout
```http
POST /auth/logout  
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "message": "Successfully logged out",
  "loggedOutAt": "2024-10-15T10:45:00Z"
}
```

**Side Effects**:
- Invalidates the provided access token
- Revokes associated refresh token
- Clears server-side session data

---

## Device Registration

### Register Device
```http
POST /auth/devices
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "deviceId": "unique-device-identifier",
  "deviceName": "iPhone 15 Pro",
  "platform": "iOS",
  "pushToken": "apns-push-token-here"
}
```

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "deviceId": "unique-device-identifier",
  "deviceName": "iPhone 15 Pro",
  "platform": "iOS",
  "registeredAt": "2024-10-15T10:30:00Z",
  "isActive": true
}
```

**Validation Rules**:
- `deviceId`: Required, 1-100 characters, unique per user
- `deviceName`: Required, 1-50 characters
- `platform`: Required, enum: ["iOS", "Android", "Web", "Console"]
- `pushToken`: Optional, platform-specific push notification token

---

### Update Device
```http
PUT /auth/devices/{deviceId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
Content-Type: application/json

{
  "deviceName": "Updated Device Name",
  "pushToken": "updated-push-token"
}
```

**Response**: Same as registration response with updated values

---

### List User Devices
```http
GET /auth/devices
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**:
```json
{
  "devices": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "deviceId": "device-1",
      "deviceName": "iPhone 15 Pro",
      "platform": "iOS", 
      "lastSyncAt": "2024-10-15T10:25:00Z",
      "registeredAt": "2024-10-15T09:00:00Z",
      "isActive": true
    }
  ]
}
```

---

### Unregister Device
```http
DELETE /auth/devices/{deviceId}
Authorization: Bearer eyJhbGciOiJSUzI1NiIs...
```

**Response**: `204 No Content`

**Side Effects**:
- Marks device as inactive
- Stops push notifications to device
- Retains sync history for audit purposes

---

## Security Considerations

### JWT Token Structure
```json
{
  "iss": "https://api.bloodthinnertracker.com",
  "aud": "bloodthinner-client",
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "iat": 1697365800,
  "exp": 1697369400,
  "email": "user@example.com",
  "scopes": ["medication:read", "medication:write"],
  "device_id": "unique-device-identifier"
}
```

### Rate Limiting
- Authentication endpoints: 10 requests per minute per IP
- Token refresh: 5 requests per minute per user  
- Device registration: 3 requests per hour per user

### Error Response Format
```json
{
  "error": {
    "code": "INVALID_TOKEN",
    "message": "The provided access token is invalid or expired",
    "details": "Token signature verification failed",
    "timestamp": "2024-10-15T10:30:00Z",
    "traceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

### Common Error Codes
- `INVALID_CREDENTIALS`: Username/password authentication failed
- `INVALID_TOKEN`: JWT token invalid, expired, or malformed
- `INSUFFICIENT_SCOPES`: Token lacks required permissions
- `DEVICE_LIMIT_EXCEEDED`: User has registered maximum allowed devices
- `RATE_LIMIT_EXCEEDED`: Too many requests in time window