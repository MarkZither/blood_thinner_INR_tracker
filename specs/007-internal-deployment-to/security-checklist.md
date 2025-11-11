# Security Checklist — Internal deployment

This checklist documents the verification steps required for FR-015 and the TLS requirements (FR-016 transport posture).

Encryption at rest
- [ ] Verify target Windows host has BitLocker enabled for system and data volumes: run `Get-BitLockerVolume` and confirm `VolumeStatus` is `FullyEncrypted`.
- [ ] Verify target Linux host uses LUKS or host-managed disk encryption: run `lsblk -o NAME,FSTYPE` and check for `crypto_LUKS` or check `/etc/crypttab`.
- [ ] Ensure rollback artifact locations and scratch paths are stored on encrypted volumes.
- Verification scripts:
  - `tools/verify-encryption.ps1` — PowerShell script for local/remote checks.
  - `tools/verify-encryption.sh` — simple POSIX check for LUKS/crypttab.

TLS verification
- [ ] Obtain certificate (example: `tailscale cert <hostname>`) and install into the host's TLS store or file paths referenced in `deployment.config`.
- [ ] Verify TLS 1.3 negotiation against the host: use `tools/verify-tls.ps1 -Host <hostname> -Port 443` (operator machine must have openssl or curl installed).
- [ ] Confirm certificate CN/SAN matches the Tailnet hostname used by operators and that private key file permissions are restricted.

Notes
- These steps are operator-run and intended for manual verification during deployment. Automating these checks in CI for remote hosts is out of scope for this iteration.
