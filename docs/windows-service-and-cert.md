# Windows Service and Kestrel Certificate Configuration

This document describes the minimal changes made to run the API and Web as Windows services and how to configure TLS for Kestrel.

Config keys
- `Kestrel:Certificates:Default:Thumbprint` - A certificate thumbprint present in `LocalMachine\My` certificate store. When set, Kestrel will attempt to load the cert by thumbprint and use it for HTTPS.
- `Kestrel:Certificates:Default:Path` - Path to a PFX file. If provided and exists, Kestrel will use this PFX for HTTPS.
- `Kestrel:Certificates:Default:Password` - Password for the PFX file (if Path is used).

Notes
- The host is configured to call `UseWindowsService()` when available. Ensure the Microsoft.Extensions.Hosting.WindowsServices package is referenced for Windows deployments if building there.
- The deploy scripts do not modify application code; to enable HTTPS in production, operators should ensure the certificate is installed in `LocalMachine\My` and set the thumbprint in either `appsettings.Production.json` or as an environment variable for the service.
- Private key permissions: When importing a PFX into LocalMachine\My, ensure the service account has read access to the private key file. The `tools/bind-windows-cert.ps1` helper (T060) will automate this step when implemented.

Recommended operator flow
1. Install certificate (PFX) to LocalMachine\My or place PFX on disk.
2. Grant private key read access to the service account (e.g., `NT SERVICE\BloodTrackerApi`).
3. Set `Kestrel:Certificates:Default:Thumbprint` or `Kestrel:Certificates:Default:Path` in `appsettings.Production.json` or as an environment variable for the Windows service.
4. Restart the service.

Security considerations
- Avoid storing PFX passwords in plaintext in repo files. Use Key Vault or a secure secret store; the code already reads KeyVault when in Production.
- Prefer certificate thumbprint from `LocalMachine\My` when possible; manage private key ACLs carefully.

