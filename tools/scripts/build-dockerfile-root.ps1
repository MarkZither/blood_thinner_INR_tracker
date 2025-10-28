# Temporary Dockerfile root build script for Azure Container Apps
# Usage: .\tools\scripts\build-dockerfile-root.ps1

$projectDockerfile = "src/BloodThinnerTracker.Api/Dockerfile"
$rootDockerfile = "Dockerfile"

Write-Host "Copying API Dockerfile to repo root..."
Copy-Item $projectDockerfile $rootDockerfile -Force

Write-Host "Building Docker image from root..."
docker build -t bloodtracker-api:test -f Dockerfile .

Write-Host "Running az containerapp up from root..."
az containerapp up `
  --name bloodtrackerapi `
  --resource-group FreeNorthEurope `
  --location northeurope `
  --source . `
  --ingress external `
  --target-port 5234 `
  --env-vars ASPNETCORE_ENVIRONMENT=Production

Write-Host "Cleaning up temporary Dockerfile..."
Remove-Item $rootDockerfile -Force

Write-Host "Done."
