# Authentication Implementation Gap Analysis

**Date**: October 23, 2025  
**Issue**: T010 and T011 marked complete but OAuth2 not actually implemented

## 🚨 Critical Issues

### 1. Specification Says OAuth2 ONLY
- **spec.md** Line 20: "I can create an account with email and password" ← MISLEADING
- **plan.md** Line 34: "Azure AD/Google OAuth integration" ← CORRECT REQUIREMENT
- **plan.md** Line 122: "unified abstraction with platform-specific OAuth" ← CORRECT REQUIREMENT
- **research.md** Line 37: "MAUI: Native Azure AD/Google OAuth" ← CORRECT REQUIREMENT

### 2. Current Implementation is Fake
```csharp
// AuthenticationService.cs - Lines 100-122
public async Task<AuthenticationResponse?> AuthenticateAsync(LoginRequest request)
{
    // TODO: Implement user lookup and password verification
    // This is a placeholder implementation until User entity is created
    var user = new UserInfo
    {
        Id = Guid.NewGuid().ToString(),
        Email = request.Email,
        Name = request.Email.Split('@')[0],
        Role = "Patient",
        Provider = "Local",  // ← WRONG! Should be "AzureAD" or "Google"
        // ...
    };
    // Just returns a JWT for ANY input!
}
```

**Problem**: This accepts any email/password and returns a valid JWT token. No actual authentication happening.

### 3. LoginRequest Model is Wrong

```csharp
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;  // ← SHOULD NOT EXIST!
    
    public bool RememberMe { get; set; } = false;
    public string? DeviceId { get; set; }
    public string? TwoFactorCode { get; set; }
}
```

**Problem**: For OAuth2, you don't send username/password to your API. The flow is:
1. User clicks "Sign in with Google/Microsoft"
2. Redirect to Google/Microsoft login
3. OAuth provider authenticates user
4. Provider redirects back with auth code
5. Your API exchanges code for user info
6. Your API issues JWT with user claims

### 4. OAuth2 Infrastructure Exists But Not Wired Up

The code **does** have OAuth2 setup in `AuthenticationExtensions.cs`:
- Google OAuth configured (lines 100-125)
- Azure AD OpenID Connect configured (lines 126-151)

But:
- No endpoints to trigger OAuth flow
- No callback handlers implemented
- AuthController.Login() doesn't use any of it
- `AuthenticateExternalAsync()` method exists but never called

### 5. Missing OAuth2 Endpoints

These endpoints don't exist:
```
POST /api/auth/external/google        // Initiate Google OAuth
POST /api/auth/external/azuread       // Initiate Azure AD OAuth
GET  /api/auth/callback/google        // Google callback handler
GET  /api/auth/callback/azuread       // Azure AD callback handler
```

## ✅ What SHOULD Exist

### Correct LoginRequest for OAuth2
```csharp
public class ExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // "Google" or "AzureAD"
    
    public string? DeviceId { get; set; }
    
    // For mobile apps using platform-native OAuth
    public string? IdToken { get; set; }
    
    // For web apps using authorization code flow
    public string? AuthorizationCode { get; set; }
    public string? RedirectUri { get; set; }
}
```

**NO PASSWORD FIELD!**

### Correct OAuth2 Flow

#### Web/Blazor Flow:
1. User clicks "Sign in with Google"
2. `GET /api/auth/external/google` → Redirects to `https://accounts.google.com/o/oauth2/v2/auth`
3. User authenticates with Google
4. Google redirects to `GET /api/auth/callback/google?code=ABC123`
5. API exchanges code for user info
6. API creates/updates User entity
7. API returns JWT tokens
8. Web app stores JWT and user is logged in

#### Mobile/MAUI Flow:
1. User clicks "Sign in with Google"
2. MAUI uses native `WebAuthenticator.AuthenticateAsync()`
3. Opens system browser for Google login
4. Google returns ID token to app
5. App sends ID token to `POST /api/auth/external/google`
6. API validates ID token with Google
7. API creates/updates User entity
8. API returns JWT tokens
9. App stores JWT in secure storage

## 📋 Required Changes

### Change 1: Update spec.md US1 Scenario 1
```markdown
<!-- WRONG -->
1. **Given** I am a new user, **When** I open the app, **Then** I can create an account with email and password

<!-- CORRECT -->
1. **Given** I am a new user, **When** I open the app, **Then** I can sign in with my Microsoft or Google account
```

### Change 2: Replace LoginRequest Model
```csharp
// DELETE: LoginRequest.cs (with Password field)
// CREATE: ExternalLoginRequest.cs (OAuth2 only)
public class ExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;
    
    [Required] 
    public string IdToken { get; set; } = string.Empty; // For mobile
    
    public string? AuthorizationCode { get; set; }      // For web
    public string? RedirectUri { get; set; }
    public string? DeviceId { get; set; }
}
```

### Change 3: Add OAuth2 Endpoints to AuthController
```csharp
[HttpGet("external/{provider}")]
public IActionResult ExternalLogin(string provider, [FromQuery] string? returnUrl = null)
{
    // Initiate OAuth flow - redirects to Google/Microsoft
}

[HttpGet("callback/{provider}")]
public async Task<ActionResult<AuthenticationResponse>> ExternalCallback(
    string provider, 
    [FromQuery] string code, 
    [FromQuery] string? state)
{
    // Handle OAuth callback, exchange code for user info
}

[HttpPost("external/mobile")]
public async Task<ActionResult<AuthenticationResponse>> ExternalLoginMobile(
    [FromBody] ExternalLoginRequest request)
{
    // Handle mobile OAuth with ID token
}
```

### Change 4: Implement Real OAuth2 in AuthenticationService
```csharp
public async Task<AuthenticationResponse?> AuthenticateExternalAsync(
    string provider, 
    string idToken,  // Changed from externalId
    string? authCode = null)
{
    // 1. Validate ID token with Google/Microsoft
    // 2. Extract user claims (email, name, external ID)
    // 3. Find or create User entity in database
    // 4. Generate JWT tokens
    // 5. Return AuthenticationResponse
}
```

### Change 5: Update Tasks

**T010**: 
```markdown
<!-- CURRENT (WRONG) -->
- [x] T010 [P] Implement authentication abstraction and OAuth2 integration (Azure AD, Google)

<!-- SHOULD BE -->
- [ ] T010 [P] Implement authentication abstraction and OAuth2 integration (Azure AD, Google)
  - [x] T010a Add OAuth2 middleware and configuration (Google, Azure AD)
  - [ ] T010b Implement OAuth2 initiation endpoints (/auth/external/{provider})
  - [ ] T010c Implement OAuth2 callback handlers (/auth/callback/{provider})
  - [ ] T010d Implement ID token validation for mobile flows
  - [ ] T010e Create ExternalLoginRequest model (remove password-based LoginRequest)
  - [ ] T010f Update AuthenticationService.AuthenticateExternalAsync() with real OAuth logic
```

**T011**:
```markdown
<!-- CURRENT (WRONG) -->
- [x] T011 [P] Add JWT token issuance and validation middleware

<!-- SHOULD BE -->
- [ ] T011 [P] Add JWT token issuance and validation middleware
  - [x] T011a Implement JwtTokenService.GenerateAccessToken()
  - [x] T011b Add JWT Bearer authentication middleware
  - [ ] T011c Connect JWT generation to OAuth2-authenticated users (not fake users)
  - [ ] T011d Add refresh token storage in database
  - [ ] T011e Implement token revocation endpoint
```

**T015**:
```markdown
<!-- CURRENT (WRONG) -->
- [x] T015 [US1] Implement user registration and login endpoints

<!-- SHOULD BE -->
- [ ] T015 [US1] Implement OAuth2 user registration and login endpoints
  - [ ] T015a Remove password-based login endpoint
  - [ ] T015b Add OAuth2 web flow endpoints (initiate, callback)
  - [ ] T015c Add OAuth2 mobile endpoint (ID token exchange)
  - [ ] T015d Implement automatic user creation on first OAuth login
  - [ ] T015e Add User entity to database with ExternalUserId field
```

## 🎯 Priority Actions

### Immediate (Blocking)
1. **Update spec.md** - Remove "email and password" language
2. **Update tasks.md** - Uncheck T010, T011, T015 and add subtasks
3. **Create ExternalLoginRequest.cs** - Replace LoginRequest
4. **Document OAuth2 flows** - Create diagrams showing web vs mobile flows

### Short-term (This Sprint)
5. **Implement OAuth2 endpoints** in AuthController
6. **Wire up existing OAuth middleware** to actual endpoints
7. **Add User entity** to database with OAuth fields
8. **Implement token validation** (Google/Microsoft ID tokens)

### Medium-term (Next Sprint)  
9. **Update Mobile app** to use native OAuth
10. **Update Blazor Web app** to use OAuth redirect flow
11. **Add integration tests** for OAuth flows
12. **Remove all password-based code**

## 🔐 Security Implications

**Current State**: CRITICALLY INSECURE
- Accepts any email/password
- Returns valid JWTs for any input
- No actual user verification
- Would allow anyone to impersonate any user

**After OAuth2**: SECURE
- Users authenticate with Microsoft/Google (proven identity)
- No password handling in our system
- Reduced attack surface
- Industry-standard security

## 📚 References

- [OAuth 2.0 for Mobile Apps (RFC 8252)](https://tools.ietf.org/html/rfc8252)
- [Azure AD OAuth2](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Google OAuth2](https://developers.google.com/identity/protocols/oauth2)
- [ASP.NET Core External Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)
