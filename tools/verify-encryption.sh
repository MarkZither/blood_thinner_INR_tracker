#!/usr/bin/env bash
# Simple local Linux check for LUKS or crypttab entries
set -e
if command -v lsblk >/dev/null 2>&1; then
  if lsblk -o NAME,FSTYPE | grep -q crypto_LUKS; then
    echo "Detected LUKS-encrypted block devices"
    exit 0
  fi
fi
if [ -f /etc/crypttab ]; then
  if [ -s /etc/crypttab ]; then
    echo "/etc/crypttab contains entries; encrypted devices likely configured"
    exit 0
  fi
fi

echo "No LUKS devices or crypttab entries detected."
exit 1
