# Docker Compose secrets on Alpine (openrc) — recommended setup

This file documents how to place secret files for the `bloodthinner-web` service on an Alpine Linux host (openrc). The compose stack mounts `/run/secrets/bloodtracker` into the container as read-only.

Options
- Ephemeral tmpfs mount (recommended): secrets live in RAM and vanish on reboot.
- Persistent directory on encrypted disk (if you need persistence) under `/etc/bloodtracker/secrets`.

1) One-off tmpfs and file creation (immediate):

```sh
sudo mkdir -p /run/secrets/bloodtracker
sudo mount -t tmpfs -o mode=0700 tmpfs /run/secrets/bloodtracker
sudo sh -c 'umask 077; printf "%s" "<client-id>" > /run/secrets/bloodtracker/AZURE_CLIENT_ID'
sudo sh -c 'umask 077; printf "%s" "<client-secret>" > /run/secrets/bloodtracker/AZURE_CLIENT_SECRET'
sudo sh -c 'umask 077; printf "%s" "<tenant-id>" > /run/secrets/bloodtracker/AZURE_TENANT_ID'
sudo chmod 0400 /run/secrets/bloodtracker/*
```

2) Make the tmpfs mount survive reboots on Alpine (openrc)

Create a small init script at `/etc/local.d/run-secrets-bloodtracker.start`:

```sh
#!/bin/sh
mkdir -p /run/secrets/bloodtracker
mount -t tmpfs -o mode=0700 tmpfs /run/secrets/bloodtracker
chown root:root /run/secrets/bloodtracker
chmod 0700 /run/secrets/bloodtracker
exit 0
```

Make it executable and enable local services to run at boot:

```sh
sudo chmod +x /etc/local.d/run-secrets-bloodtracker.start
sudo rc-update add local default
```

Then create your secret files after the mount is up (or package a provisioning step that creates them on first-boot).

3) Persistent encrypted storage

If you need persistence, create `/etc/bloodtracker/secrets` on an encrypted partition (LUKS) and use the same strict perms (`0700` directory, `0400` files).

SELinux/AppArmor

- Alpine typically does not use SELinux by default; if you have AppArmor or other MAC, verify policies allow Docker to read the host path.

Compose snippet (already applied in project):

```yaml
volumes:
  - /run/secrets/bloodtracker:/run/secrets:ro
  - ./specs/008-install-web-app/docker/docker-entrypoint.sh:/docker-entrypoint.sh:ro
entrypoint: ["/docker-entrypoint.sh"]
```

Security checklist
- Do not store secrets under version control.
- Keep directory mode 0700 and file mode 0400.
- Exclude secret paths from backups, or protect backups with encryption.
- Prefer integrating with Key Vault + External Secrets Operator for production.
