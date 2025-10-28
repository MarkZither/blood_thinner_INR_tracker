# Authentication Implementation: Phased Hybrid Approach

**Project**: Blood Thinner Medication & INR Tracker  
**Created**: 2025-10-23  
**Status**: Planning → Implementation  
**Architecture**: Hybrid OAuth2 + mTLS

---

## Executive Summary

This document outlines the **three-phase authentication implementation strategy** combining OAuth2 (user authentication) with mutual TLS (testing and integrations). This hybrid approach provides:

- ✅ **End Users**: Secure OAuth2 authentication (Azure AD, Google)
- ✅ **Developers**: Easy API testing via Swagger with OAuth redirect
- ✅ **QA/CI-CD**: Automated testing with mTLS (no user interaction)
- ✅ **Integrations**: Future healthcare system connections via mTLS

---

## Three Authentication Methods

### 1. OAuth2 ID Token Exchange (Mobile) - ✅ COMPLETED

**Status**: Fully implemented and tested  
**Use Case**: iOS and Android mobile applications  
**Implementation**: T010d (IdTokenValidationService), T011d (RefreshToken entity)

**How it works**:
```
Mobile App → Platform OAuth (Sign in with Apple/Google) → ID Token
→ POST /api/auth/external/mobile with ID token
→ API validates token → Creates/updates user → Returns JWT tokens
→ App stores tokens securely (Keychain/Keystore)
```

**Key Features**:
- ✅ Real token validation (Microsoft.Identity.Web, Google.Apis.Auth)
- ✅ Automatic user creation on first login
- ✅ Refresh token rotation (7-day lifetime)
- ✅ SHA-256 token hashing in database
- ✅ Device tracking and multi-device support

**Files Modified**:
- `IdTokenValidationService.cs` - Token validation logic
- `AuthenticationService.cs` - User creation and JWT generation
- `RefreshToken.cs` entity - Persistent token storage
- Database migration: RefreshTokens table

---

### 2. OAuth2 Redirect Flow (Web + Swagger) - ⏳ NEXT PHASE

**Status**: To be implemented (T015b-h)  
**Use Case**: Web applications, Swagger UI testing, backend-driven auth  
**Priority**: HIGH (unblocks API testing and Web UI development)

**How it works**:
```
User clicks "Sign in" → GET /api/auth/external/{provider}
→ API redirects to Azure AD/Google consent page
→ User authenticates → Provider redirects to callback
→ GET /api/auth/callback/{provider}?code=...
→ API exchanges code for tokens → Validates ID token → Returns JWT
→ Web app stores tokens → Calls API with Authorization header
```

**Implementation Tasks (T015b-h)**:

| Task | Description | Effort | Files |
|------|-------------|--------|-------|
| T015a | Remove password-based endpoints | 15 min | AuthController.cs, LoginRequest.cs |
| T015b | Add initiation endpoint | 1 hr | AuthController.cs |
| T015c | Add callback handler | 1.5 hr | AuthController.cs |
| T015d | Mobile endpoint (use existing T010d) | 30 min | AuthController.cs |
| T015e | Auto user creation (reuse existing) | 15 min | AuthenticationService.cs |
| T015g | Swagger OAuth configuration | 30 min | Program.cs |
| T015h | Distributed cache for state | 30 min | Program.cs |
| **Total** | **Full OAuth redirect implementation** | **~4-5 hours** | |

**Key Features**:
- CSRF protection with state parameter (5-minute cache expiration)
- Authorization code exchange (not implicit flow - more secure)
- ID token validation (reuses existing IdTokenValidationService)
- Automatic user creation (reuses existing logic)
- Swagger "Authorize" button integration
- Web UI delegation (API handles OAuth, not client-side)

**Testing Workflow**:
```bash
# 1. Open Swagger UI
https://localhost:7000/scalar/v1

# 2. Click "Authorize" button
# 3. Select provider: Google or Azure AD
# 4. Complete OAuth flow in popup
# 5. Swagger stores tokens automatically
# 6. All API calls include Authorization: Bearer {token}
```

---

### 3. Mutual TLS (mTLS) - 🔮 FUTURE PHASE

**Status**: To be implemented (T046a-l)  
**Use Case**: Automated testing, CI/CD pipelines, healthcare integrations  
**Priority**: MEDIUM (improves testing workflow but not required for MVP)

**How it works**:
```
Client presents X.509 certificate → Server validates certificate
→ Extracts subject/CN → Looks up integration partner in database
→ If valid, grants API access with partner permissions
→ No user session required
```

**Implementation Tasks (T046a-l)**:

| Task | Description | Effort | Files |
|------|-------------|--------|-------|
| T046a | Install certificate package | 5 min | BloodThinnerTracker.Api.csproj |
| T046b | IntegrationPartner entity | 30 min | Models/IntegrationPartner.cs |
| T046c | Database migration | 15 min | Migrations/ |
| T046d | Certificate authentication middleware | 1 hr | Program.cs |
| T046e | ICertificateValidationService interface | 30 min | Services/ICertificateValidationService.cs |
| T046f | CertificateValidationService implementation | 1 hr | Services/CertificateValidationService.cs |
| T046g | Admin endpoints (register/revoke) | 1 hr | Controllers/IntegrationPartnerController.cs |
| T046h | Multi-scheme authorization | 30 min | All API controllers |
| T046i | Certificate generation scripts | 30 min | tools/scripts/ |
| T046j | Documentation updates | 30 min | docs/AUTHENTICATION_TESTING_GUIDE.md |
| T046k | Integration tests | 1 hr | tests/BloodThinnerTracker.Api.Tests/ |
| T046l | Audit logging | 30 min | Models/CertificateAuditLog.cs |
| **Total** | **Full mTLS implementation** | **~7-8 hours** | |

**Key Features**:
- X.509 certificate validation (expiry, CA trust, revocation)
- OCSP revocation checking
- Subject/CN matching against registered partners
- Granular permissions model (JSON array in database)
- Security audit logging (success + failed attempts)
- Admin endpoints for partner lifecycle management

**Testing Workflow**:
```bash
# 1. Generate test certificate
tools/scripts/generate-test-cert.ps1

# 2. Register certificate (admin operation)
POST /api/admin/integration-partners
{
  "name": "Test Automation",
  "certificateSubject": "CN=test-integration",
  "certificateThumbprint": "ABC123...",
  "permissions": ["medication:read", "inr:read"]
}

# 3. Test API with certificate
curl --cert client-cert.pem --key client-key.pem \
  https://localhost:7000/api/medications

# 4. Revoke certificate after testing
PUT /api/admin/integration-partners/{id}/revoke
```

**Use Cases**:
- ✅ **CI/CD Pipelines**: Generate ephemeral certificate, run tests, revoke
- ✅ **Development Tools**: Postman, curl with client certificates
- ✅ **Integration Partners**: Hospital EMR systems (HL7/FHIR)
- ✅ **Internal Services**: Microservice-to-microservice authentication

---

## Implementation Timeline

### Phase 1: Mobile OAuth (COMPLETED ✅)
**Duration**: ~6 hours (already done)  
**Delivered**:
- ID token validation service
- Refresh token entity and migrations
- User auto-creation logic
- Token rotation and revocation

**Validation**:
```bash
# Working test command
curl -X POST https://localhost:7000/api/auth/external/mobile \
  -H "Content-Type: application/json" \
  -d '{"provider":"Google","idToken":"...","deviceId":"test-device"}'
# Returns: {"accessToken":"...","refreshToken":"..."}
```

---

### Phase 2: OAuth Redirect (NEXT ⏳)
**Duration**: ~4-5 hours  
**Delivers**:
- Swagger "Authorize" button works with real OAuth
- Web UI can delegate authentication to API
- No more fake/password authentication
- Complete end-to-end OAuth flow testing

**Success Criteria**:
- [ ] Swagger UI OAuth flow completes successfully
- [ ] Developer can test all API endpoints with authenticated session
- [ ] Web UI can authenticate users via API redirect flow
- [ ] Password-based authentication completely removed
- [ ] All authentication flows tested (web + mobile)

**Blockers**: None - all dependencies complete (T010d, T011d)

---

### Phase 3: mTLS (FUTURE 🔮)
**Duration**: ~7-8 hours  
**Delivers**:
- CI/CD integration tests without OAuth
- Healthcare system integration capability
- Development tools testing (Postman, curl)
- Security audit trail for certificate authentication

**Success Criteria**:
- [ ] CI/CD pipeline runs integration tests with mTLS
- [ ] Certificate registration/revocation workflow functional
- [ ] OCSP revocation checking operational
- [ ] Security audit logging capturing all attempts
- [ ] Documentation complete for integration partners

**Blockers**: None - can be developed in parallel with other features after T015

---

## Architecture Decisions

### Why Hybrid (OAuth + mTLS)?

**OAuth2 Strengths**:
- ✅ User-friendly (no password management)
- ✅ Industry standard for user authentication
- ✅ Provider-managed security (Azure AD, Google)
- ✅ Mobile platform integration (Sign in with Apple)

**OAuth2 Limitations**:
- ❌ Requires user interaction (not ideal for CI/CD)
- ❌ Token expiration requires refresh flow
- ❌ Browser-based flow needed for web testing

**mTLS Strengths**:
- ✅ No user interaction required (perfect for automation)
- ✅ Certificate-based trust model
- ✅ Industry standard for B2B integrations
- ✅ Long-lived credentials (certificate lifetime)

**mTLS Limitations**:
- ❌ Certificate management overhead
- ❌ Not user-friendly (requires certificate installation)
- ❌ Revocation checking complexity

**Decision**: Use BOTH methods where each excels:
- **OAuth for humans** (end users, developer testing)
- **mTLS for machines** (CI/CD, integrations, automation)

---

### Multi-Scheme Authorization

API controllers will accept BOTH authentication methods:

```csharp
[Authorize(AuthenticationSchemes = "Bearer,Certificate")]
[ApiController]
[Route("api/medications")]
public class MedicationController : ControllerBase
{
    // Works with:
    // 1. Authorization: Bearer {jwt_token} (OAuth users)
    // 2. Client certificate in TLS handshake (mTLS partners)
}
```

Authorization middleware checks:
1. **JWT token present?** → Validate with JWT bearer authentication
2. **Client certificate present?** → Validate with certificate authentication
3. **Neither present?** → Return 401 Unauthorized

This allows seamless coexistence of both methods without code duplication.

---

## Security Considerations

### OAuth2 Security (T015)
- ✅ State parameter prevents CSRF attacks
- ✅ Authorization code flow (not implicit - more secure)
- ✅ ID token signature validation (JWKS)
- ✅ Refresh token rotation (old token invalidated)
- ✅ Tokens hashed in database (SHA-256)
- ✅ 15-minute access token lifetime
- ✅ 7-day refresh token lifetime

### mTLS Security (T046)
- ✅ Certificate expiration validation
- ✅ Trusted CA chain verification
- ✅ Subject/CN matching against whitelist
- ✅ OCSP revocation checking
- ✅ Failed attempt audit logging
- ✅ Certificate rotation support
- ✅ Granular permission model

### Medical Data Protection
- All health data encrypted at rest (AES-256)
- All network traffic encrypted (TLS 1.2+)
- OAuth tokens grant read/write to user's own data only
- mTLS certificates grant limited read-only permissions
- Audit trail for all authentication attempts

---

## Testing Strategy

### Manual Testing (Developers)
**Method**: OAuth2 Redirect (Swagger UI)  
**Effort**: Click "Authorize" button, authenticate, test endpoints  
**Best For**: API exploration, endpoint validation, ad-hoc testing

### Automated Testing (CI/CD)
**Method**: mTLS Certificate  
**Effort**: Generate cert, register, run tests, revoke  
**Best For**: Integration tests, regression testing, performance testing

### Mobile Testing (QA)
**Method**: OAuth2 ID Token Exchange  
**Effort**: Use OAuth Playground to get ID token, POST to /mobile endpoint  
**Best For**: Mobile app validation, platform-specific flows

---

## Migration Path

### Current State (Before T015)
```
✅ Mobile: OAuth ID token exchange working
❌ Web: Fake password authentication (placeholder)
❌ Swagger: No authentication configured
❌ CI/CD: No automated testing possible
```

### After T015 (OAuth Redirect)
```
✅ Mobile: OAuth ID token exchange working
✅ Web: OAuth redirect flow working
✅ Swagger: OAuth "Authorize" button working
⏳ CI/CD: Still requires manual OAuth (not ideal)
```

### After T046 (mTLS)
```
✅ Mobile: OAuth ID token exchange working
✅ Web: OAuth redirect flow working
✅ Swagger: OAuth "Authorize" button working
✅ CI/CD: mTLS certificate authentication (fully automated)
```

---

## Configuration Management

### Development (appsettings.Development.json)
```json
{
  "Authentication": {
    "AzureAd": {
      "TenantId": "common",
      "ClientId": "dev-client-id",
      "ClientSecret": "dev-secret"
    },
    "Google": {
      "ClientId": "dev.apps.googleusercontent.com",
      "ClientSecret": "dev-secret"
    }
  },
  "CertificateAuthentication": {
    "Enabled": true,
    "AllowedCertificateTypes": "SelfSigned"  // Dev accepts self-signed
  }
}
```

### Production (Azure Key Vault)
```json
{
  "Authentication": {
    "AzureAd": {
      "TenantId": "@Microsoft.KeyVault(SecretUri=...)",
      "ClientId": "@Microsoft.KeyVault(SecretUri=...)",
      "ClientSecret": "@Microsoft.KeyVault(SecretUri=...)"
    }
  },
  "CertificateAuthentication": {
    "Enabled": true,
    "AllowedCertificateTypes": "Chained",  // Prod requires CA-signed
    "RevocationMode": "Online"  // OCSP checking enabled
  }
}
```

---

## Success Metrics

### T015 Success (OAuth Redirect)
- [ ] Swagger UI OAuth flow: 100% success rate
- [ ] Web UI authentication: <3 seconds from click to logged in
- [ ] Zero password-based authentication attempts
- [ ] Developer satisfaction: "Testing API is easy"

### T046 Success (mTLS)
- [ ] CI/CD integration tests: 100% automated (no manual steps)
- [ ] Certificate validation: <100ms latency overhead
- [ ] Zero failed legitimate mTLS attempts
- [ ] Security audit: All attempts logged with timestamps

### Overall Authentication Health
- [ ] 99.9% authentication success rate
- [ ] <500ms average authentication latency
- [ ] Zero security incidents (failed CSRF, token theft, cert misuse)
- [ ] 100% test coverage for all three authentication methods

---

## Next Steps

### Immediate (This Week)
1. **Implement T015b-h** (OAuth redirect endpoints)
   - Priority: HIGH
   - Effort: ~4-5 hours
   - Deliverable: Swagger OAuth working

2. **Test Swagger OAuth flow**
   - Validate Azure AD authentication
   - Validate Google authentication
   - Document any issues

3. **Update Web UI** (T018c)
   - Remove client-side OAuth libraries
   - Delegate to API OAuth endpoints
   - Test end-to-end web authentication

### Short-Term (Next Sprint)
1. **Implement T046a-l** (mTLS certificate authentication)
   - Priority: MEDIUM
   - Effort: ~7-8 hours
   - Deliverable: CI/CD integration tests working

2. **Document testing workflows**
   - Update README with authentication examples
   - Add troubleshooting guide
   - Create video walkthrough for team

### Long-Term (Future Releases)
1. **Healthcare integrations**
   - Partner with EMR vendors
   - Implement HL7/FHIR endpoints
   - Use mTLS for secure hospital connections

2. **Advanced features**
   - Multi-factor authentication (MFA)
   - Biometric authentication (mobile)
   - Certificate auto-rotation

---

## Conclusion

The **Hybrid OAuth2 + mTLS** authentication strategy provides comprehensive coverage for all use cases:

- ✅ **End Users**: Secure, user-friendly OAuth2
- ✅ **Developers**: Easy testing via Swagger OAuth
- ✅ **QA/CI-CD**: Fully automated testing with mTLS
- ✅ **Integrations**: Enterprise-ready certificate authentication

**Current Progress**: Phase 1 complete (Mobile OAuth) ✅  
**Next Phase**: T015 OAuth redirect (~4-5 hours) ⏳  
**Future Phase**: T046 mTLS (~7-8 hours) 🔮

All requirements validated in:
- ✅ FR-001: OAuth2 authentication requirement
- ✅ FR-022: Multi-method authentication (OAuth + mTLS)
- ✅ CHK095a-CHK095g: Requirements checklist validation

**Ready to proceed with T015 implementation!** 🚀
