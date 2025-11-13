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

exec "$@"
