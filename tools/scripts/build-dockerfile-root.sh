#!/bin/bash
# Temporary Dockerfile root build script for Azure Container Apps
# Usage: ./build-dockerfile-root.sh

set -e
PROJECT_DOCKERFILE="src/BloodThinnerTracker.Api/Dockerfile"
ROOT_DOCKERFILE="Dockerfile"

echo "Copying API Dockerfile to repo root..."
cp "$PROJECT_DOCKERFILE" "$ROOT_DOCKERFILE"

echo "Building Docker image from root..."
docker build -t bloodtracker-api:test -f Dockerfile .

echo "Running az containerapp up from root..."
az containerapp up \
  --name bloodtrackerapi \
  --resource-group FreeNorthEurope \
  --location northeurope \
  --source . \
  --ingress external \
  --target-port 5234 \
  --env-vars ASPNETCORE_ENVIRONMENT=Production

echo "Cleaning up temporary Dockerfile..."
rm "$ROOT_DOCKERFILE"

echo "Done."
