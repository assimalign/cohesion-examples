# Builds the shop's two Web programs as container images and pushes them to the registry the cluster
# pulls from, as <registry>/shop-orders:latest and <registry>/shop-storefront:latest.
#
# A kind cluster's nodes run on the engine kind uses, Podman or Docker, the first one on PATH (Podman
# first) unless -Engine names one. Its architecture follows the host's: an arm64 machine runs arm64
# nodes, which cannot run an x64 image. Without -RuntimeIdentifier the script builds for the engine's
# architecture. The .NET SDK pushes to the registry itself; the engine is asked only for its
# architecture.
[CmdletBinding()]
param(
    [string] $Registry = 'localhost:5001',
    [ValidateSet('', 'Podman', 'Docker')][string] $Engine = '',
    [ValidateSet('', 'linux-x64', 'linux-arm64')][string] $RuntimeIdentifier = ''
)
$ErrorActionPreference = 'Stop'

if (-not $RuntimeIdentifier) {
    if (-not $Engine) {
        $Engine = if (Get-Command podman -ErrorAction SilentlyContinue) { 'Podman' }
                  elseif (Get-Command docker -ErrorAction SilentlyContinue) { 'Docker' }
                  else { throw 'Neither podman nor docker is on PATH; pass -RuntimeIdentifier.' }
    }
    $architecture = if ($Engine -eq 'Podman') { podman info --format '{{.Host.Arch}}' } else { docker version --format '{{.Server.Arch}}' }
    if ($LASTEXITCODE -ne 0) { throw "Reading the engine's architecture failed (exit $LASTEXITCODE)." }
    $RuntimeIdentifier = switch ($architecture.Trim()) {
        'amd64' { 'linux-x64' }
        'arm64' { 'linux-arm64' }
        default { throw "The engine's architecture '$architecture' has no .NET runtime identifier here." }
    }
}

# A local registry serves plain HTTP; the .NET SDK pushes over HTTP only to registries named here.
$env:DOTNET_CONTAINER_INSECURE_REGISTRIES = $Registry
foreach ($project in 'Shop.Orders', 'Shop.Storefront') {
    dotnet publish (Join-Path $PSScriptRoot $project "$project.csproj") `
        -c Release `
        -r $RuntimeIdentifier `
        -t:PublishContainer `
        "-p:ContainerRegistry=$Registry" `
        -nologo
    if ($LASTEXITCODE -ne 0) { throw "Publishing the $project image failed (exit $LASTEXITCODE)." }
}
Write-Host "Pushed $Registry/shop-orders:latest and $Registry/shop-storefront:latest ($RuntimeIdentifier)."
