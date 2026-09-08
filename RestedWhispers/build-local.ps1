param(
    [string]$ValheimRoot = "",
    [switch]$Install
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ValheimRoot)) {
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Valheim",
        "C:\Program Files\Steam\steamapps\common\Valheim"
    )

    $ValheimRoot = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($ValheimRoot) -or -not (Test-Path $ValheimRoot)) {
    throw "Valheim installation not found. Pass -ValheimRoot 'C:\path\to\Valheim'."
}

$managed = Join-Path $ValheimRoot "valheim_Data\Managed"
$bepInExCore = Join-Path $ValheimRoot "BepInEx\core"

if (-not (Test-Path (Join-Path $managed "assembly_valheim.dll"))) {
    throw "assembly_valheim.dll was not found under $managed."
}

if (-not (Test-Path (Join-Path $bepInExCore "BepInEx.dll"))) {
    throw "BepInEx.dll was not found under $bepInExCore. Install BepInExPack for Valheim first."
}

$env:VALHEIM_MANAGED = $managed
$env:BEPINEX_CORE = $bepInExCore

$project = Join-Path $PSScriptRoot "RestedWhispers.csproj"

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Rested Whispers build failed."
}

$dll = Join-Path $PSScriptRoot "bin\Release\netstandard2.0\RestedWhispers.dll"
if (-not (Test-Path $dll)) {
    throw "Build reported success, but RestedWhispers.dll was not found at $dll."
}

Write-Host "Built: $dll"

if ($Install) {
    $pluginDir = Join-Path $ValheimRoot "BepInEx\plugins\RestedWhispers"
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    Copy-Item $dll (Join-Path $pluginDir "RestedWhispers.dll") -Force
    Write-Host "Installed: $pluginDir\RestedWhispers.dll"
}
