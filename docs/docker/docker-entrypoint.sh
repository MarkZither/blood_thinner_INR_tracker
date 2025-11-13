#!/usr/bin/env sh
set -e

# docker-entrypoint.sh
# Simple entrypoint that exports environment variables from files mounted at /run/secrets
# Expected secret files (examples): AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID

SECRETS_DIR="/run/secrets"

export_if_file() {
  key="$1"
  file="$SECRETS_DIR/$key"
  if [ -f "$file" ]; then
    # Read file content without trailing newline issues
    val=$(cat "$file")
    export "$key"="$val"
    echo "[entrypoint] exported $key from $file"
  fi
}

# List of secrets to export — adjust to your app's needs
export_if_file "AZURE_CLIENT_ID"
export_if_file "AZURE_CLIENT_SECRET"
export_if_file "AZURE_TENANT_ID"
export_if_file "DB_PASSWORD"

# Drop privileges and exec the final command as the non-root app user
# Prefer gosu for proper signal forwarding and exec semantics. Fall back to
# runuser or su if gosu is not available. If no helper is present, exec as-is.
drop_priv_and_exec() {
  target_user="appuser"
  if command -v gosu >/dev/null 2>&1; then
    exec gosu "$target_user" "$@"
  elif command -v runuser >/dev/null 2>&1; then
    # runuser preserves environment and runs command as target user
    exec runuser -u "$target_user" -- "$@"
  elif command -v su >/dev/null 2>&1; then
    # su -c runs command as target user via a shell
    exec su -s /bin/sh -c "\"$*\"" "$target_user"
  else
    # last resort: just exec the command (may run as root)
    exec "$@"
  fi
}

drop_priv_and_exec "$@"
