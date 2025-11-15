# Docker Compose & Traefik variants

This folder contains platform-specific docker-compose and Traefik dynamic configuration variants.

Files
- `../docker-compose.linux.yml` - Use on Linux hosts (includes SELinux-friendly `:z` mount and unix socket)
- `../docker-compose.windows.yml` - Use on Windows hosts (uses `./secrets` host path and no `:z` label)
- `../dynamic.linux.yml` - Traefik dynamic configuration for Linux setup
- `../dynamic.windows.yml` - Traefik dynamic configuration for Windows setup

Usage
- Linux:
  - docker compose -f docker-compose.linux.yml up --build -d
- Windows:
  - docker compose -f docker-compose.windows.yml up --build -d

Notes
- The Linux variant mounts `/run/secrets/bloodtracker` as a read-only secrets directory with `:z` label for SELinux hosts. The Windows variant expects a `./secrets` folder in the repo root.
- Both variants use a named volume `btdata` to persist the SQLite database at `/data`.

Using external certs (recommended)
- The Traefik service expects TLS certs inside the container at `/certs/cloud.crt` and `/certs/cloud.key` (these are referenced in `dynamic.linux.yml` / `dynamic.windows.yml`).
- For security we recommend keeping certs out of the repo and mounting them from an absolute host path using the `CERTS_HOST_PATH` environment variable.

Example (Linux):

```powershell
# generate mkcert certs in a host folder (outside repo) then start compose with env var
mkcert -cert-file /home/dev/certs/cloud.crt -key-file /home/dev/certs/cloud.key "web.local" "api.local"
setx CERTS_HOST_PATH "/home/dev/certs"
docker compose -f docker-compose.linux.yml up --build -d
```

Example (Windows PowerShell):

```powershell
# generate mkcert certs in a host folder (outside repo) then start compose with env var
mkcert -cert-file C:\Users\dev\certs\cloud.crt -key-file C:\Users\dev\certs\cloud.key "web.local" "api.local"
$env:CERTS_HOST_PATH = 'C:\Users\dev\certs'
docker compose -f docker-compose.windows.yml up --build -d
```

Notes
- On Linux set the env permanently or prefix the compose command with the variable:
  `CERTS_HOST_PATH=/home/dev/certs docker compose -f docker-compose.linux.yml up --build -d`
- On Windows, ensure Docker Desktop is running in Linux container mode (Traefik image is Linux).

