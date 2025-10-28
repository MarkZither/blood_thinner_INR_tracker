# OAuth2 Flow - Quick Reference

## 🌐 Web/Blazor Flow

### Step 1: User Clicks "Sign in with Google"
```html
<button onclick="window.location='/api/auth/external/google'">
    Sign in with Google
</button>
```

### Step 2: API Redirects to Google
```csharp
[HttpGet("external/{provider}")]
public IActionResult ExternalLogin(string provider)
{
    var redirectUrl = Url.Action("ExternalCallback", "Auth", new { provider });
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    
    return Challenge(properties, provider); // Redirects to Google
}
```

User sees Google login page: `https://accounts.google.com/o/oauth2/v2/auth?client_id=...`

### Step 3: Google Redirects Back
After user logs in, Google redirects to:
```
GET /api/auth/callback/google?code=ABC123&state=xyz
```

### Step 4: API Exchanges Code for User Info
```csharp
[HttpGet("callback/{provider}")]
public async Task<ActionResult> ExternalCallback(string provider, string code)
{
    // 1. Exchange code for user info
    var result = await HttpContext.AuthenticateAsync(provider);
    var email = result.Principal.FindFirst(ClaimTypes.Email).Value;
    var name = result.Principal.FindFirst(ClaimTypes.Name).Value;
    var externalId = result.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
    
    // 2. Find or create user
    var user = await _authService.AuthenticateExternalAsync(
        provider, externalId, email, name);
    
    // 3. Return JWT tokens
    return Redirect($"/login-success?token={user.AccessToken}");
}
```

### Step 5: Web App Stores JWT
```javascript
// On /login-success page
const params = new URLSearchParams(window.location.search);
const token = params.get('token');
localStorage.setItem('jwt', token);
window.location = '/dashboard';
```

---

## 📱 Mobile/MAUI Flow

### Step 1: User Clicks "Sign in with Google"
```csharp
private async Task GoogleSignInAsync()
{
    try
    {
        // MAUI uses platform-native OAuth2
        var authResult = await WebAuthenticator.AuthenticateAsync(
            new Uri("https://accounts.google.com/o/oauth2/v2/auth?" +
                   $"client_id={GoogleClientId}&" +
                   "response_type=id_token&" +
                   "scope=openid profile email&" +
                   $"redirect_uri={RedirectUri}&" +
                   "nonce=random123"),
            new Uri(RedirectUri));
        
        // Extract ID token
        var idToken = authResult.IdToken;
        
        // Send to our API
        await ExchangeIdTokenForJwt(idToken);
    }
    catch (TaskCanceledException)
    {
        // User cancelled
    }
}
```

### Step 2: API Validates ID Token
```csharp
[HttpPost("external/mobile")]
public async Task<ActionResult<AuthenticationResponse>> ExternalLoginMobile(
    [FromBody] ExternalLoginRequest request)
{
    // 1. Validate ID token with Google
    var payload = await GoogleJsonWebSignature.ValidateAsync(
        request.IdToken, 
        new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _config.Google.ClientId }
        });
    
    // 2. Extract user info
    var email = payload.Email;
    var name = payload.Name;
    var externalId = payload.Subject;
    
    // 3. Find or create user
    var user = await _authService.AuthenticateExternalAsync(
        "Google", externalId, email, name);
    
    // 4. Return JWT tokens
    return Ok(user);
}
```

### Step 3: Mobile App Stores JWT
```csharp
private async Task ExchangeIdTokenForJwt(string idToken)
{
    var request = new ExternalLoginRequest
    {
        Provider = "Google",
        IdToken = idToken,
        DeviceId = DeviceInfo.Current.Id
    };
    
    var response = await _httpClient.PostAsJsonAsync(
        "/api/auth/external/mobile", request);
    
    var authResult = await response.Content
        .ReadFromJsonAsync<AuthenticationResponse>();
    
    // Store JWT in secure storage
    await SecureStorage.SetAsync("jwt_access", authResult.AccessToken);
    await SecureStorage.SetAsync("jwt_refresh", authResult.RefreshToken);
    
    // Navigate to dashboard
    await Shell.Current.GoToAsync("//dashboard");
}
```

---

## 🔑 Key Models

### ExternalLoginRequest (Replaces LoginRequest)
```csharp
public class ExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // "Google" | "AzureAD"
    
    [Required]
    public string IdToken { get; set; } = string.Empty; // For mobile
    
    public string? AuthorizationCode { get; set; }      // For web
    public string? RedirectUri { get; set; }
    public string? DeviceId { get; set; }
}
```

**NO PASSWORD FIELD!**

### AuthenticationResponse (Unchanged)
```csharp
public class AuthenticationResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserInfo User { get; set; }
    public List<string> Permissions { get; set; }
}
```

### User Entity (Updated)
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    
    // OAuth2 fields
    public string Provider { get; set; }           // "Google" | "AzureAD"
    public string ExternalUserId { get; set; }     // Google/Microsoft user ID
    
    // NO PASSWORD HASH FIELD!
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
```

---

## ❌ What NOT to Do

### DON'T: Accept Passwords
```csharp
// ❌ WRONG - Don't do this
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // Accepting username/password is NOT OAuth2!
}
```

### DON'T: Return JWT Without Validation
```csharp
// ❌ WRONG - Don't do this  
var user = new UserInfo
{
    Email = request.Email,  // Just trusting the input!
    Name = "Random Name"
};
return GenerateJWT(user);  // Issued without any validation!
```

### DON'T: Store Passwords
```csharp
// ❌ WRONG - OAuth2 doesn't use passwords
public class User
{
    public string PasswordHash { get; set; }  // Delete this!
}
```

---

## ✅ What TO Do

### DO: Validate ID Tokens
```csharp
// ✅ CORRECT
var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
// Now we KNOW this user is authenticated by Google
```

### DO: Check Provider User ID
```csharp
// ✅ CORRECT
var user = await _context.Users
    .FirstOrDefaultAsync(u => 
        u.Provider == "Google" && 
        u.ExternalUserId == payload.Subject);

if (user == null)
{
    // First login - create user
    user = new User
    {
        Provider = "Google",
        ExternalUserId = payload.Subject,
        Email = payload.Email,
        Name = payload.Name
    };
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
}
```

### DO: Use Platform-Native OAuth
```csharp
// ✅ CORRECT - MAUI
var result = await WebAuthenticator.AuthenticateAsync(...);

// ✅ CORRECT - Blazor
return Challenge(properties, "Google");
```

---

## 🧪 Testing OAuth2

### Unit Test: Token Validation
```csharp
[Fact]
public async Task AuthenticateExternal_ValidGoogleToken_ReturnsUser()
{
    // Arrange
    var mockIdToken = CreateMockGoogleIdToken();
    
    // Act
    var result = await _authService.AuthenticateExternalAsync(
        "Google", mockIdToken);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Google", result.User.Provider);
}
```

### Integration Test: OAuth Flow
```csharp
[Fact]
public async Task OAuthCallback_ValidCode_ReturnsJWT()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync(
        "/api/auth/callback/google?code=test_code");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content
        .ReadFromJsonAsync<AuthenticationResponse>();
    Assert.NotNull(result.AccessToken);
}
```

---

## 📚 Resources

- [Google OAuth2](https://developers.google.com/identity/protocols/oauth2)
- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [ASP.NET Core External Auth](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)
- [MAUI WebAuthenticator](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/communication/authentication)
