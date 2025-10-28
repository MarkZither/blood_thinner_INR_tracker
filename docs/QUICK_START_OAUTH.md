# Quick Start: OAuth Testing (30 Seconds)

## TL;DR

1. **Start API**: `dotnet run --project src/BloodThinnerTracker.Api`
2. **Open browser**: http://localhost:5000/oauth-test.html
3. **Click login**: Choose "Login with Google" or "Login with Azure AD"
4. **Copy token**: Click "Copy Token" button after successful login
5. **Use in Scalar**: Paste into Scalar UI at http://localhost:5000/scalar/v1

**That's it!** You now have a JWT token for testing protected endpoints.

---

## First-Time Setup (One-Time)

### Google OAuth Configuration

1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create OAuth 2.0 Client ID:
   - Application type: **Web application**
   - Authorized redirect URIs: `https://localhost:7000/api/auth/callback/google`
3. Copy **Client ID** and **Client Secret**
4. Add to `src/BloodThinnerTracker.Api/appsettings.Development.json`:
   ```json
   {
     "Google": {
       "ClientId": "your-client-id.apps.googleusercontent.com",
       "ClientSecret": "your-client-secret"
     }
   }
   ```

### Azure AD OAuth Configuration

1. Go to [Azure Portal](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Register new application:
   - Name: **Blood Thinner Tracker Dev**
   - Redirect URI: `https://localhost:7000/api/auth/callback/azuread`
3. Copy **Application (client) ID**, **Directory (tenant) ID**
4. Create **Client Secret** in "Certificates & secrets"
5. Add to `src/BloodThinnerTracker.Api/appsettings.Development.json`:
   ```json
   {
     "AzureAd": {
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret",
       "TenantId": "your-tenant-id"
     }
   }
   ```

---

## Using Your JWT Token

### In Scalar UI

1. Go to http://localhost:5000/scalar/v1
2. Click **"Authorize"** button (top right)
3. Select **"Bearer"** authentication
4. Paste your JWT token
5. Click **"Authorize"**
6. Try any protected endpoint (e.g., GET /api/medication)

### In cURL

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://localhost:7000/api/medication
```

### In Postman

1. Create new request
2. Go to **Authorization** tab
3. Type: **Bearer Token**
4. Token: Paste your JWT token
5. Send request

---

## Troubleshooting

### "OAuth configuration not found"
- Check `appsettings.Development.json` has Google/AzureAd sections
- Restart API after adding configuration

### "Redirect URI mismatch"
- Ensure OAuth provider has `https://localhost:7000/api/auth/callback/{provider}` registered
- Check port number matches (default: 7000 for HTTPS, 5000 for HTTP)

### "Token expired"
- JWT tokens expire after 15 minutes (security best practice)
- Simply get a new token from `/oauth-test.html` (takes 30 seconds)

### "Invalid state parameter"
- State tokens expire after 5 minutes
- Don't refresh the browser during OAuth flow
- Complete the OAuth flow within 5 minutes

---

## Next Steps

- **Full Documentation**: See [docs/OAUTH_TESTING_GUIDE.md](./OAUTH_TESTING_GUIDE.md)
- **Task Specification**: See [specs/tasks/T015i-oauth-test-page.md](../specs/tasks/T015i-oauth-test-page.md)
- **Authentication Guide**: See [docs/AUTHENTICATION_TESTING_GUIDE.md](./AUTHENTICATION_TESTING_GUIDE.md)

---

## Architecture

```
Browser                 API Server              OAuth Provider
   |                        |                          |
   |  GET /oauth-test.html  |                          |
   |----------------------->|                          |
   |    HTML + JS page      |                          |
   |<-----------------------|                          |
   |                        |                          |
   |  Click "Login"         |                          |
   |  GET /api/auth/external/google?redirectUri=/oauth-test.html
   |----------------------->|                          |
   |                        |  Generate CSRF state     |
   |                        |  Cache state (5 min)     |
   |                        |                          |
   |    302 Redirect to Google                         |
   |----------------------->|------------------------->|
   |                        |                          |
   |          User consents to access                  |
   |                        |                          |
   |    GET /api/auth/callback/google?code=...&state=...|
   |<-----------------------|<-------------------------|
   |                        |                          |
   |                        |  Validate state          |
   |                        |  Exchange code for token |
   |                        |  Validate ID token       |
   |                        |  Create/update user      |
   |                        |  Generate JWT            |
   |                        |                          |
   |    302 Redirect to /oauth-test.html?token=JWT     |
   |<-----------------------|                          |
   |                        |                          |
   |  Display token with    |                          |
   |  copy button           |                          |
   |                        |                          |
```

---

**Document Version**: 1.0  
**Last Updated**: January 2025
