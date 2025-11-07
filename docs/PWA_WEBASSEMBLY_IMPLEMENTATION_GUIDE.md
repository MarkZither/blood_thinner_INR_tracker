# PWA and WebAssembly Implementation Guide

## Overview

This guide provides step-by-step instructions for adding Progressive Web App (PWA) and WebAssembly support to the BloodThinnerTracker Blazor Web project.

**Target Architecture**: Blazor Web with WebAssembly Client Project  
**Framework**: .NET 10 (C# 13)  
**UI Library**: MudBlazor 8.13.0  
**Estimated Time**: 5-6 days

---

## Prerequisites

- .NET 10 SDK (10.0.x)
- Visual Studio 2025 or VS Code with C# Dev Kit
- Understanding of Blazor render modes
- Familiarity with service workers and PWA concepts

---

## Phase 1: Create WebAssembly Client Project

### Step 1.1: Create Client Project

```bash
cd src
dotnet new blazorwasm -n BloodThinnerTracker.Web.Client -f net10.0
```

### Step 1.2: Update Project File

**File**: `src/BloodThinnerTracker.Web.Client/BloodThinnerTracker.Web.Client.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0-rc.2.25502.107" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.0-rc.2.25502.107" PrivateAssets="all" />
    <PackageReference Include="MudBlazor" Version="8.13.0" />
    <PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BloodThinnerTracker.Shared\BloodThinnerTracker.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />
  </ItemGroup>
</Project>
```

### Step 1.3: Add Client Project to Solution

```bash
cd ../..
dotnet sln add src/BloodThinnerTracker.Web.Client/BloodThinnerTracker.Web.Client.csproj
```

### Step 1.4: Update Server Project Reference

**File**: `src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\BloodThinnerTracker.Web.Client\BloodThinnerTracker.Web.Client.csproj" />
  <ProjectReference Include="..\BloodThinnerTracker.ServiceDefaults\BloodThinnerTracker.ServiceDefaults.csproj" />
  <ProjectReference Include="..\BloodThinnerTracker.Shared\BloodThinnerTracker.Shared.csproj" />
</ItemGroup>
```

### Step 1.5: Update Server Program.cs

**File**: `src/BloodThinnerTracker.Web/Program.cs`

Add WebAssembly render mode support:

```csharp
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents(); // ← ADD THIS

// ... existing code ...

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode() // ← ADD THIS
    .AddAdditionalAssemblies(typeof(BloodThinnerTracker.Web.Client._Imports).Assembly); // ← ADD THIS
```

### Step 1.6: Create Client Program.cs

**File**: `src/BloodThinnerTracker.Web.Client/Program.cs`

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add local storage for client-side data persistence
builder.Services.AddBlazoredLocalStorage();

// Add HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

await builder.Build().RunAsync();
```

---

## Phase 2: Move Components to Client Project

### Step 2.1: Create Client Component Structure

Create the following folders in `BloodThinnerTracker.Web.Client`:

```
Components/
├── Layout/
├── Pages/
└── Medications/
```

### Step 2.2: Move Interactive Components

Components to move from Server to Client:

**Pages**:
- `Medications.razor`
- `MedicationAdd.razor`
- `MedicationEdit.razor`
- `MedicationHistory.razor`
- `PatternHistory.razor`
- `INRAdd.razor`
- `INREdit.razor`
- `Dashboard.razor`

**Medications**:
- `PatternEntryComponent.razor`
- `PatternDisplayComponent.razor`
- `VarianceIndicator.razor`

### Step 2.3: Update Render Mode Directives

Add to each moved component:

```razor
@rendermode InteractiveWebAssembly

@* Component code *@
```

**Example**: `Components/Pages/Medications.razor`

```razor
@page "/medications"
@rendermode InteractiveWebAssembly
@attribute [Authorize]
@inject IMedicationService MedicationService
@inject IDialogService DialogService

<PageTitle>Medications</PageTitle>

@* Existing component code *@
```

### Step 2.4: Create _Imports.razor in Client

**File**: `src/BloodThinnerTracker.Web.Client/Components/_Imports.razor`

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.JSInterop
@using MudBlazor
@using BloodThinnerTracker.Shared.Models
@using BloodThinnerTracker.Web.Client
@using BloodThinnerTracker.Web.Client.Components
@using BloodThinnerTracker.Web.Client.Services
```

---

## Phase 3: Implement Client-Side Authentication

### Step 3.1: Install Required Packages

```bash
cd src/BloodThinnerTracker.Web.Client
dotnet add package Blazored.LocalStorage
dotnet add package Microsoft.AspNetCore.Components.Authorization
```

### Step 3.2: Create Client Authentication State Provider

**File**: `src/BloodThinnerTracker.Web.Client/Services/ClientAuthenticationStateProvider.cs`

```csharp
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BloodThinnerTracker.Web.Client.Services;

/// <summary>
/// Client-side authentication state provider for WebAssembly.
/// Manages JWT token storage and authentication state using browser localStorage.
/// </summary>
public class ClientAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientAuthenticationStateProvider> _logger;

    private const string AuthTokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string TokenExpiryKey = "tokenExpiry";

    public ClientAuthenticationStateProvider(
        ILocalStorageService localStorage,
        HttpClient httpClient,
        ILogger<ClientAuthenticationStateProvider> logger)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // SECURITY NOTE: Token should be validated server-side before storage.
            // Client-side validation is for UX only (checking expiry).
            // Signature verification happens server-side during API calls.
            
            // Check if token is expired (client-side expiry check)
            var expiry = await _localStorage.GetItemAsync<DateTime?>(TokenExpiryKey);
            if (expiry.HasValue && expiry.Value < DateTime.UtcNow)
            {
                _logger.LogInformation("Token expired, attempting refresh");
                var refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    await ClearAuthenticationAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                
                // Get refreshed token
                token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            }

            var claims = ParseClaimsFromJwt(token!);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    /// <summary>
    /// Authenticate user with JWT token.
    /// </summary>
    public async Task AuthenticateAsync(string token, string? refreshToken = null, DateTime? expiry = null)
    {
        await _localStorage.SetItemAsync(AuthTokenKey, token);
        
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _localStorage.SetItemAsync(RefreshTokenKey, refreshToken);
        }

        if (expiry.HasValue)
        {
            await _localStorage.SetItemAsync(TokenExpiryKey, expiry.Value);
        }
        else
        {
            // Default to 1 hour expiry if not specified
            await _localStorage.SetItemAsync(TokenExpiryKey, DateTime.UtcNow.AddHours(1));
        }

        var authState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    /// <summary>
    /// Clear authentication state (logout).
    /// </summary>
    public async Task ClearAuthenticationAsync()
    {
        await _localStorage.RemoveItemAsync(AuthTokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        await _localStorage.RemoveItemAsync(TokenExpiryKey);

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    private async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
            
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result == null)
            {
                return false;
            }

            await AuthenticateAsync(result.AccessToken, result.RefreshToken, result.ExpiresAt);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }

    /// <summary>
    /// Parse claims from JWT token.
    /// SECURITY NOTE: This is a simplified example for demonstration.
    /// In production, use System.IdentityModel.Tokens.Jwt for proper validation.
    /// This implementation only decodes the payload without signature verification.
    /// The token should be validated server-side before being stored client-side.
    /// </summary>
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        
        // SECURITY: In production, validate the token signature here
        // using JwtSecurityTokenHandler and TokenValidationParameters
        
        var payload = jwt.Split('.')[1];
        
        // Add padding if necessary
        var base64 = payload.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        var jsonBytes = Convert.FromBase64String(base64);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new Claim(kvp.Key, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(kvp.Key, element.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));
                }
            }
        }

        return claims;
    }

    private record TokenResponse(string AccessToken, string? RefreshToken, DateTime ExpiresAt);
}
```

### Step 3.3: Register Authentication in Client

**File**: `src/BloodThinnerTracker.Web.Client/Program.cs`

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Blazored.LocalStorage;
using BloodThinnerTracker.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add local storage
builder.Services.AddBlazoredLocalStorage();

// Add authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ClientAuthenticationStateProvider>();
builder.Services.AddScoped<ClientAuthenticationStateProvider>();

// Add HttpClient with authorization
builder.Services.AddScoped(sp => 
{
    var client = new HttpClient 
    { 
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) 
    };
    return client;
});

await builder.Build().RunAsync();
```

### Step 3.4: Create Authorization Message Handler

**File**: `src/BloodThinnerTracker.Web.Client/Services/ClientAuthorizationMessageHandler.cs`

```csharp
using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace BloodThinnerTracker.Web.Client.Services;

/// <summary>
/// Message handler that adds JWT token to outgoing HTTP requests.
/// </summary>
public class ClientAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ClientAuthorizationMessageHandler> _logger;

    public ClientAuthorizationMessageHandler(
        ILocalStorageService localStorage,
        ILogger<ClientAuthorizationMessageHandler> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding authorization header");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

---

## Phase 4: Add PWA Support

### Step 4.1: Create Web App Manifest

**File**: `src/BloodThinnerTracker.Web.Client/wwwroot/manifest.webmanifest`

```json
{
  "name": "Blood Thinner & INR Tracker",
  "short_name": "INR Tracker",
  "description": "Track blood thinner medication and INR test results. Manage dosage patterns and monitor treatment effectiveness.",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#594AE2",
  "orientation": "portrait-primary",
  "scope": "/",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any"
    },
    {
      "src": "icon-192-maskable.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any"
    },
    {
      "src": "icon-512-maskable.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "maskable"
    }
  ],
  "categories": ["health", "medical", "lifestyle"],
  "shortcuts": [
    {
      "name": "Log Medication",
      "short_name": "Log Med",
      "description": "Quickly log a medication dose",
      "url": "/medication-log",
      "icons": [{ "src": "icon-192.png", "sizes": "192x192" }]
    },
    {
      "name": "Add INR Test",
      "short_name": "INR Test",
      "description": "Record a new INR test result",
      "url": "/inr/add",
      "icons": [{ "src": "icon-192.png", "sizes": "192x192" }]
    }
  ]
}
```

### Step 4.2: Create Service Worker (Development)

**File**: `src/BloodThinnerTracker.Web.Client/wwwroot/service-worker.js`

```javascript
// Development service worker
// Provides minimal offline support for development

const CACHE_NAME = 'blazor-cache-v1';

self.addEventListener('install', event => {
    console.log('[Service Worker] Installing...');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('[Service Worker] Activating...');
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', event => {
    // Let all requests pass through in development
    event.respondWith(fetch(event.request));
});
```

### Step 4.3: Create Service Worker (Production)

**File**: `src/BloodThinnerTracker.Web.Client/wwwroot/service-worker.published.js`

```javascript
// Production service worker with offline caching

const CACHE_NAME = 'blazor-offline-cache-v1';
const DATA_CACHE_NAME = 'blazor-data-cache-v1';

// Resources to cache on install
// NOTE: In production, use Blazor's ServiceWorkerAssetsManifest for automatic
// asset discovery. This hardcoded list is for demonstration purposes.
// The build process generates 'service-worker-assets.js' with all assets.
const PRECACHE_URLS = [
    '/',
    '/index.html',
    '/css/app.css',
    '/manifest.webmanifest',
    '/icon-192.png',
    '/icon-512.png'
];

// Install event - pre-cache essential resources
self.addEventListener('install', event => {
    console.log('[Service Worker] Installing and caching resources');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Pre-caching resources');
                return cache.addAll(PRECACHE_URLS);
            })
            .then(() => self.skipWaiting())
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    console.log('[Service Worker] Activating');
    
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(cacheName => 
                        cacheName !== CACHE_NAME && 
                        cacheName !== DATA_CACHE_NAME
                    )
                    .map(cacheName => {
                        console.log('[Service Worker] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch event - implement caching strategies
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    
    // Skip caching for authentication endpoints
    if (url.pathname.startsWith('/api/auth')) {
        event.respondWith(fetch(event.request));
        return;
    }
    
    // Network-first strategy for API calls
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(networkFirstStrategy(event.request));
        return;
    }
    
    // Cache-first strategy for static resources
    event.respondWith(cacheFirstStrategy(event.request));
});

// Cache-first strategy: Check cache, fallback to network
async function cacheFirstStrategy(request) {
    const cache = await caches.open(CACHE_NAME);
    const cachedResponse = await cache.match(request);
    
    if (cachedResponse) {
        console.log('[Service Worker] Cache hit:', request.url);
        return cachedResponse;
    }
    
    console.log('[Service Worker] Cache miss, fetching:', request.url);
    const response = await fetch(request);
    
    // Cache successful responses
    if (response.status === 200) {
        cache.put(request, response.clone());
    }
    
    return response;
}

// Network-first strategy: Try network, fallback to cache
async function networkFirstStrategy(request) {
    const cache = await caches.open(DATA_CACHE_NAME);
    
    try {
        const response = await fetch(request);
        
        // Cache successful API responses
        if (response.status === 200) {
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.log('[Service Worker] Network failed, trying cache:', request.url);
        const cachedResponse = await cache.match(request);
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // Return offline page for failed requests
        return new Response(
            JSON.stringify({ error: 'Offline', message: 'Network unavailable' }),
            { status: 503, headers: { 'Content-Type': 'application/json' } }
        );
    }
}

// Background sync for queued requests
self.addEventListener('sync', event => {
    if (event.tag === 'sync-medication-logs') {
        event.waitUntil(syncMedicationLogs());
    }
});

async function syncMedicationLogs() {
    // Implement background sync logic for queued medication logs
    console.log('[Service Worker] Syncing queued medication logs');
    
    // TODO: Retrieve queued logs from IndexedDB
    // TODO: Send to API when online
    // TODO: Clear queue on success
}
```

### Step 4.4: Update App.razor for PWA

**File**: `src/BloodThinnerTracker.Web/Components/App.razor`

```html
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    
    <!-- PWA Manifest -->
    <link rel="manifest" href="manifest.webmanifest" />
    
    <!-- Icons -->
    <link rel="icon" type="image/png" href="icon-192.png" />
    <link rel="apple-touch-icon" sizes="192x192" href="icon-192.png" />
    <link rel="apple-touch-icon" sizes="512x512" href="icon-512.png" />
    
    <!-- Theme color -->
    <meta name="theme-color" content="#594AE2" />
    
    <!-- iOS specific meta tags -->
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="default" />
    <meta name="apple-mobile-web-app-title" content="INR Tracker" />
    
    <ResourcePreloader />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <link rel="stylesheet" href="@Assets["BloodThinnerTracker.Web.styles.css"]" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <ImportMap />
    <HeadOutlet />
</head>

<body>
    <CascadingAuthenticationState>
        <Routes @rendermode="InteractiveServer" />
    </CascadingAuthenticationState>
    <ReconnectModal />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    
    <!-- Service Worker Registration -->
    <script>
        if ('serviceWorker' in navigator) {
            window.addEventListener('load', () => {
                navigator.serviceWorker.register('/service-worker.js')
                    .then(reg => console.log('[PWA] Service worker registered:', reg))
                    .catch(err => console.error('[PWA] Service worker registration failed:', err));
            });
        }
    </script>
</body>

</html>
```

### Step 4.5: Generate App Icons

Use a tool like [PWA Asset Generator](https://github.com/onderceylan/pwa-asset-generator) or create icons manually:

**Required Icons**:
- `icon-192.png` (192x192px)
- `icon-192-maskable.png` (192x192px with safe zone)
- `icon-512.png` (512x512px)
- `icon-512-maskable.png` (512x512px with safe zone)

Place all icons in `src/BloodThinnerTracker.Web.Client/wwwroot/`

---

## Phase 5: Testing

### Test Checklist

**Installation**:
- [ ] Browser shows "Install App" prompt on desktop
- [ ] App can be installed from browser menu
- [ ] App launches in standalone mode (no browser UI)
- [ ] App icon appears correctly on home screen (mobile)

**Offline Functionality**:
- [ ] App loads when offline (cached shell)
- [ ] Previously viewed data available offline
- [ ] Appropriate offline message shown for new data
- [ ] Network requests queue when offline
- [ ] Queued requests sync when online

**Authentication**:
- [ ] Login works in WASM mode
- [ ] Token persists across browser sessions
- [ ] Token refresh works automatically
- [ ] Logout clears token from localStorage
- [ ] Expired tokens handled gracefully

**MudBlazor Components**:
- [ ] Dialogs open/close correctly
- [ ] Snackbars display properly
- [ ] Forms validate correctly
- [ ] Data tables render properly
- [ ] Theme applies correctly

**Service Worker**:
- [ ] Service worker registers successfully
- [ ] Cache updates on new deployment
- [ ] Old caches removed on activation
- [ ] Offline page shown when appropriate

---

## Phase 6: Deployment Configuration

### Update AppHost for WASM

**File**: `src/BloodThinnerTracker.AppHost/Program.cs`

```csharp
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api)
    .WithHttpsEndpoint(port: 5001, name: "https");

// Enable static web assets for WASM
web.WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);
```

### Azure App Service Configuration

For Azure deployment:

1. Enable static web assets compression:
```json
{
  "webOptimizer": {
    "enableCompression": true,
    "enableBrotli": true
  }
}
```

2. Add MIME types for WASM files in `web.config`:
```xml
<staticContent>
  <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
  <mimeMap fileExtension=".dll" mimeType="application/octet-stream" />
</staticContent>
```

---

## Troubleshooting

### Issue: Service Worker Not Registering

**Solution**: Check browser console for errors. Ensure HTTPS is enabled (service workers require secure context).

### Issue: Components Not Rendering

**Solution**: Verify render mode directive on component. Check that `AddInteractiveWebAssemblyRenderMode()` is called in Program.cs.

### Issue: Authentication Not Working

**Solution**: Check browser localStorage for token. Verify API base URL is correct. Check CORS configuration on API.

### Issue: Large Download Size

**Solution**: Enable AOT compilation and trimming:
```xml
<RunAOTCompilation>true</RunAOTCompilation>
<BlazorWebAssemblyPreserveCollationData>false</BlazorWebAssemblyPreserveCollationData>
```

---

## Performance Optimization

### Enable AOT Compilation

```xml
<PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

**Result**: 30-40% smaller download size, 3x faster execution

### Enable Trimming

```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>
```

**Result**: Remove unused code, reduce download size

### Configure Compression

**appsettings.Production.json**:
```json
{
  "ResponseCompression": {
    "EnableForHttps": true,
    "MimeTypes": ["application/wasm", "application/octet-stream"]
  }
}
```

---

## Security Considerations

### Content Security Policy

Add to `index.html` or middleware:

```html
<meta http-equiv="Content-Security-Policy" 
      content="default-src 'self'; 
               script-src 'self' 'unsafe-eval' 'unsafe-inline'; 
               style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; 
               font-src 'self' https://fonts.gstatic.com;">
```

### Token Security

- ✅ Store access tokens in localStorage (short-lived, 15-60 min)
- ✅ Store refresh tokens in HttpOnly cookies (long-lived)
- ✅ Implement automatic token refresh
- ✅ Clear tokens on logout
- ❌ Do not store sensitive medical data unencrypted

---

## Next Steps

After implementation:

1. **Monitor PWA metrics**: Install rate, offline usage, service worker errors
2. **User feedback**: Gather feedback on offline experience
3. **Performance monitoring**: Track download size, load time, cache hit rate
4. **Security audit**: Review token storage, CSP policy, CORS configuration
5. **Documentation update**: Update user guide with installation instructions

---

## References

- [Blazor WebAssembly Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly)
- [Progressive Web Apps on MDN](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps)
- [MudBlazor Documentation](https://www.mudblazor.com/)
- [Service Worker API](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)

---

**Document Version**: 1.0  
**Last Updated**: November 2025
