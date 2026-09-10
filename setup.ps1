[CmdletBinding()]
param(
    [switch] $Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = $PSScriptRoot
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

function ConvertTo-SolutionPath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return $Path.Replace('/', '\')
}

function Get-SolutionEntries {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectRoot,

        [Parameter(Mandatory)]
        [string] $SolutionRoot,

        [string] $FolderPrefix,

        [int] $ScaffoldOrder
    )

    $resolvedProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
    $resolvedSolutionRoot = [System.IO.Path]::GetFullPath($SolutionRoot)
    $projects = @(
        Get-ChildItem -LiteralPath $resolvedProjectRoot -Recurse -File -Filter '*.csproj' |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    )

    if ($projects.Count -eq 0) {
        throw "No projects were found under '$resolvedProjectRoot'."
    }

    foreach ($project in $projects) {
        $relativeWithinScaffold = [System.IO.Path]::GetRelativePath($resolvedProjectRoot, $project.FullName)
        $segments = @($relativeWithinScaffold -split '[\\/]')
        $folderSegments = [System.Collections.Generic.List[string]]::new()

        if (-not [string]::IsNullOrWhiteSpace($FolderPrefix)) {
            $folderSegments.Add($FolderPrefix)
        }

        if ($segments.Count -gt 2) {
            foreach ($segment in $segments[0..($segments.Count - 3)]) {
                $folderSegments.Add($segment)
            }
        }

        [pscustomobject]@{
            Folder = $folderSegments -join '/'
            Path = ConvertTo-SolutionPath ([System.IO.Path]::GetRelativePath($resolvedSolutionRoot, $project.FullName))
            ScaffoldOrder = $ScaffoldOrder
        }
    }
}

function ConvertTo-SolutionXml {
    param(
        [Parameter(Mandatory)]
        [object[]] $Entries,

        [string[]] $RootFiles = @()
    )

    $builder = [System.Text.StringBuilder]::new()
    [void] $builder.AppendLine('<Solution>')

    if ($RootFiles.Count -gt 0) {
        [void] $builder.AppendLine('  <Folder Name="/cohesion-examples/">')
        foreach ($rootFile in $RootFiles) {
            $escapedPath = [System.Security.SecurityElement]::Escape($rootFile)
            [void] $builder.AppendLine(('    <File Path="{0}" />' -f $escapedPath))
        }
        [void] $builder.AppendLine('  </Folder>')
    }

    $orderedEntries = @($Entries | Sort-Object ScaffoldOrder, Folder, Path)
    $orderedFolders = @($orderedEntries | Select-Object -ExpandProperty Folder -Unique)
    foreach ($folder in $orderedFolders) {
        $folderEntries = @($orderedEntries | Where-Object { $_.Folder -eq $folder })
        if ([string]::IsNullOrWhiteSpace($folder)) {
            foreach ($entry in $folderEntries) {
                $escapedPath = [System.Security.SecurityElement]::Escape($entry.Path)
                [void] $builder.AppendLine(('  <Project Path="{0}" />' -f $escapedPath))
            }
            continue
        }

        $escapedFolder = [System.Security.SecurityElement]::Escape($folder)
        [void] $builder.AppendLine(('  <Folder Name="/{0}/">' -f $escapedFolder))
        foreach ($entry in $folderEntries) {
            $escapedPath = [System.Security.SecurityElement]::Escape($entry.Path)
            [void] $builder.AppendLine(('    <Project Path="{0}" />' -f $escapedPath))
        }
        [void] $builder.AppendLine('  </Folder>')
    }

    [void] $builder.AppendLine('</Solution>')
    return $builder.ToString()
}

function Test-OrWriteGeneratedFile {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Content
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $existingContent = if ([System.IO.File]::Exists($resolvedPath)) {
        [System.IO.File]::ReadAllText($resolvedPath)
    }
    else {
        ''
    }

    $normalizedExisting = $existingContent.Replace("`r`n", "`n")
    $normalizedExpected = $Content.Replace("`r`n", "`n")
    if ([string]::Equals($normalizedExisting, $normalizedExpected, [System.StringComparison]::Ordinal)) {
        Write-Host "Up to date: $resolvedPath"
        return $false
    }

    if ($Check) {
        Write-Warning "Out of date: $resolvedPath"
        return $true
    }

    [System.IO.File]::WriteAllText($resolvedPath, $Content, $utf8WithoutBom)
    Write-Host "Generated: $resolvedPath"
    return $true
}

$scaffolds = @(
    [pscustomobject]@{
        Name = 'single-app'
        Root = Join-Path $repositoryRoot 'examples/single-app'
        Solution = Join-Path $repositoryRoot 'examples/single-app/Acme.slnx'
        Order = 0
    },
    [pscustomobject]@{
        Name = 'k8s'
        Root = Join-Path $repositoryRoot 'examples/k8s'
        Solution = Join-Path $repositoryRoot 'examples/k8s/Example.K8s.slnx'
        Order = 1
    },
    [pscustomobject]@{
        Name = 'k8s-federated'
        Root = Join-Path $repositoryRoot 'examples/k8s-federated'
        Solution = Join-Path $repositoryRoot 'examples/k8s-federated/Example.Federated.slnx'
        Order = 2
    }
)

$rootEntries = @()
$generatedFilesAreStale = $false
foreach ($scaffold in $scaffolds) {
    $scaffoldEntries = @(
        Get-SolutionEntries `
            -ProjectRoot $scaffold.Root `
            -SolutionRoot $scaffold.Root `
            -ScaffoldOrder $scaffold.Order
    )
    $scaffoldXml = ConvertTo-SolutionXml -Entries $scaffoldEntries
    if (Test-OrWriteGeneratedFile -Path $scaffold.Solution -Content $scaffoldXml) {
        $generatedFilesAreStale = $true
    }

    $rootEntries += @(
        Get-SolutionEntries `
            -ProjectRoot $scaffold.Root `
            -SolutionRoot $repositoryRoot `
            -FolderPrefix $scaffold.Name `
            -ScaffoldOrder $scaffold.Order
    )
}

$rootSolutionPath = Join-Path $repositoryRoot 'Assimalign.Cohesion.Examples.slnx'
$rootSolutionXml = ConvertTo-SolutionXml `
    -Entries $rootEntries `
    -RootFiles @('.gitignore', 'Directory.Build.props', 'Directory.Build.targets', 'global.json', 'nuget.config', 'README.md', 'setup.ps1')
if (Test-OrWriteGeneratedFile -Path $rootSolutionPath -Content $rootSolutionXml) {
    $generatedFilesAreStale = $true
}

if ($Check -and $generatedFilesAreStale) {
    throw 'One or more generated solution files are out of date. Run ./setup.ps1 and commit the results.'
}
