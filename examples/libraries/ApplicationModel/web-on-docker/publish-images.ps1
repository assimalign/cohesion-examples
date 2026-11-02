# Builds the shop's two Web programs as container images and loads them into the local engine's image
# store, as shop-orders:latest and shop-storefront:latest, where the gateway runs them.
#
# The engine is Podman or Docker, the first one on PATH (Podman first) unless -Engine names one. The
# images run on the engine's architecture: a Podman machine on an arm64 host is linux/arm64 and cannot
# run an x64 image. Without -RuntimeIdentifier the script asks the engine.
[CmdletBinding()]
param(
    [ValidateSet('', 'Podman', 'Docker')][string] $Engine = '',
    [ValidateSet('', 'linux-x64', 'linux-arm64')][string] $RuntimeIdentifier = ''
)
$ErrorActionPreference = 'Stop'

if (-not $Engine) {
    $Engine = if (Get-Command podman -ErrorAction SilentlyContinue) { 'Podman' }
              elseif (Get-Command docker -ErrorAction SilentlyContinue) { 'Docker' }
              else { throw 'Neither podman nor docker is on PATH.' }
}

if (-not $RuntimeIdentifier) {
    $architecture = if ($Engine -eq 'Podman') { podman info --format '{{.Host.Arch}}' } else { docker version --format '{{.Server.Arch}}' }
    if ($LASTEXITCODE -ne 0) { throw "Reading the engine's architecture failed (exit $LASTEXITCODE)." }
    $RuntimeIdentifier = switch ($architecture.Trim()) {
        'amd64' { 'linux-x64' }
        'arm64' { 'linux-arm64' }
        default { throw "The engine's architecture '$architecture' has no .NET runtime identifier here." }
    }
}

# LocalRegistry hands each image to the engine's CLI (podman load or docker load) instead of a registry.
foreach ($project in 'Shop.Orders', 'Shop.Storefront') {
    dotnet publish (Join-Path $PSScriptRoot $project "$project.csproj") `
        -c Release `
        -r $RuntimeIdentifier `
        -t:PublishContainer `
        "-p:LocalRegistry=$Engine" `
        -nologo
    if ($LASTEXITCODE -ne 0) { throw "Publishing the $project image failed (exit $LASTEXITCODE)." }
}
Write-Host "Loaded shop-orders:latest and shop-storefront:latest ($RuntimeIdentifier) into $Engine."

# The gateway must reach the same engine. On Windows it defaults to the docker_engine pipe, which a
# Podman machine serves only when Docker Desktop does not hold it; name the machine's own pipe then.
if ($Engine -eq 'Podman' -and $IsWindows) {
    $pipe = podman machine inspect --format '{{.ConnectionInfo.PodmanPipe.Path}}' 2>$null | Select-Object -First 1
    if ($LASTEXITCODE -eq 0 -and $pipe) {
        $endpoint = 'npipe:' + ($pipe.Trim() -replace '\\', '/')
        Write-Host "If Docker Desktop is also installed, run the gateway against this engine with: dotnet run --project Shop.Gateway -- --endpoint $endpoint"
    }
}
