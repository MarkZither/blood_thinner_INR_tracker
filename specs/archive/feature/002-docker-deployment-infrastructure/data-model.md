# Data Model: Blood Thinner Medication & INR Tracker (Deployment Infrastructure)

**Source**: Adapted from archived canonical model (see specs/archive/feature/blood-thinner-medication-tracker/data-model.md)
**Scope**: Deployment infrastructure, configuration entities, health check endpoints

---

## Reference Entities

> For full entity definitions, see the canonical data model in the archive. This feature does not introduce new business entities but references:
- **User**
- **Medication**
- **MedicationSchedule**
- **MedicationLog**
- **INRTest**
- **INRSchedule**
- **UserDevice**
- **UserPreferences**
- **AuditLog**
- **SyncMetadata**

## Deployment-Specific Entities

### Configuration Entities

```csharp
public class OAuthConfig
{
    public string MicrosoftClientId { get; set; }
    public string MicrosoftTenantId { get; set; }
    public string GoogleClientId { get; set; }
    public string GoogleClientSecret { get; set; }
    public string RedirectUri { get; set; }
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } // PostgreSQL/SQLite
    public string KeyVaultUri { get; set; }
}
```

### Health Check Endpoint

```csharp
// API endpoint for health monitoring
[HttpGet("/health")]
public IActionResult Health()
{
    // Returns status, uptime, DB connectivity, etc.
}
```

## Containerized Database Notes
- PostgreSQL container for cloud
- SQLite for local/mobile
- Entity Framework Core multi-provider
- All migrations must be compatible with both providers

## Security & Compliance
- All configuration entities MUST use strongly-typed options pattern
- Secrets loaded from environment variables or Azure Key Vault
- No hardcoded secrets
- All health check endpoints must validate DB connectivity and encryption status

## References
- See canonical model for all business entities
- See plan.md for infrastructure details
