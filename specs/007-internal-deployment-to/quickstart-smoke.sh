#!/bin/sh
# Placeholder smoke-check script that triggers the health endpoint. Replace with platform-appropriate version.
HOST="localhost:5000"

echo "Checking health endpoint http://$HOST/health"
curl -sSf "http://$HOST/health" || echo "Health check failed (placeholder)"
