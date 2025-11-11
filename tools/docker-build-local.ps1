param(
    [string]$Tag = 'local',
    [switch]$Push = $false,
    [string]$DockerHubUser = 'markzither'
)

function Test-DockerfileIsValid {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $false }
    $content = Get-Content $Path -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { return $false }
    # Basic sanity checks: no markdown fences and contains FROM
    if ($content -match '```' -or $content -notmatch '\bFROM\b') { return $false }
    return $true
}

function Build-Image {
    param(
        [string]$Dockerfile,
        [string]$Context,
        [string]$ImageName
    )
    Write-Host "Building $ImageName from $Dockerfile..."
    docker build -f $Dockerfile -t $ImageName $Context
    if ($LASTEXITCODE -ne 0) { throw "docker build failed for $ImageName" }
}

function Tag-And-Push {
    param([string]$LocalImage, [string]$RemoteImage, [switch]$Push)
    docker tag $LocalImage $RemoteImage
    if ($LASTEXITCODE -ne 0) { throw "docker tag failed: $LocalImage -> $RemoteImage" }
    if ($Push) {
        Write-Host "Pushing $RemoteImage ..."
        docker push $RemoteImage
        if ($LASTEXITCODE -ne 0) { throw "docker push failed: $RemoteImage" }
    }
}

try {
    $repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path + '\..' | Resolve-Path -Relative
} catch {
    $repoRoot = Get-Location
}

$apiDockerfile = 'src/BloodThinnerTracker.Api/Dockerfile'
$webDockerfile = 'src/BloodThinnerTracker.Web/Dockerfile'

$imagesBuilt = @()

# Build Web
if (Test-DockerfileIsValid $webDockerfile) {
    $webImageLocal = "bloodthinner-web:$Tag"
    Build-Image -Dockerfile $webDockerfile -Context '.' -ImageName $webImageLocal
    $webRemote = "$DockerHubUser/bloodthinner-web:$Tag"
    Tag-And-Push -LocalImage $webImageLocal -RemoteImage $webRemote -Push:$Push
    $imagesBuilt += $webRemote
} else {
    Write-Warning "Web Dockerfile '$webDockerfile' is missing or invalid. Skipping Web build."
}

# Build API
if (Test-DockerfileIsValid $apiDockerfile) {
    $apiImageLocal = "bloodthinner-api:$Tag"
    Build-Image -Dockerfile $apiDockerfile -Context '.' -ImageName $apiImageLocal
    $apiRemote = "$DockerHubUser/bloodthinner-api:$Tag"
    Tag-And-Push -LocalImage $apiImageLocal -RemoteImage $apiRemote -Push:$Push
    $imagesBuilt += $apiRemote
} else {
    Write-Warning "API Dockerfile '$apiDockerfile' is missing or invalid. Skipping API build."
}

Write-Host "Done. Images built/pushed:"
$imagesBuilt | ForEach-Object { Write-Host " - $_" }
