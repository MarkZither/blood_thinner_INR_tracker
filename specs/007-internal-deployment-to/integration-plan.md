# Integration test plan (lightweight harness)

Purpose
-------
Provide a clear, small-footprint integration test harness design that implementers can use to validate deployment scripts end-to-end against a controlled test environment (VM or container). These tests are intended to satisfy the constitution requirement for integration tests while remaining lightweight and suitable for CI or local runs.

Environment
-----------
- Test VM: A disposable VM or container image with SSH access, .NET 10 runtime, and minimal tooling (tar, unzip, systemd for Linux image). Prefer using a lightweight Linux image (Ubuntu Server or Debian slim) for RPi-like runs, and a Windows Server Core container or VM for Windows tests.
- Network: Test harness requires network connectivity from the CI runner to the test VM (private network or ephemeral host). Use ephemeral environments (e.g., GitHub Actions self-hosted runner, or local Vagrant/HashiCorp Packer-built image) where possible.
- Credentials: SSH key pair for the CI runner account that is injected into the test VM on boot. Keys must be ephemeral and rotated per-run.

Test harness responsibilities
---------------------------
- Provision test VM/container (or use pre-provisioned image) with a deterministic hostname.
- Ensure required prerequisites on VM (disk, .NET runtime, user account) are present; run the script prereq checks in `tools/deploy-to-pi.ps1`/`tools/check-windows-prereqs.ps1` as part of setup.
- Copy published artifacts to the VM using scp/WinRM and follow the same steps an operator would run (service registration, config placement).
- Start the service and assert health endpoint responds (HTTP 200) within the configured timeout.
- Perform a basic functional validation (create a sample record or call a health-verified endpoint) to ensure the service handles a basic request.
- Run rollback: simulate a prior artifact restore and assert service returns to previous behavior.

Test cases (minimal)
--------------------
1. Provision VM, install prerequisites, and verify prereq checks pass.
2. Deploy published artifact, register as service, start service, assert `/health` = 200.
3. Replace artifact with a new version (simulated), restart service, assert `/health` = 200 and version changed.
4. Run rollback to previous artifact, restart service, assert `/health` = 200 and version reverted.
5. Validate log files exist at configured paths and are readable.

CI integration
--------------
- Add a job to CI that provisions a disposable test VM (or reuses a warmed VM), runs the integration tests, and tears down the VM on completion.
- Publish artifacts for each run and store logs/artifacts as CI job artifacts for auditing.
- If full CI integration is not possible for the initial iteration, provide documented local runner steps in this file.

Security and cost considerations
-------------------------------
- Limit test VM lifetime to the job duration and rotate SSH keys per run.
- Use small instance sizes to reduce cost; prefer containers when Windows-specific behavior is not required.

Deliverables
------------
- `specs/007-internal-deployment-to/integration-plan.md` (this file)
- `specs/007-internal-deployment-to/integration/` (placeholder for integration test code - created in implementation phase)
- CI job definition or documented local-run steps (added to `specs/007-internal-deployment-to/integration-plan.md` or the repo CI dir)

Acceptance
----------
- Integration job runs in CI or locally and completes the minimal test cases above without manual intervention.
