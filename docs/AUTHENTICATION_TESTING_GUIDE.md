# Authentication & API Testing Guide

**Project**: Blood Thinner Medication & INR Tracker  
**Created**: 2025-10-23  
**Updated**: 2025-10-23 (Added OAuth Test Page)  
**Purpose**: Comprehensive guide for testing API authentication flows

---

## Quick Start: Get a JWT Token

**🎯 For developers who just want to test the API:**

1. Start the API: `dotnet run --project src/BloodThinnerTracker.Api`
2. Open: **https://localhost:7000/oauth-test.html**
3. Click "Login with Google" or "Login with Azure AD"
4. Copy your JWT token
5. Use in Scalar at **https://localhost:7000/scalar/v1**

📖 **Detailed instructions**: See [OAuth Testing Guide](OAUTH_TESTING_GUIDE.md)

---

## Overview

This application supports **three authentication methods** for different use cases:

| Method | Use Case | Requirement | Status |
|--------|----------|-------------|--------|
| **OAuth2 Redirect** | Web UI, Developer testing | FR-001, FR-022 | ✅ Implemented (T015) |
| **OAuth2 ID Token Exchange** | Mobile apps (iOS, Android) | FR-001 | ✅ Implemented |
| **Mutual TLS (mTLS)** | CI/CD, Integrations | FR-022 | ⏳ To Implement (T046) |

---

## Authentication Methods

### 1. OAuth2 Redirect Flow (Web + API Testing)

**For**: Web applications, Developer testing, Scalar UI authentication  
**Status**: ✅ **Implemented** (T015 - Complete)  
**Test Tool**: `/oauth-test.html` - Self-service token generation page

**How it works**:
```
1. Developer/User navigates to /oauth-test.html
2. Clicks "Sign in with Google" or "Sign in with Azure AD"
3. API redirects to OAuth provider (Azure AD/Google)
4. User authenticates at provider
5. Provider redirects back to API: GET /api/auth/callback/{provider}?code=...
6. API exchanges authorization code for ID token
7. API validates ID token, creates/updates user in database
8. API redirects back to test page with JWT token in query string
9. Test page displays JWT for copy/paste into Scalar or other tools
```

**API Endpoints** (T015 - ✅ Implemented):
```http
### Initiate OAuth flow
GET /api/auth/external/{provider}
  ?redirectUri=https://localhost:7000/oauth-test.html

### OAuth callback (automatically called by provider)
GET /api/auth/callback/{provider}
  ?code=authorization_code
  &state=csrf_state_value
```

**Developer Test Page** (T015i - ✅ Implemented):
- **URL**: https://localhost:7000/oauth-test.html
- **Purpose**: Self-service JWT token generation for API testing
- **Features**:
  - One-click OAuth login (Google + Azure AD)
  - Automatic token display with copy button
  - Step-by-step Scalar integration instructions
  - Error handling with user-friendly messages
- **Documentation**: See [OAuth Testing Guide](OAUTH_TESTING_GUIDE.md)

**Testing with Swagger**:
```
1. Navigate to https://localhost:7000/scalar/v1
2. Click "Authorize" button
3. Select OAuth2 provider (Azure AD or Google)
4. Complete OAuth flow in popup
5. Swagger stores tokens automatically
6. All API calls include Authorization header
```

---

### 2. OAuth2 ID Token Exchange (Mobile)

**For**: iOS, Android mobile applications  
**How it works**:
```
1. Mobile app uses platform-native OAuth (Sign in with Apple, Google Sign-In SDK)
2. User authenticates in native dialog
3. Platform returns ID token to app
4. App sends ID token to API: POST /api/auth/external/mobile
5. API validates ID token against Azure AD/Google
6. API creates/updates user in database
7. API returns JWT access + refresh tokens
8. App stores tokens securely (Keychain/Keystore)
9. App calls API with Authorization: Bearer {token}
```

**API Endpoint** (✅ Already implemented):
```http
POST /api/auth/external/mobile
Content-Type: application/json

{
  "provider": "Google",
  "idToken": "eyJhbGc...",
  "deviceId": "iPhone-14-Pro-UUID",
  "devicePlatform": "iOS 17.1"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "refresh_token_value",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "user": {
    "id": "user-guid",
    "email": "patient@example.com",
    "name": "John Doe",
    "role": "Patient",
    "provider": "Google"
  }
}
```

**Testing Mobile Flow**:
```bash
# 1. Get ID token from Google (using gcloud CLI or OAuth Playground)
# https://developers.google.com/oauthplayground

# 2. Exchange for API tokens
curl -X POST https://localhost:7000/api/auth/external/mobile \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "Google",
    "idToken": "YOUR_GOOGLE_ID_TOKEN_HERE",
    "deviceId": "test-device-123",
    "devicePlatform": "iOS 17.1"
  }'

# 3. Use returned access token for API calls
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  https://localhost:7000/api/medications
```

---

### 3. Mutual TLS (mTLS) - Certificate-Based Auth

**For**: Testing tools, CI/CD, future integrations  
**How it works**:
```
1. Client presents X.509 certificate during TLS handshake
2. Server validates certificate (trusted CA, not expired, not revoked)
3. Server extracts subject/CN from certificate
4. Server looks up integration partner by certificate subject
5. If valid, grant API access with partner permissions
6. No user session - direct API access with certificate identity
```

**Use Cases**:
- ✅ **Automated Testing**: CI/CD pipelines can test API without OAuth
- ✅ **Development Tools**: Postman, curl with client certificates
- ✅ **Integration Partners**: Future HL7/FHIR healthcare system integrations
- ✅ **Internal Services**: Microservice-to-microservice communication

**Testing with mTLS** (To be implemented):
```bash
# Generate test certificate
openssl req -x509 -newkey rsa:4096 -keyout client-key.pem -out client-cert.pem -days 365 -nodes \
  -subj "/CN=test-integration/O=BloodTracker/OU=Testing"

# Register certificate with API (admin operation)
# Store certificate subject in database: Integration Partner record

# Test API with certificate
curl --cert client-cert.pem --key client-key.pem \
  https://localhost:7000/api/medications

# Server validates certificate and grants access
```

**Certificate Validation**:
- Certificate not expired
- Signed by trusted CA (or self-signed for development)
- Subject/CN matches registered integration partner
- Certificate not revoked (OCSP check)
- Failed attempts logged for security monitoring

---

## Recommended Implementation Order

### Phase 1: Core OAuth (Current - T015)
✅ ID Token Exchange (mobile) - **DONE**  
⏳ OAuth Redirect Flow (web + Swagger)
- Endpoint: `GET /api/auth/external/{provider}`
- Endpoint: `GET /api/auth/callback/{provider}`
- Swagger OAuth2 configuration

### Phase 2: mTLS Support (T022 - New)
⏳ Certificate validation middleware  
⏳ Integration partner registration  
⏳ Certificate revocation checking  
⏳ Security audit logging

---

## Testing Scenarios

### Scenario 1: Web Application Testing (OAuth Redirect)
```
✅ Developer opens Swagger UI
✅ Clicks "Authorize" → "Sign in with Microsoft"
✅ Redirected to Azure AD login
✅ Authenticates with work account
✅ Redirected back to Swagger with tokens
✅ Tests API endpoints with authenticated session
```

### Scenario 2: Mobile App Testing (ID Token Exchange)
```
✅ Developer uses Google OAuth Playground to get ID token
✅ Sends ID token to /api/auth/external/mobile
✅ Receives JWT access + refresh tokens
✅ Uses tokens to test mobile-specific API endpoints
✅ Tests token refresh flow
```

### Scenario 3: Integration Testing (mTLS)
```
⏳ CI/CD pipeline generates ephemeral client certificate
⏳ Registers certificate with test API environment
⏳ Runs integration tests with certificate authentication
⏳ Tests pass without user interaction
⏳ Certificate auto-revoked after test run
```

### Scenario 4: Healthcare Integration (mTLS)
```
⏳ Hospital EMR system requests integration
⏳ Hospital provides CA-signed certificate
⏳ Admin registers certificate in BloodTracker
⏳ Hospital EMR calls API with certificate
⏳ Fetches patient data for HL7/FHIR export
```

---

## Security Considerations

### OAuth2 Security
- ✅ State parameter prevents CSRF attacks
- ✅ Authorization code flow (not implicit flow)
- ✅ ID token signature validation against Azure AD/Google JWKS
- ✅ Refresh token rotation on every refresh
- ✅ 15-minute access token lifetime
- ✅ 7-day refresh token lifetime
- ✅ Tokens stored hashed in database (SHA-256)

### mTLS Security
- ⏳ Client certificate validation (trusted CA, expiry, revocation)
- ⏳ Subject/CN matching against registered partners
- ⏳ OCSP revocation checking
- ⏳ Failed authentication attempts logged
- ⏳ Certificate rotation support
- ⏳ Separate permissions model for mTLS vs OAuth

---

## Implementation Tasks

### T015: OAuth2 Redirect Endpoints (Priority: High)
```
- [ ] Create OAuthController
- [ ] GET /api/auth/external/{provider} - Redirect to OAuth provider
- [ ] GET /api/auth/callback/{provider} - Handle OAuth callback
- [ ] State parameter validation (CSRF protection)
- [ ] Integrate with IdTokenValidationService
- [ ] Swagger OAuth2 configuration
- [ ] Web UI integration (delegate auth to API)
```

### T022: Mutual TLS Support (Priority: Medium)
```
- [ ] Add Microsoft.AspNetCore.Authentication.Certificate package
- [ ] Create CertificateAuthenticationMiddleware
- [ ] IntegrationPartner entity (stores certificate subjects)
- [ ] Certificate validation service
- [ ] OCSP revocation checking
- [ ] Admin endpoints for certificate management
- [ ] Security audit logging
- [ ] Development certificate generation script
```

---

## Configuration Examples

### OAuth2 Configuration (appsettings.json)
```json
{
  "Authentication": {
    "Jwt": {
      "SecretKey": "your-256-bit-secret",
      "Issuer": "https://bloodtracker.com",
      "Audience": "bloodtracker-api",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    },
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "common",
      "ClientId": "your-azure-ad-client-id",
      "ClientSecret": "your-azure-ad-client-secret",
      "CallbackPath": "/api/auth/callback/azuread"
    },
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret",
      "CallbackPath": "/api/auth/callback/google"
    }
  }
}
```

### mTLS Configuration (appsettings.json)
```json
{
  "CertificateAuthentication": {
    "Enabled": true,
    "AllowedCertificateTypes": "SelfSigned,Chained",
    "RevocationMode": "Online",
    "RevocationFlag": "EntireChain",
    "ValidateCertificateUse": true,
    "ValidateValidityPeriod": true,
    "ChainTrustValidationMode": "CustomRootTrust"
  }
}
```

---

## Testing Tools

### Recommended Tools
- **Swagger UI**: OAuth2 flow testing (web redirect)
- **Postman**: All auth methods (OAuth, mTLS)
- **curl**: Command-line testing with certificates
- **OAuth Playground**: Get test ID tokens (Google)
- **Azure Portal**: Get test tokens (Azure AD)

### Quick Test Commands
```bash
# Test with OAuth token (manual)
TOKEN=$(curl -s -X POST .../token | jq -r .access_token)
curl -H "Authorization: Bearer $TOKEN" https://localhost:7000/api/medications

# Test with mTLS
curl --cert client.pem --key key.pem https://localhost:7000/api/medications

# Test token refresh
curl -X POST https://localhost:7000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken": "your_refresh_token"}'
```

---

## Summary

**Current State:**
- ✅ Mobile ID token exchange fully implemented and tested
- ✅ Refresh token rotation working
- ✅ Database schema ready (RefreshTokens table)

**Next Steps:**
1. **Implement T015**: OAuth redirect endpoints for web/Swagger testing
2. **Implement T022**: mTLS certificate authentication for integrations
3. **Update Swagger**: Configure OAuth2 authorization UI
4. **Document**: Add testing examples to README

**Benefits:**
- **Developers**: Easy testing via Swagger with real OAuth
- **QA**: Integration tests with mTLS (no user interaction)
- **Mobile**: Platform-native OAuth with ID token exchange
- **Future**: Healthcare system integrations via mTLS

All three methods work together to provide comprehensive authentication coverage for all use cases!
