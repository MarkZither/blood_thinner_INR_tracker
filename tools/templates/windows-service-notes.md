Windows service notes for BloodThinnerTracker

- Use `sc.exe` or NSSM to register the API executable as a service.
- Example with `sc.exe` (run as Administrator):

  sc create BloodThinnerTrackerApi binPath= "C:\\deployments\\api\\current\\BloodThinnerTracker.Api.exe" start= auto

- Remember to open firewall ports for the service (example PowerShell):

  New-NetFirewallRule -DisplayName "Allow BloodThinnerTracker API" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow

- Recommended: Use BitLocker or platform disk encryption for Windows hosts. Store TLS cert location securely and restrict file permissions on private key files.
