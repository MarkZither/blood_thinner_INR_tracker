# Docker Compose: mount secret files + entrypoint (recommended short-term pattern)

This folder contains an example of using host-mounted secret files plus a simple entrypoint script that reads those files and exports environment variables for the containerized app.

Why this pattern?
- Works with plain Docker Compose (no swarm or external secret manager required).
- Keeps secrets off of images and out of version control (store them on the host; optionally encrypt/rotate outside of Git).
- Easy to migrate later to k3s ExternalSecrets or Secrets Store CSI driver.

Security notes:
- Host secret files should be stored with strict filesystem permissions and never committed to Git.
- For GitOps, use SealedSecrets or your secret management workflow instead of plaintext files.

Platforms:
- For Windows hosts see earlier task notes (pwsh examples).
- For Alpine Linux hosts (openrc) see `README.alpine.md` in this folder for openrc/local.d instructions and tmpfs setup.

Example usage (PowerShell / pwsh):

```powershell
# Create secret dir (on Windows host)
mkdir C:\secrets\bloodtracker

# Create secret files
Set-Content -Path C:\secrets\bloodtracker\AZURE_CLIENT_ID -Value '<client-id>' -NoNewline
Set-Content -Path C:\secrets\bloodtracker\AZURE_CLIENT_SECRET -Value '<client-secret>' -NoNewline
Set-Content -Path C:\secrets\bloodtracker\AZURE_TENANT_ID -Value '<tenant-id>' -NoNewline

# Start the composed services (reads entrypoint which exports the env vars)
docker compose -f specs/008-install-web-app/docker/docker-compose.secrets-example.yml up --build
```

Files in this folder:
- `docker-entrypoint.sh` — simple shell script that exports environment variables from mounted files into environment variables and execs the app.
- `docker-compose.secrets-example.yml` — Compose file showing mounted host secret folder and service definition.
- `README.alpine.md` — Alpine (openrc) specific guidance for mounting `/run/secrets/bloodtracker`.

Entrypoint baked into images
--------------------------------
The `docker-entrypoint.sh` is now baked into the Web and API images at build time. This keeps the script versioned with the source and avoids bind-mounting the script in production.

If you need to override for local development (for example to test a modified entrypoint script), you can override the entrypoint in `docker-compose` using the `entrypoint:` key or by mounting a local modified script (only recommended for dev).

Example override in compose (dev only):

```yaml
services:
  bloodthinner-web:
	  entrypoint: ["/docker-entrypoint.sh", "dotnet", "BloodThinnerTracker.Web.dll"]
	  volumes:
	    - ./docs/docker/docker-entrypoint.sh:/docker-entrypoint.sh:ro
	    - /run/secrets/bloodtracker:/run/secrets:ro,z
```

Verify alternative base images
--------------------------------
If you want to evaluate alternate official base images (for example `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`), use the helper script in this folder to inspect the manifest and available platforms:

```sh
./docs/docker/verify-base-image.sh mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
```

Run this locally or in CI (requires Docker Buildx installed). The script calls `docker buildx imagetools inspect` to list supported platforms and the manifest digest.


