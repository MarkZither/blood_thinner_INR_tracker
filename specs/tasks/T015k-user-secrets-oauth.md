# T015k: Configure User Secrets for OAuth Credentials

**Epic**: T015 - OAuth2 Redirect Flow Implementation  
**Parent Task**: T015 - Implement OAuth2 redirect endpoints  
**Status**: ✅ Complete  
**Priority**: Critical (Security)  
**Estimated Effort**: 30 minutes  
**Actual Effort**: 30 minutes  

---

## Overview

Configure ASP.NET Core User Secrets to store OAuth credentials (Google Client ID/Secret, Azure AD credentials) outside the repository, preventing accidental commit of sensitive data to source control.

**Problem**: Currently, `appsettings.Development.json` contains placeholder OAuth credentials (`"dev-client-id"`, `"dev-tenant-id"`) which cause authentication failures. Real credentials must be configured but **cannot be committed to Git**.

**Solution**: Use ASP.NET Core User Secrets to store OAuth credentials locally on each developer's machine. User Secrets are stored outside the project directory (`%APPDATA%\Microsoft\UserSecrets\` on Windows) and never committed to source control.

---

## Requirements

### Functional Requirements

**FR-T015k-1**: User Secrets Initialization
- Initialize User Secrets for `BloodThinnerTracker.Api` project
- Generate unique UserSecretsId GUID in `.csproj` file
- User Secrets stored in user profile directory (not in repository)

**FR-T015k-2**: OAuth Credentials Configuration
- Store Google OAuth credentials in User Secrets (ClientId, ClientSecret)
- Store Azure AD OAuth credentials in User Secrets (TenantId, ClientId, ClientSecret)
- User Secrets override `appsettings.Development.json` values at runtime

**FR-T015k-3**: Developer Documentation
- Provide step-by-step guide for obtaining OAuth credentials
- Document User Secrets setup commands for each developer
- Include troubleshooting for common OAuth configuration errors

**FR-T015k-4**: Placeholder Removal
- Remove real credentials from `appsettings.Development.json`
- Replace with clear placeholder values indicating User Secrets required
- Add comments explaining User Secrets configuration

### Non-Functional Requirements

**NFR-T015k-1**: Security
- No OAuth credentials committed to Git (ever)
- User Secrets file permissions restricted to user account
- `.gitignore` already excludes User Secrets directory
- Production credentials use Azure Key Vault (not User Secrets)

**NFR-T015k-2**: Developer Experience
- Simple one-time setup per developer machine
- Clear error messages if credentials not configured
- Documentation provides exact commands to run
- No manual file editing required

**NFR-T015k-3**: Maintainability
- User Secrets configuration documented in repository
- Template commands provided for easy copy-paste
- OAuth provider setup instructions included
- Team can share setup guide with new developers

---

## Implementation

### File Changes

**Modified**:
1. `src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj` - Add UserSecretsId
2. `src/BloodThinnerTracker.Api/appsettings.Development.json` - Update with placeholder comments

**Created**:
1. `docs/USER_SECRETS_SETUP.md` - Complete setup guide for developers
2. `specs/tasks/T015k-user-secrets-oauth.md` - This task specification

**User Secrets File** (created locally, **never committed**):
- Location: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json` (Windows)
- Location: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json` (Linux/Mac)

---

## Step-by-Step Implementation

### Step 1: Initialize User Secrets

```powershell
# Navigate to API project directory
cd src/BloodThinnerTracker.Api

# Initialize User Secrets (generates GUID and adds to .csproj)
dotnet user-secrets init

# Output: Set UserSecretsId to '{GUID}' for project 'BloodThinnerTracker.Api'
```

**What this does**:
- Adds `<UserSecretsId>{GUID}</UserSecretsId>` to `BloodThinnerTracker.Api.csproj`
- GUID is committed to Git (safe, it's just an identifier)
- Creates secrets storage directory for this project

### Step 2: Set Google OAuth Credentials

**First, obtain credentials from Google Cloud Console:**
1. Go to https://console.cloud.google.com/apis/credentials
2. Create "OAuth 2.0 Client ID" (Web application)
3. Add authorized redirect URI: `https://localhost:7000/api/auth/callback/google`
4. Copy Client ID and Client Secret

**Then, configure User Secrets:**
```powershell
# Set Google Client ID
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"

# Set Google Client Secret
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

### Step 3: Set Azure AD OAuth Credentials

**First, obtain credentials from Azure Portal:**
1. Go to https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade
2. Click "New registration"
3. Name: "Blood Thinner Tracker Dev"
4. Redirect URI: `https://localhost:7000/api/auth/callback/azuread`
5. Create client secret in "Certificates & secrets"
6. Copy Application (client) ID, Directory (tenant) ID, and Client Secret

**Then, configure User Secrets:**
```powershell
# Set Azure AD Tenant ID
dotnet user-secrets set "Authentication:AzureAd:TenantId" "YOUR_TENANT_ID"

# Set Azure AD Client ID  
dotnet user-secrets set "Authentication:AzureAd:ClientId" "YOUR_CLIENT_ID"

# Set Azure AD Client Secret
dotnet user-secrets set "Authentication:AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
```

### Step 4: Verify Configuration

```powershell
# List all configured secrets
dotnet user-secrets list

# Output should show:
# Authentication:Google:ClientId = YOUR_GOOGLE_CLIENT_ID...
# Authentication:Google:ClientSecret = YOUR_GOOGLE_CLIENT_SECRET
# Authentication:AzureAd:TenantId = YOUR_TENANT_ID
# Authentication:AzureAd:ClientId = YOUR_CLIENT_ID
# Authentication:AzureAd:ClientSecret = YOUR_CLIENT_SECRET
```

### Step 5: Update appsettings.Development.json

Replace credential values with clear placeholders:

```json
{
  "Authentication": {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "CONFIGURED_VIA_USER_SECRETS",
      "ClientId": "CONFIGURED_VIA_USER_SECRETS",
      "ClientSecret": "CONFIGURED_VIA_USER_SECRETS",
      "CallbackPath": "/signin-oidc",
      "Scopes": ["openid", "profile", "email"]
    },
    "Google": {
      "ClientId": "CONFIGURED_VIA_USER_SECRETS",
      "ClientSecret": "CONFIGURED_VIA_USER_SECRETS",
      "CallbackPath": "/signin-google",
      "Scopes": ["openid", "profile", "email"]
    }
  }
}
```

Add comment at top of file:
```json
{
  "//": "OAuth credentials are configured via User Secrets for security.",
  "//": "See docs/USER_SECRETS_SETUP.md for setup instructions.",
  "//": "Run: dotnet user-secrets list (in src/BloodThinnerTracker.Api)"
}
```

---

## Testing

### Test Cases

**TC-T015k-1**: User Secrets Initialized ✅
1. Navigate to `src/BloodThinnerTracker.Api`
2. Run `dotnet user-secrets init`
3. Check `BloodThinnerTracker.Api.csproj` for `<UserSecretsId>` element
4. Verify GUID is valid format
**Expected**: UserSecretsId added to .csproj file

**TC-T015k-2**: Google OAuth Configuration ✅
1. Set Google credentials via `dotnet user-secrets set`
2. Run `dotnet user-secrets list`
3. Verify both ClientId and ClientSecret are listed
4. Start API and test Google login at `/oauth-test.html`
**Expected**: Google OAuth flow works, no 400 error

**TC-T015k-3**: Azure AD OAuth Configuration ✅
1. Set Azure AD credentials via `dotnet user-secrets set`
2. Run `dotnet user-secrets list`
3. Verify TenantId, ClientId, and ClientSecret are listed
4. Start API and test Azure AD login at `/oauth-test.html`
**Expected**: Azure AD OAuth flow works, no AADSTS900023 error

**TC-T015k-4**: Configuration Override ✅
1. Verify `appsettings.Development.json` has placeholder values
2. Start API with User Secrets configured
3. Check application logs for OAuth configuration (if logging enabled)
4. Test OAuth login flows
**Expected**: User Secrets override appsettings.Development.json

**TC-T015k-5**: No Secrets Committed ✅
1. Run `git status` in repository
2. Check for any modified files containing real credentials
3. Verify User Secrets directory not in repository
4. Check `.gitignore` excludes User Secrets
**Expected**: No secrets in Git working directory

---

## Security Considerations

### Why User Secrets?

**Problems with committing credentials**:
- ❌ Credentials exposed in Git history (even if removed later)
- ❌ Credentials visible in GitHub/Azure DevOps
- ❌ Credentials shared with all developers (security risk)
- ❌ Difficult to rotate credentials (must update in repository)
- ❌ Violates principle of least privilege

**Benefits of User Secrets**:
- ✅ Credentials stored outside repository
- ✅ Each developer has their own credentials
- ✅ Credentials never committed to Git
- ✅ Easy to rotate (just update locally)
- ✅ Works seamlessly with ASP.NET Core configuration

### User Secrets Limitations

**NOT for production**:
- User Secrets are **development only**
- Production should use:
  - **Azure Key Vault** (recommended)
  - **AWS Secrets Manager**
  - **Environment variables** (encrypted)
  - **Kubernetes Secrets**

**Security level**:
- User Secrets stored as **plain text** on local machine
- Protected by file system permissions (user account only)
- Better than Git, but not encrypted at rest
- Adequate for development, insufficient for production

### OAuth Security Best Practices

**Google OAuth**:
- ✅ Use separate Client ID for each environment (dev, staging, prod)
- ✅ Configure authorized redirect URIs (exact match required)
- ✅ Restrict Client ID to specific domains in production
- ✅ Enable "Consent Screen" with proper branding
- ✅ Request minimum scopes needed (openid, profile, email)

**Azure AD OAuth**:
- ✅ Use separate App Registration for each environment
- ✅ Configure redirect URIs (exact match required)
- ✅ Use client secrets with expiration dates
- ✅ Rotate secrets before expiration
- ✅ Enable "ID tokens" in Authentication settings
- ✅ Configure API permissions (Microsoft Graph User.Read)

---

## Documentation

### Updated Documentation

**Created**:
1. `docs/USER_SECRETS_SETUP.md` - Step-by-step setup guide
2. `specs/tasks/T015k-user-secrets-oauth.md` - This task specification

**Updated**:
1. `README.md` - Add User Secrets setup to Quick Start
2. `docs/QUICK_START_OAUTH.md` - Add User Secrets prerequisite
3. `docs/OAUTH_TESTING_GUIDE.md` - Update configuration section

### Developer Onboarding Checklist

New developers should:
- [ ] Clone repository
- [ ] Install .NET 10 SDK
- [ ] Navigate to `src/BloodThinnerTracker.Api`
- [ ] Run `dotnet user-secrets init` (if not already initialized)
- [ ] Follow `docs/USER_SECRETS_SETUP.md` to configure OAuth credentials
- [ ] Run `dotnet user-secrets list` to verify
- [ ] Start API and test OAuth at `/oauth-test.html`

---

## Common Errors and Solutions

### Error 1: Google 400 "Malformed Request"

**Symptom**: Google OAuth returns 400 error
**Cause**: Invalid or missing Client ID/Secret
**Solution**:
```powershell
# Check current configuration
dotnet user-secrets list

# Reconfigure with correct values
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_REAL_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_REAL_CLIENT_SECRET"

# Restart API
```

### Error 2: Azure AD AADSTS900023 "Invalid Tenant"

**Symptom**: `Specified tenant identifier 'dev-tenant-id' is neither a valid DNS name, nor a valid external domain`
**Cause**: Placeholder tenant ID not replaced with real Tenant ID
**Solution**:
```powershell
# Set real Tenant ID (GUID from Azure Portal)
dotnet user-secrets set "Authentication:AzureAd:TenantId" "YOUR_REAL_TENANT_ID"

# Restart API
```

### Error 3: "Redirect URI Mismatch"

**Symptom**: OAuth provider shows "redirect_uri_mismatch" error
**Cause**: Redirect URI in OAuth provider doesn't match API configuration
**Solution**:
1. Check API redirect URI: `/api/auth/callback/{provider}`
2. Update in OAuth provider console:
   - Google: `https://localhost:7000/api/auth/callback/google`
   - Azure AD: `https://localhost:7000/api/auth/callback/azuread`
3. Ensure exact match (case-sensitive, trailing slashes matter)

### Error 4: User Secrets Not Found

**Symptom**: API uses placeholder values instead of User Secrets
**Cause**: User Secrets not initialized or configured
**Solution**:
```powershell
# Check if initialized
dotnet user-secrets init

# List current secrets
dotnet user-secrets list

# If empty, configure secrets (see Step 2 and Step 3 above)
```

### Error 5: Secrets File Corrupted

**Symptom**: API fails to start, JSON parsing errors
**Cause**: Malformed `secrets.json` file
**Solution**:
```powershell
# Clear all secrets
dotnet user-secrets clear

# Reconfigure from scratch
dotnet user-secrets set "Authentication:Google:ClientId" "..."
# (repeat for all credentials)
```

---

## Acceptance Criteria

- [x] ✅ User Secrets initialized for BloodThinnerTracker.Api project
- [x] ✅ UserSecretsId added to `.csproj` file
- [x] ✅ Google OAuth credentials configured via User Secrets
- [x] ✅ Azure AD OAuth credentials configured via User Secrets
- [x] ✅ `appsettings.Development.json` updated with placeholder values
- [x] ✅ Comments added to appsettings.json explaining User Secrets
- [x] ✅ `docs/USER_SECRETS_SETUP.md` created with complete guide
- [x] ✅ No OAuth credentials committed to Git
- [x] ✅ `dotnet user-secrets list` shows all configured credentials
- [x] ✅ Google OAuth login works at `/oauth-test.html`
- [x] ✅ Azure AD OAuth login works at `/oauth-test.html`
- [x] ✅ Build succeeds with User Secrets configuration
- [x] ✅ README.md updated with User Secrets setup instructions

---

## Technical Details

### How User Secrets Work

**Configuration Hierarchy** (bottom overrides top):
1. `appsettings.json` (base configuration)
2. `appsettings.Development.json` (environment-specific)
3. **User Secrets** (development only)
4. Environment variables
5. Command-line arguments

**Configuration Binding**:
```csharp
// ASP.NET Core automatically loads User Secrets in Development
var builder = WebApplication.CreateBuilder(args);

// Access configuration (User Secrets override appsettings)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
// Returns User Secrets value, not "CONFIGURED_VIA_USER_SECRETS"
```

**User Secrets File Location**:
- Windows: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
- Linux: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`
- macOS: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`

**Example secrets.json**:
```json
{
  "Authentication:Google:ClientId": "123456789.apps.googleusercontent.com",
  "Authentication:Google:ClientSecret": "GOCSPX-abc123...",
  "Authentication:AzureAd:TenantId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "Authentication:AzureAd:ClientId": "ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj",
  "Authentication:AzureAd:ClientSecret": "XYZ123~abc..."
}
```

### Production Configuration

**Azure App Service**:
```powershell
# Set Application Settings (encrypted, managed by Azure)
az webapp config appsettings set --name myapp --settings \
  "Authentication__Google__ClientId"="..." \
  "Authentication__Google__ClientSecret"="..."
```

**Azure Key Vault** (recommended):
```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myvault.vault.azure.net/"),
    new DefaultAzureCredential());

// Secrets stored in Key Vault:
// - Authentication--Google--ClientId
// - Authentication--Google--ClientSecret
```

**Environment Variables**:
```bash
export Authentication__Google__ClientId="..."
export Authentication__Google__ClientSecret="..."
```

---

## Dependencies

**Depends On**:
- T015b: OAuth initiation endpoint (needs credentials to redirect)
- T015c: OAuth callback handler (needs credentials to validate tokens)
- T015i: OAuth test page (users test with real credentials)

**Enables**:
- ✅ Working OAuth authentication
- ✅ End-to-end testing of OAuth flows
- ✅ Secure credential management
- ✅ Team collaboration without sharing secrets

---

## Completion Notes

**Completed**: October 23, 2025  
**Implementation Time**: 30 minutes  

### What Was Delivered

1. **User Secrets Initialization**
   - UserSecretsId added to `.csproj`
   - Secrets storage directory created

2. **OAuth Credentials Configuration**
   - Google ClientId and ClientSecret in User Secrets
   - Azure AD TenantId, ClientId, and ClientSecret in User Secrets
   - Configuration verified with `dotnet user-secrets list`

3. **Security Improvements**
   - No credentials in Git repository
   - Placeholder values in `appsettings.Development.json`
   - Comments explaining User Secrets requirement

4. **Developer Documentation**
   - Complete setup guide (`USER_SECRETS_SETUP.md`)
   - Task specification (`T015k-user-secrets-oauth.md`)
   - Updated Quick Start documentation

### Verification

**Test Results**:
- ✅ Google OAuth login successful (no 400 error)
- ✅ Azure AD OAuth login successful (no AADSTS900023 error)
- ✅ JWT tokens generated correctly
- ✅ Tokens work in Scalar UI
- ✅ No secrets in `git status`

### Known Issues
None

### Future Enhancements

1. **Azure Key Vault Integration** (production)
   - Migrate from User Secrets to Key Vault for deployed environments
   - Use Managed Identity for secure access

2. **Credential Rotation**
   - Document client secret rotation process
   - Set up expiration alerts for Azure AD secrets

3. **Team Secrets Sharing** (optional)
   - Consider shared development credentials for team testing
   - Use Azure Key Vault for shared dev environment

4. **CI/CD Configuration**
   - Configure GitHub Actions secrets for automated testing
   - Use service principals for CI/CD OAuth testing

---

**Task Complete** ✅  
**Ready for**: OAuth authentication testing  
**Next Task**: T018c - Blazor Web UI Authentication  

---

**Generated**: October 23, 2025  
**Document Version**: 1.0  
**Maintained By**: Development Team
