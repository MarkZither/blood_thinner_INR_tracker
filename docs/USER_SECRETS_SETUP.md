# User Secrets Setup Guide

**Blood Thinner Tracker - OAuth Credentials Configuration**

This guide walks you through setting up OAuth credentials using ASP.NET Core User Secrets. This is a **required one-time setup** for each developer machine to enable Google and Azure AD authentication.

---

## 🔐 Why User Secrets?

OAuth credentials (Client IDs and Secrets) are **sensitive information** that must **never be committed to Git**. ASP.NET Core User Secrets provides a secure way to store these credentials locally on your development machine.

**User Secrets**:
- ✅ Stored outside the repository (in your user profile directory)
- ✅ Never committed to Git
- ✅ Unique to each developer
- ✅ Automatically loaded in Development environment
- ✅ Override values in `appsettings.Development.json`

---

## 📋 Prerequisites

Before starting, ensure you have:

- [x] .NET 10 SDK installed (`dotnet --version` should show 10.0.x)
- [x] Git repository cloned to your local machine
- [x] PowerShell or terminal access
- [x] Access to Google Cloud Console (for Google OAuth)
- [x] Access to Azure Portal (for Azure AD OAuth)

---

## 🚀 Quick Start

### Step 1: Initialize User Secrets

Open PowerShell and navigate to the API project:

```powershell
cd c:\Source\github\blood_thinner_INR_tracker\src\BloodThinnerTracker.Api

# Initialize User Secrets
dotnet user-secrets init
```

**Expected Output**:
```
Set UserSecretsId to 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx' for project 'BloodThinnerTracker.Api'
```

**What happened?**:
- A unique `UserSecretsId` was added to `BloodThinnerTracker.Api.csproj`
- A secrets directory was created at:
  - Windows: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\`
  - Linux/Mac: `~/.microsoft/usersecrets/{UserSecretsId}/`

---

### Step 2: Register OAuth Applications

You need to create OAuth applications in both Google Cloud Console and Azure Portal to get credentials.

#### Option A: Google OAuth (Recommended for Personal Use)

**2.1. Go to Google Cloud Console**

Visit: https://console.cloud.google.com/apis/credentials

**2.2. Create or Select a Project**

- Click "Select a project" → "New Project"
- Name: "Blood Thinner Tracker Dev"
- Click "Create"

**2.3. Enable APIs**

- Go to "Library" (left sidebar)
- Search for "Google+ API" → Click → "Enable"
- Search for "People API" → Click → "Enable"

**2.4. Create OAuth 2.0 Client ID**

1. Go to "Credentials" (left sidebar)
2. Click "Create Credentials" → "OAuth client ID"
3. If prompted, configure "OAuth consent screen":
   - User Type: **External**
   - App name: "Blood Thinner Tracker Dev"
   - User support email: Your email
   - Developer contact: Your email
   - Click "Save and Continue"
   - Scopes: Skip (click "Save and Continue")
   - Test users: Add your email
   - Click "Save and Continue"
4. Back to "Create OAuth client ID":
   - Application type: **Web application**
   - Name: "Blood Thinner Tracker Local Dev"
   - Authorized redirect URIs: Add both:
     - `https://localhost:7000/api/auth/callback/google`
     - `http://localhost:5026/api/auth/callback/google`
   - Click "Create"

**2.5. Copy Credentials**

- You'll see a dialog with:
  - **Client ID**: `123456789-abc123.apps.googleusercontent.com`
  - **Client Secret**: `GOCSPX-abc123xyz...`
- Click the 📋 copy button for each
- **Save these somewhere safe** (you'll use them in Step 3)

#### Option B: Azure AD OAuth (Recommended for Enterprise Use)

**2.6. Go to Azure Portal**

Visit: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade

**2.7. Register New Application**

1. Click "New registration"
2. Name: "Blood Thinner Tracker Dev"
3. Supported account types: **Accounts in this organizational directory only**
4. Redirect URI:
   - Platform: **Web**
   - URI: `https://localhost:7000/api/auth/callback/azuread`
5. Click "Register"

**2.8. Note Application IDs**

On the Overview page, copy:
- **Application (client) ID**: `ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj`
- **Directory (tenant) ID**: `aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee`

**2.9. Create Client Secret**

1. Go to "Certificates & secrets" (left sidebar)
2. Click "New client secret"
3. Description: "Local Development"
4. Expires: 180 days (6 months)
5. Click "Add"
6. **Copy the Value immediately** (you can only see it once!): `XYZ123~abc...`

**2.10. Configure API Permissions**

1. Go to "API permissions" (left sidebar)
2. Click "Add a permission"
3. Select "Microsoft Graph"
4. Select "Delegated permissions"
5. Search and check:
   - `openid`
   - `profile`
   - `email`
   - `User.Read`
6. Click "Add permissions"
7. Click "Grant admin consent for [Your Organization]" (if you have permission)

**2.11. Configure Authentication**

1. Go to "Authentication" (left sidebar)
2. Under "Implicit grant and hybrid flows":
   - ✅ Check "ID tokens (used for implicit and hybrid flows)"
3. Click "Save"

---

### Step 3: Configure User Secrets

Now store the credentials you copied in Step 2.

**Ensure you're in the API project directory**:
```powershell
cd c:\Source\github\blood_thinner_INR_tracker\src\BloodThinnerTracker.Api
```

#### Set Google Credentials

```powershell
# Replace with YOUR actual Client ID from Step 2.5
dotnet user-secrets set "Authentication:Google:ClientId" "123456789-abc123.apps.googleusercontent.com"

# Replace with YOUR actual Client Secret from Step 2.5
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-abc123xyz..."
```

#### Set Azure AD Credentials

```powershell
# Replace with YOUR actual Tenant ID from Step 2.8
dotnet user-secrets set "Authentication:AzureAd:TenantId" "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"

# Replace with YOUR actual Client ID from Step 2.8
dotnet user-secrets set "Authentication:AzureAd:ClientId" "ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj"

# Replace with YOUR actual Client Secret from Step 2.9
dotnet user-secrets set "Authentication:AzureAd:ClientSecret" "XYZ123~abc..."
```

---

### Step 4: Verify Configuration

**List all configured secrets**:
```powershell
dotnet user-secrets list
```

**Expected Output**:
```
Authentication:AzureAd:ClientId = ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj
Authentication:AzureAd:ClientSecret = XYZ123~abc...
Authentication:AzureAd:TenantId = aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
Authentication:Google:ClientId = 123456789-abc123.apps.googleusercontent.com
Authentication:Google:ClientSecret = GOCSPX-abc123xyz...
```

If you see your actual credentials (not placeholders), you're all set! ✅

---

### Step 5: Test OAuth Authentication

**Start the API**:
```powershell
# From repository root
cd c:\Source\github\blood_thinner_INR_tracker
dotnet run --project src/BloodThinnerTracker.AppHost
```

**Or** start just the API project:
```powershell
cd src/BloodThinnerTracker.Api
dotnet run
```

**Wait for**:
```
Now listening on: https://localhost:7000
Now listening on: http://localhost:5026
```

**Test OAuth Flows**:

1. **Open OAuth Test Page**:
   - Navigate to: http://localhost:5026/oauth-test.html
   - Or: https://localhost:7000/oauth-test.html

2. **Test Google Login**:
   - Click "Login with Google"
   - Sign in with your Google account
   - Authorize the application
   - Should redirect back with a JWT token
   - Click "Copy Token" to use in API requests

3. **Test Azure AD Login**:
   - Click "Login with Azure AD"
   - Sign in with your Microsoft account
   - Authorize the application
   - Should redirect back with a JWT token

**Success Criteria**:
- ✅ No 400 errors from Google
- ✅ No AADSTS900023 errors from Azure AD
- ✅ JWT token displayed on page
- ✅ Token can be copied and used

---

## 🧪 Testing with Scalar UI

After obtaining a token from `/oauth-test.html`:

1. **Navigate to Scalar UI**: http://localhost:5026/scalar/v1
2. Click "Authorize" button (top right)
3. Select "Bearer (JWT)"
4. Paste your token
5. Click "Authorize"
6. Test protected endpoints (e.g., `/api/users/me`)

---

## 🛠️ Troubleshooting

### Error: Google 400 "Malformed Request"

**Symptom**: Google OAuth returns 400 error

**Possible Causes**:
1. Client ID is still placeholder value
2. Client Secret is incorrect
3. Redirect URI mismatch

**Solutions**:

```powershell
# Check what's configured
dotnet user-secrets list | Select-String "Google"

# If shows placeholder, reconfigure:
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_REAL_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_REAL_CLIENT_SECRET"

# Restart API
```

**Verify Redirect URI**:
- In Google Cloud Console → Credentials → Your OAuth Client
- Authorized redirect URIs should include:
  - `https://localhost:7000/api/auth/callback/google`
  - `http://localhost:5026/api/auth/callback/google`
- Must match **exactly** (case-sensitive, no trailing slash)

---

### Error: Azure AD AADSTS900023 "Invalid Tenant"

**Symptom**: `Specified tenant identifier 'dev-tenant-id' is neither a valid DNS name, nor a valid external domain`

**Cause**: Placeholder tenant ID not replaced with real Tenant ID

**Solution**:

```powershell
# Check current configuration
dotnet user-secrets list | Select-String "AzureAd"

# Set real Tenant ID (should be a GUID)
dotnet user-secrets set "Authentication:AzureAd:TenantId" "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"

# Restart API
```

**Find Tenant ID**:
- Azure Portal → Azure Active Directory → Overview
- Look for "Tenant ID" (GUID format)

---

### Error: "Redirect URI Mismatch"

**Symptom**: OAuth provider shows `redirect_uri_mismatch` error

**Cause**: Redirect URI in OAuth provider doesn't match API configuration

**Solution**:

1. **Check API configuration** (`appsettings.Development.json`):
   - Google: `"CallbackPath": "/signin-google"` → API uses `/api/auth/callback/google`
   - Azure AD: `"CallbackPath": "/signin-oidc"` → API uses `/api/auth/callback/azuread`

2. **Update OAuth Provider**:
   - **Google Cloud Console**: 
     - Credentials → Your OAuth Client → Authorized redirect URIs
     - Add: `https://localhost:7000/api/auth/callback/google`
   - **Azure Portal**:
     - App Registration → Authentication → Redirect URIs
     - Add: `https://localhost:7000/api/auth/callback/azuread`

3. **Ensure exact match** (case-sensitive, include http/https, no trailing slash)

---

### Error: User Secrets Not Loading

**Symptom**: API still uses placeholder values despite configuring User Secrets

**Possible Causes**:
1. User Secrets not initialized
2. Running in wrong environment (Production instead of Development)
3. Secrets file corrupted

**Solutions**:

```powershell
# Check if initialized
dotnet user-secrets init

# Verify environment
$env:ASPNETCORE_ENVIRONMENT
# Should show: Development

# List secrets
dotnet user-secrets list

# If empty or wrong, reconfigure (see Step 3)
```

**Manual Check** (Windows):
```powershell
# Find secrets file location
$userSecretsId = (Select-Xml -Path "BloodThinnerTracker.Api.csproj" -XPath "//UserSecretsId").Node.InnerText
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets\$userSecretsId\secrets.json"

# Open file
notepad $secretsPath
```

Should contain JSON like:
```json
{
  "Authentication:Google:ClientId": "...",
  "Authentication:Google:ClientSecret": "...",
  "Authentication:AzureAd:TenantId": "...",
  "Authentication:AzureAd:ClientId": "...",
  "Authentication:AzureAd:ClientSecret": "..."
}
```

---

### Error: Secrets File Corrupted

**Symptom**: API fails to start, JSON parsing errors

**Solution**:

```powershell
# Clear all secrets
dotnet user-secrets clear

# Confirm
# Y

# Reconfigure from scratch (see Step 3)
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
# ... (continue with all credentials)
```

---

## 🔄 Managing User Secrets

### View All Secrets

```powershell
dotnet user-secrets list
```

### Remove a Specific Secret

```powershell
dotnet user-secrets remove "Authentication:Google:ClientId"
```

### Clear All Secrets

```powershell
dotnet user-secrets clear
```

### Update a Secret

```powershell
# Just set it again with new value (overwrites)
dotnet user-secrets set "Authentication:Google:ClientSecret" "NEW_SECRET_VALUE"
```

---

## 🔒 Security Best Practices

### ✅ DO

- **Store credentials in User Secrets** (development)
- **Use separate OAuth apps for each environment** (dev, staging, prod)
- **Rotate client secrets regularly** (every 6 months)
- **Use Azure Key Vault in production** (not User Secrets)
- **Enable MFA on OAuth provider accounts** (Google, Azure)
- **Review OAuth consent scopes** (request minimum necessary)
- **Keep secrets.json file permissions restricted** (user account only)

### ❌ DON'T

- **Never commit secrets to Git** (even accidentally)
- **Don't share User Secrets between developers** (each has their own)
- **Don't use User Secrets in production** (insecure for deployed apps)
- **Don't hardcode credentials in code** (always use configuration)
- **Don't reuse production credentials in development** (security risk)
- **Don't share OAuth Client Secrets via email/chat** (use secure channels)

---

## 📚 Additional Resources

### Official Documentation

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Google OAuth 2.0 Setup](https://developers.google.com/identity/protocols/oauth2)
- [Azure AD OAuth Setup](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)

### Project Documentation

- [OAuth Testing Guide](./OAUTH_TESTING_GUIDE.md) - How to test OAuth flows
- [API Documentation](./api/README.md) - API endpoints reference
- [Deployment Guide](./deployment/README.md) - Production configuration

### Related Tasks

- [T015k: User Secrets OAuth](../specs/tasks/T015k-user-secrets-oauth.md) - Task specification
- [T015: OAuth2 Implementation](../specs/tasks/T015-oauth2-redirect-flow.md) - Parent task

---

## 🆘 Getting Help

If you encounter issues not covered in this guide:

1. **Check Logs**: Run API with `ASPNETCORE_ENVIRONMENT=Development` and check console output
2. **Verify Configuration**: Use `dotnet user-secrets list` to confirm values
3. **Test Manually**: Use `/oauth-test.html` to see exact error messages
4. **Review OAuth Provider Logs**: Check Google/Azure admin consoles for errors
5. **Ask for Help**: Contact the development team with:
   - Error messages (without credentials!)
   - Steps you've tried
   - Output of `dotnet user-secrets list` (redact actual secrets)

---

## ✅ Setup Checklist

Complete this checklist to ensure proper setup:

- [ ] Cloned repository to local machine
- [ ] Installed .NET 10 SDK
- [ ] Navigated to `src/BloodThinnerTracker.Api`
- [ ] Ran `dotnet user-secrets init`
- [ ] Verified `UserSecretsId` in `.csproj` file
- [ ] Registered Google OAuth application (if using Google)
- [ ] Registered Azure AD application (if using Azure AD)
- [ ] Configured User Secrets with Google credentials
- [ ] Configured User Secrets with Azure AD credentials
- [ ] Ran `dotnet user-secrets list` to verify
- [ ] Started API (`dotnet run`)
- [ ] Tested Google login at `/oauth-test.html`
- [ ] Tested Azure AD login at `/oauth-test.html`
- [ ] Obtained JWT token successfully
- [ ] Tested token in Scalar UI
- [ ] No secrets committed to Git (`git status` is clean)

**All done?** You're ready to develop! 🎉

---

**Last Updated**: October 23, 2025  
**Document Version**: 1.0  
**Maintained By**: Development Team

