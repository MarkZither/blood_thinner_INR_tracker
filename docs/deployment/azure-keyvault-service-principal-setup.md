# Azure Key Vault Service Principal Setup Guide

This guide explains how to create an Azure AD App Registration (service principal), grant it access to Azure Key Vault, and obtain the three required environment variables for use with `DefaultAzureCredential`:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_CLIENT_SECRET`

## 1. Create an Azure AD App Registration

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Navigate to **Azure Active Directory** > **App registrations** > **New registration**.
3. Enter a name (e.g., `BloodThinnerTrackerService`), leave the default settings, and click **Register**.

## 2. Create a Client Secret

1. In your new app registration, go to **Certificates & secrets**.
2. Click **New client secret**.
3. Add a description (e.g., `Service Key`) and select an expiry (recommend: 1 or 2 years).
4. Click **Add**.
5. **Copy the value** of the client secret immediately. You will not be able to see it again!

## 3. Get the Required Values

- **Client ID**: Go to **Overview** and copy the **Application (client) ID**.
- **Tenant ID**: Go to **Overview** and copy the **Directory (tenant) ID**.
- **Client Secret**: Use the value you copied in step 2.5.

## 4. Grant Access to Key Vault

1. Go to your **Key Vault** in the Azure Portal.
2. Select **Access control (IAM)** > **Add role assignment**.
3. Role: **Key Vault Secrets User** (or **Key Vault Reader** for read-only, or **Key Vault Administrator** for full access).
4. Assign access to: **User, group, or service principal**.
5. Select your app registration (search for the name you used).
6. Click **Save**.

## 5. Set Environment Variables

Set these variables on your server, in your deployment pipeline, or as Docker secrets:

- `AZURE_CLIENT_ID` = Application (client) ID
- `AZURE_TENANT_ID` = Directory (tenant) ID
- `AZURE_CLIENT_SECRET` = The client secret value

**Example (PowerShell):**
```powershell
$env:AZURE_CLIENT_ID = "<your-client-id>"
$env:AZURE_TENANT_ID = "<your-tenant-id>"
$env:AZURE_CLIENT_SECRET = "<your-client-secret>"
```

**Example (Docker Compose):**
```yaml
environment:
  - AZURE_CLIENT_ID=<your-client-id>
  - AZURE_TENANT_ID=<your-tenant-id>
  - AZURE_CLIENT_SECRET=<your-client-secret>
```

## 6. Restart Your Service

After setting the environment variables, restart your service or container. `DefaultAzureCredential` will now use these credentials to access Azure Key Vault.

## 7. Troubleshooting: Common Errors

### 403 Forbidden: Caller is not authorized to perform action on resource

**Error Example:**
```
Azure.RequestFailedException: Caller is not authorized to perform action on resource.
If role assignments, deny assignments or role definitions were changed recently, please observe propagation time.
Caller: appid=...;oid=...;iss=...
Action: 'Microsoft.KeyVault/vaults/secrets/readMetadata/action'
Resource: '/subscriptions/.../resourcegroups/.../providers/microsoft.keyvault/vaults/bloodtracker'
Assignment: (not found)
DenyAssignmentId: null
DecisionReason: null
Vault: BloodTracker;location=northeurope
Status: 403 (Forbidden)
ErrorCode: Forbidden
```

**Cause:**
- The service principal (App Registration) does not have the required Key Vault role assignment.
- You may have skipped step 4 (granting access to Key Vault), or the assignment has not yet propagated.

**Solution:**
1. Go to your Key Vault in the Azure Portal.
2. Assign the correct role (e.g., Key Vault Secrets User) to your app registration.
3. Wait a few minutes for RBAC changes to propagate.
4. Restart your service and try again.

**Tip:** Always verify role assignments and allow time for Azure RBAC propagation after changes.

---

**Security Note:**
- Never commit secrets to source control.
- Rotate client secrets regularly.
- Use Azure Managed Identity if running in Azure for even better security (no secrets required).
