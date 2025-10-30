# Polly Resilience Policies with .NET Aspire

## Overview
Implement Polly-based HTTP resilience policies using .NET Aspire's `ServiceDefaults` to handle transient failures in OAuth token exchange and API calls.

## Priority
**Medium** - Implement during Aspire integration phase

## Benefits
- ✅ Automatic retry for transient failures (timeouts, 5xx errors, network issues)
- ✅ Exponential backoff with jitter to prevent thundering herd
- ✅ Circuit breaker to prevent cascading failures
- ✅ Per-request timeout configuration
- ✅ Integrated telemetry in Aspire dashboard
- ✅ Reduces user-visible errors from transient API issues

## Current State

### What Works Now
```csharp
// Program.cs - Basic HttpClient configuration
builder.Services.AddHttpClient<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    
    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7234")
    };
    
    return httpClient;
});
```

### Current Error Handling
- **OAuth failures**: `OnRemoteFailure` event handler redirects to login with error message
- **API errors**: Try/catch blocks in Razor components show Snackbar errors
- **User experience**: Friendly error messages, but no automatic retry

## Proposed Implementation

### 1. Update ServiceDefaults with Polly

**File**: `src/BloodThinnerTracker.ServiceDefaults/ServiceDefaults.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace BloodThinnerTracker.ServiceDefaults;

public static class ServiceDefaults
{
    public static IServiceCollection AddServiceDefaults(this IServiceCollection services)
    {
        // Add standard resilience handlers for all HTTP clients
        services.ConfigureHttpClientDefaults(http =>
        {
            // Standard resilience pipeline (Polly)
            http.AddStandardResilienceHandler(options =>
            {
                // Retry Strategy
                options.Retry = new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => 
                            response.StatusCode >= System.Net.HttpStatusCode.InternalServerError ||
                            response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                };

                // Circuit Breaker Strategy
                options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,              // Open circuit at 50% failure rate
                    SamplingDuration = TimeSpan.FromSeconds(10),
                    MinimumThroughput = 5,           // Need at least 5 requests
                    BreakDuration = TimeSpan.FromSeconds(30) // Stay open 30 seconds
                };

                // Timeout Strategy (per attempt)
                options.AttemptTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                // Overall Timeout (all attempts)
                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
            });

            // Add standard hedging for improved latency (optional)
            // http.AddStandardHedgingHandler();
        });

        return services;
    }
}
```

### 2. Update Program.cs to Use ServiceDefaults

**File**: `src/BloodThinnerTracker.Web/Program.cs`

```csharp
// Add Aspire ServiceDefaults
builder.Services.AddServiceDefaults();

// Named HttpClient with resilience
builder.Services.AddHttpClient("BloodThinnerApi", (sp, client) =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7234");
})
.AddHttpMessageHandler<AuthorizationMessageHandler>();

// Register scoped HttpClient for dependency injection
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("BloodThinnerApi");
});
```

### 3. Update AppHost for Resilience Configuration

**File**: `src/BloodThinnerTracker.AppHost/Program.cs`

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// API with resilience configuration
var api = builder.AddProject<Projects.BloodThinnerTracker_Api>("api")
    .WithExternalHttpEndpoints();

// Web with reference to API
var web = builder.AddProject<Projects.BloodThinnerTracker_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### 4. Advanced: Custom Resilience for OAuth

**File**: `src/BloodThinnerTracker.Web/Components/Pages/OAuthCallback.razor`

```csharp
// Custom resilience pipeline for token exchange
private static readonly ResiliencePipeline<HttpResponseMessage> _tokenExchangePipeline = 
    new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutException>()
                .HandleResult(r => !r.IsSuccessStatusCode)
        })
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

// Use in token exchange
private async Task<TokenResponse> ExchangeTokenWithRetry(string idToken)
{
    var response = await _tokenExchangePipeline.ExecuteAsync(async ct =>
    {
        return await Http.PostAsJsonAsync("api/auth/exchange", new { idToken }, ct);
    });

    return await response.Content.ReadFromJsonAsync<TokenResponse>();
}
```

## Configuration

### appsettings.json
```json
{
  "Resilience": {
    "Http": {
      "Retry": {
        "MaxRetryAttempts": 3,
        "BaseDelay": "00:00:01",
        "MaxDelay": "00:00:10"
      },
      "CircuitBreaker": {
        "FailureThreshold": 0.5,
        "SamplingDuration": "00:00:10",
        "MinimumThroughput": 5,
        "BreakDuration": "00:00:30"
      },
      "Timeout": {
        "PerAttempt": "00:00:10",
        "Total": "00:00:30"
      }
    }
  }
}
```

## Telemetry Integration

### Aspire Dashboard Metrics
- **Retry attempts**: How often retries occur
- **Circuit breaker state**: Open/Closed/Half-Open
- **Request duration**: Including retry overhead
- **Failure rates**: By endpoint and status code
- **Timeout frequency**: Per-attempt vs total

### Access Dashboard
```bash
# Start with Aspire AppHost
dotnet run --project src/BloodThinnerTracker.AppHost

# Dashboard available at: http://localhost:15888
```

## Testing Resilience

### 1. Simulate Transient Failures
```csharp
// Test controller for fault injection
[ApiController]
[Route("api/test")]
public class FaultController : ControllerBase
{
    private static int _callCount = 0;

    [HttpGet("intermittent")]
    public IActionResult GetIntermittent()
    {
        _callCount++;
        
        // Fail first 2 attempts, succeed on 3rd
        if (_callCount % 3 != 0)
        {
            return StatusCode(503, "Service temporarily unavailable");
        }
        
        return Ok(new { message = "Success after retry" });
    }

    [HttpGet("timeout")]
    public async Task<IActionResult> GetTimeout()
    {
        // Simulate slow response
        await Task.Delay(TimeSpan.FromSeconds(15));
        return Ok(new { message = "Delayed response" });
    }
}
```

### 2. Verify Retry Behavior
```bash
# Watch logs for retry attempts
dotnet run --project src/BloodThinnerTracker.AppHost

# Call API that fails intermittently
curl http://localhost:5173/api/test/intermittent

# Expected: 3 attempts, final success
# Log output: 
#   Attempt 1: 503 Service Unavailable (retry)
#   Attempt 2: 503 Service Unavailable (retry)
#   Attempt 3: 200 OK (success)
```

### 3. Circuit Breaker Test
```bash
# Flood API with failing requests
for i in {1..10}; do
  curl http://localhost:5173/api/medications
done

# Expected: Circuit opens after 50% failure rate
# Subsequent requests fail fast without hitting API
```

## Performance Impact

### Without Polly
- **Transient failure**: Immediate error to user
- **User retry**: Manual refresh required
- **Cascading failures**: No protection

### With Polly
- **Transient failure**: Automatic retry (transparent to user)
- **Success rate**: ~95% → 99.5% with 3 retries
- **Circuit breaker**: Prevents cascading failures
- **Slight latency**: +1-3 seconds on failed attempts (but succeeds vs failing)

## Migration Path

### Phase 1: Enable Default Resilience (Easy)
1. Add `ServiceDefaults.AddServiceDefaults()`
2. Test existing API calls work
3. Monitor Aspire dashboard for metrics

### Phase 2: Tune Configuration (Medium)
1. Analyze failure patterns in dashboard
2. Adjust retry counts, delays, circuit breaker thresholds
3. Add custom policies for specific endpoints

### Phase 3: Advanced Scenarios (Optional)
1. Custom pipelines for OAuth token exchange
2. Hedging for read operations
3. Rate limiting with Polly
4. Bulkhead isolation for critical paths

## NuGet Packages Required

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.0.0" />
<PackageReference Include="Polly" Version="8.5.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
```

**Note**: Aspire ServiceDefaults automatically includes these dependencies.

## Related Issues

This enhancement addresses:
- **Current**: GatewayTimeout from Microsoft OAuth API
- **Current**: Network errors during API calls show errors to user
- **Future**: Improved reliability during high load or service degradation

## Success Criteria

✅ **Automatic retry**: Transient failures recover without user intervention  
✅ **Circuit breaker**: API overload doesn't cascade to web tier  
✅ **Telemetry**: Aspire dashboard shows resilience metrics  
✅ **Configuration**: Resilience tunable per environment (dev/staging/prod)  
✅ **Testing**: Fault injection tests pass with expected retry behavior  

## References

- [.NET Aspire Resilience](https://learn.microsoft.com/en-us/dotnet/aspire/resilience/overview)
- [Polly Documentation](https://www.pollydocs.org/)
- [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Circuit Breaker Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)

## Related Files
- `src/BloodThinnerTracker.ServiceDefaults/ServiceDefaults.cs` - Resilience configuration
- `src/BloodThinnerTracker.Web/Program.cs` - HttpClient registration
- `src/BloodThinnerTracker.AppHost/Program.cs` - Aspire orchestration
- `docs/fixes/oauth-error-handling.md` - Current error handling approach
