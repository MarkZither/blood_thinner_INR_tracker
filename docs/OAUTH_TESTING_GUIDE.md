# OAuth Testing Guide for Developers

## Quick Start: Get a JWT Token in 30 Seconds

### Step 1: Start the API
```powershell
dotnet run --project src/BloodThinnerTracker.Api
```

### Step 2: Open the OAuth Test Page
Navigate to: **https://localhost:7000/oauth-test.html**

### Step 3: Login with OAuth
Click either:
- **Login with Google** 🔵
- **Login with Azure AD** 🔷

### Step 4: Get Your Token
After successful authentication, your JWT token will appear on the page. Click **"Copy"** to copy it to your clipboard.

### Step 5: Test in Scalar
1. Navigate to **https://localhost:7000/scalar/v1**
2. Click the **"Authenticate"** button (top right)
3. Select **"bearer"** authentication scheme
4. Paste your JWT token
5. Click **"Authorize"**
6. Test any protected API endpoint! 🚀

---

## What This Test Page Does

The OAuth test page (`/oauth-test.html`) is a developer tool that:

1. **Initiates OAuth Flow**: Redirects you to Google or Azure AD login
2. **Handles Callback**: Receives the OAuth callback with authorization code
3. **Exchanges for Token**: API exchanges code for ID token and validates it
4. **Returns JWT**: Displays the JWT access token for easy copy/paste

## Architecture

```
┌─────────────┐      ┌──────────────┐      ┌───────────────┐
│OAuth Test   │      │Blood Thinner │      │Google/Azure AD│
│Page (HTML)  │─────▶│ Tracker API  │─────▶│    (OAuth)    │
└─────────────┘      └──────────────┘      └───────────────┘
      │                     │                      │
      │  1. Click Login     │                      │
      │────────────────────▶│                      │
      │                     │  2. Redirect to     │
      │                     │     Provider        │
      │                     │─────────────────────▶│
      │                     │                      │
      │  3. User Authenticates                    │
      │                     │◀─────────────────────│
      │                     │  4. Callback with   │
      │◀────────────────────│     Authorization   │
      │  5. Redirect with   │     Code            │
      │     JWT Token       │                      │
      └─────────────────────┴──────────────────────┘
```

## API Endpoints Used

### 1. Initiate OAuth
```
GET /api/auth/external/{provider}?redirectUri=https://localhost:7000/oauth-test.html
```
- **provider**: `google` or `azuread`
- **redirectUri**: Must end with `/oauth-test.html` for test page flow

### 2. OAuth Callback
```
GET /api/auth/callback/{provider}?code={code}&state={state}
```
- Validates CSRF state
- Exchanges code for ID token
- Returns JWT in query string: `?token={jwt}`

### 3. Mobile OAuth (Alternative)
```
POST /api/auth/external/mobile
{
  "provider": "Google",
  "idToken": "{id_token}",
  "deviceId": "test-device",
  "devicePlatform": "Android"
}
```

## OAuth Configuration Required

Update `appsettings.Development.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7000",
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "AzureAd": {
      "ClientId": "YOUR_AZURE_AD_CLIENT_ID",
      "ClientSecret": "YOUR_AZURE_AD_CLIENT_SECRET",
      "TenantId": "YOUR_TENANT_ID"
    }
  }
}
```

### Get OAuth Credentials

**Google:**
1. Go to https://console.cloud.google.com/
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URI: `https://localhost:7000/api/auth/callback/google`

**Azure AD:**
1. Go to https://portal.azure.com/
2. Navigate to Azure Active Directory → App registrations
3. Register a new application
4. Add redirect URI: `https://localhost:7000/api/auth/callback/azuread`
5. Create a client secret

## Testing Authenticated Endpoints in Scalar

Once you have your JWT token:

### Example: Get Current User
```http
GET /api/users/me
Authorization: Bearer {your_jwt_token}
```

### Example: Create Medication Log
```http
POST /api/medications/logs
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
  "medicationScheduleId": 1,
  "dosageAmount": 5.0,
  "dosageUnit": "mg",
  "takenAt": "2025-10-23T08:00:00Z"
}
```

## Troubleshooting

### Error: "OAuth provider returned error: access_denied"
- User cancelled the OAuth flow
- Click the login button again

### Error: "CSRF state validation failed"
- State parameter expired (5 minute timeout)
- Clear browser cache and try again

### Error: "Google authentication not configured"
- Missing `Authentication:Google:ClientId` in appsettings.json
- Add valid Google OAuth credentials

### Token doesn't work in Scalar
- Ensure you copied the **entire** token (can be very long)
- Check token hasn't expired (15 minute lifetime)
- Verify you selected "bearer" (not "oauth2-google" or "oauth2-azuread")

## Security Notes

⚠️ **IMPORTANT:**
- This test page is **DEVELOPMENT ONLY**
- Never deploy `/oauth-test.html` to production
- JWT tokens are sensitive - don't share them
- Tokens expire after 15 minutes
- Use refresh tokens for production apps

## Alternative: Manual Testing with cURL

If you prefer command-line testing:

```bash
# 1. Get the authorization URL
curl -X GET "https://localhost:7000/api/auth/external/google?redirectUri=https://localhost:7000/oauth-test.html"
# Follow the redirect URL in your browser

# 2. After OAuth callback, extract token from URL
# URL will be: https://localhost:7000/oauth-test.html?token={jwt}

# 3. Use token in API calls
curl -X GET "https://localhost:7000/api/users/me" \
  -H "Authorization: Bearer {your_jwt_token}"
```

## Production Use

For production web applications:
- Use `/api/auth/external/{provider}` with your app's redirect URI
- Handle the callback in your frontend
- Store tokens securely (HttpOnly cookies or secure storage)
- Implement token refresh logic

For mobile applications:
- Use platform-native OAuth (Google Sign-In SDK, MSAL for Azure AD)
- Get ID token from native SDK
- POST to `/api/auth/external/mobile` with ID token
- Store access token securely (iOS Keychain, Android Keystore)

---

**Happy Testing!** 🎉
