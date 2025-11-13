#!/usr/bin/env sh
# verify-base-image.sh
# Usage: ./verify-base-image.sh mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <image:tag>"
  exit 2
fi

IMAGE="$1"

echo "Inspecting image manifest for $IMAGE"

# Attempt to use buildx imagetools if available
if command -v docker-buildx >/dev/null 2>&1 || docker buildx version >/dev/null 2>&1; then
  echo "Using docker buildx imagetools"
  docker buildx imagetools inspect --raw "$IMAGE"
  exit $?
fi

echo "docker buildx not available. You can run this in CI or install buildx (docker/setup-buildx-action in GitHub Actions)."
exit 1
