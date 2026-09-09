<#
.SYNOPSIS
    Build and validate a local Thunderstore package for Pied Piper.
.DESCRIPTION
    Creates a clean ZIP under dist/ with manifest.json, README.md, icon.png, and
    plugins/PiedPiper/PiedPiper.dll at the archive root. Refuses to package an invalid
    manifest, a missing/non-PNG icon, or an icon that is not exactly 256x256.
#>
[CmdletBinding()]
param(
    [string]$ValheimRoot = "",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

function Read-UInt32BigEndian {
    param([byte[]]$Bytes, [int]$Offset)
    return ([uint32]$Bytes[$Offset] -shl 24) -bor
        ([uint32]$Bytes[$Offset + 1] -shl 16) -bor
        ([uint32]$Bytes[$Offset + 2] -shl 8) -bor
        [uint32]$Bytes[$Offset + 3]
}

function Assert-Manifest {
    param([string]$Path)

    try {
        $manifest = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        throw "manifest.json is not valid JSON: $($_.Exception.Message)"
    }

    foreach ($name in @("name", "version_number", "website_url", "description", "dependencies")) {
        if ($null -eq $manifest.$name) {
            throw "manifest.json is missing required field '$name'."
        }
    }

    if ($manifest.name -ne "PiedPiper") {
        throw "manifest.json name must be 'PiedPiper'; found '$($manifest.name)'."
    }
    if ([string]$manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
        throw "version_number must be MAJOR.MINOR.PATCH; found '$($manifest.version_number)'."
    }
    if ([string]::IsNullOrWhiteSpace([string]$manifest.description)) {
        throw "manifest description must not be empty."
    }

    try {
        $website = [Uri]([string]$manifest.website_url)
    }
    catch {
        throw "manifest website_url is not a valid URL."
    }
    if (-not $website.IsAbsoluteUri -or $website.Scheme -notin @("http", "https")) {
        throw "manifest website_url must be an absolute HTTP(S) URL."
    }

    $dependencies = @($manifest.dependencies)
    if ($dependencies.Count -eq 0) {
        throw "manifest dependencies must include BepInExPack Valheim."
    }
    $hasBepInEx = $dependencies | Where-Object {
        [string]$_ -match '^denikson-BepInExPack_Valheim-\d+\.\d+\.\d+$'
    }
    if (-not $hasBepInEx) {
        throw "manifest dependencies do not include a valid denikson-BepInExPack_Valheim dependency string."
    }

    return $manifest
}

function Assert-Icon {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Thunderstore icon is missing: $Path"
    }

    [byte[]]$bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 24) {
        throw "icon.png is too small to be a valid PNG."
    }

    [byte[]]$signature = @(137, 80, 78, 71, 13, 10, 26, 10)
    for ($i = 0; $i -lt $signature.Length; $i++) {
        if ($bytes[$i] -ne $signature[$i]) {
            throw "icon.png does not have a valid PNG signature."
        }
    }

    $width = Read-UInt32BigEndian -Bytes $bytes -Offset 16
    $height = Read-UInt32BigEndian -Bytes $bytes -Offset 20
    if ($width -ne 256 -or $height -ne 256) {
        throw "Thunderstore icon must be exactly 256x256; found ${width}x${height}."
    }

    Write-Host "Icon: valid PNG, 256x256."
}

$modRoot = $PSScriptRoot
$repoRoot = Split-Path $modRoot -Parent
$manifestPath = Join-Path $modRoot "manifest.json"
$readmePath = Join-Path $modRoot "README.md"
$iconPath = Join-Path $modRoot "icon.png"
$buildScript = Join-Path $modRoot "build-local.ps1"

foreach ($required in @($manifestPath, $readmePath, $buildScript)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required package input is missing: $required"
    }
}

$manifest = Assert-Manifest -Path $manifestPath
Assert-Icon -Path $iconPath

$buildArgs = @()
if (-not [string]::IsNullOrWhiteSpace($ValheimRoot)) {
    $buildArgs += @("-ValheimRoot", $ValheimRoot)
}
& $buildScript @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Pied Piper build failed."
}

$dll = Join-Path $modRoot "bin\Release\netstandard2.1\PiedPiper.dll"
if (-not (Test-Path -LiteralPath $dll)) {
    throw "Build completed but PiedPiper.dll was not found: $dll"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "dist"
}

$version = [string]$manifest.version_number
$stage = Join-Path $OutputRoot "PiedPiper-$version"
$zip = Join-Path $OutputRoot "PiedPiper-$version.zip"

if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

[void][IO.Directory]::CreateDirectory($stage)
$pluginStage = Join-Path $stage "plugins\PiedPiper"
[void][IO.Directory]::CreateDirectory($pluginStage)

Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $stage "manifest.json")
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $stage "README.md")
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stage "icon.png")
Copy-Item -LiteralPath $dll -Destination (Join-Path $pluginStage "PiedPiper.dll")

$expected = @(
    "icon.png",
    "manifest.json",
    "README.md",
    "plugins/PiedPiper/PiedPiper.dll"
)
$actual = Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {
    $_.FullName.Substring($stage.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
} | Sort-Object

$missing = @($expected | Where-Object { $_ -notin $actual })
$unexpected = @($actual | Where-Object { $_ -notin $expected })
if ($missing.Count -gt 0 -or $unexpected.Count -gt 0) {
    throw "Package staging mismatch. Missing=[$($missing -join ', ')] Unexpected=[$($unexpected -join ', ')]"
}

Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -CompressionLevel Optimal
if (-not (Test-Path -LiteralPath $zip)) {
    throw "Package ZIP was not created."
}

$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
Write-Host ""
Write-Host "Pied Piper Thunderstore package ready for local upload validation."
Write-Host "Version : $version"
Write-Host "ZIP     : $zip"
Write-Host "SHA-256 : $hash"
Write-Host "Files   : $($actual.Count)"
