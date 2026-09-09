<#
.SYNOPSIS
    Build, install, disable, enable, uninstall, or inspect Celestial Dial.
.DESCRIPTION
    Local helper only. WulfPack Mods uses zero GitHub Actions. This script manages
    Celestial Dial and nothing else.
#>
[CmdletBinding(DefaultParameterSetName = "Build")]
param(
    [string]$ValheimRoot = "",
    [Parameter(ParameterSetName = "Install")] [switch]$Install,
    [Parameter(ParameterSetName = "Uninstall")] [switch]$Uninstall,
    [Parameter(ParameterSetName = "Enable")] [switch]$Enable,
    [Parameter(ParameterSetName = "Disable")] [switch]$Disable,
    [Parameter(ParameterSetName = "Status")] [switch]$Status
)

$ErrorActionPreference = "Stop"
$PluginLeaf = "CelestialDial"

function Get-SteamRoot {
    foreach ($key in @("HKCU:\SOFTWARE\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam")) {
        try { $p = Get-ItemProperty -Path $key -ErrorAction Stop } catch { continue }
        foreach ($name in @("SteamPath", "InstallPath")) {
            $value = $p.$name
            if ($value -and (Test-Path -LiteralPath $value)) {
                return ([string]$value).Replace("/", "\")
            }
        }
    }
    return $null
}

function Find-ValheimRoot {
    $candidates = New-Object System.Collections.Generic.List[string]
    $steam = Get-SteamRoot
    if ($steam) {
        $candidates.Add((Join-Path $steam "steamapps\common\Valheim"))
        $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
        if (Test-Path -LiteralPath $vdf) {
            foreach ($m in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $lib = $m.Groups[1].Value -replace '\\\\', '\'
                $candidates.Add((Join-Path $lib "steamapps\common\Valheim"))
            }
        }
    }

    $candidates.AddRange([string[]]@(
        "C:\Program Files (x86)\Steam\steamapps\common\Valheim",
        "C:\Program Files\Steam\steamapps\common\Valheim"))

    foreach ($candidate in $candidates) {
        $assembly = Join-Path $candidate "valheim_Data\Managed\assembly_valheim.dll"
        if ((Test-Path -LiteralPath $candidate) -and (Test-Path -LiteralPath $assembly)) {
            return $candidate
        }
    }
    return $null
}

function Resolve-Paths {
    param([string]$Root)
    if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Find-ValheimRoot }
    if ([string]::IsNullOrWhiteSpace($Root) -or -not (Test-Path -LiteralPath $Root)) {
        throw "Valheim installation not found. Pass -ValheimRoot 'C:\path\to\Valheim'."
    }

    $bep = Join-Path $Root "BepInEx"
    if (-not (Test-Path -LiteralPath $bep)) { throw "BepInEx was not found under '$Root'." }

    [pscustomobject]@{
        Root = $Root
        Managed = Join-Path $Root "valheim_Data\Managed"
        Core = Join-Path $bep "core"
        Enabled = Join-Path $bep "plugins\$PluginLeaf"
        Disabled = Join-Path $bep "plugins-disabled\$PluginLeaf"
        Log = Join-Path $bep "LogOutput.log"
    }
}

function Ensure-Stopped {
    if (Get-Process -Name "valheim" -ErrorAction SilentlyContinue) {
        throw "Valheim is running. Quit the game before changing installed mod files."
    }
}

function Assert-OwnedPath {
    param([string]$Path)
    $item = Get-Item -LiteralPath $Path -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing to manage '$Path': it is a reparse point."
    }
    if ((Split-Path $item.FullName -Leaf) -ne $PluginLeaf) {
        throw "Refusing to manage '$Path': unexpected leaf name."
    }
    return $item
}

function Remove-OwnedPath {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Assert-OwnedPath -Path $Path
    Remove-Item -LiteralPath $item.FullName -Recurse -Force
    return $true
}

function Invoke-Build {
    param($Paths)
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK not found on PATH."
    }

    $env:VALHEIM_MANAGED = $Paths.Managed
    $env:BEPINEX_CORE = $Paths.Core
    dotnet build (Join-Path $PSScriptRoot "CelestialDial.csproj") -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Celestial Dial build failed." }

    $dll = Join-Path $PSScriptRoot "bin\Release\netstandard2.1\CelestialDial.dll"
    if (-not (Test-Path -LiteralPath $dll)) {
        throw "Build succeeded but CelestialDial.dll was not found."
    }
    Write-Host "Built: $dll"
    return $dll
}

function Move-State {
    param([string]$From, [string]$To, [string]$Label)
    if (-not (Test-Path -LiteralPath $From)) {
        Write-Host "Nothing to move from: $From"
        return
    }
    Assert-OwnedPath -Path $From | Out-Null
    if (Test-Path -LiteralPath $To) { throw "Destination already exists: $To" }
    [void][IO.Directory]::CreateDirectory((Split-Path $To -Parent))
    Move-Item -LiteralPath $From -Destination $To
    Write-Host "${Label}: $To"
}

function Show-Status {
    param($Paths)
    $enabledDll = Join-Path $Paths.Enabled "CelestialDial.dll"
    $disabledDll = Join-Path $Paths.Disabled "CelestialDial.dll"
    $state = if (Test-Path -LiteralPath $enabledDll) { "INSTALLED (enabled)" }
             elseif (Test-Path -LiteralPath $disabledDll) { "INSTALLED (disabled)" }
             else { "NOT INSTALLED" }

    Write-Host ""
    Write-Host "Celestial Dial : $state"
    Write-Host "Valheim        : $($Paths.Root)"
    if (Test-Path -LiteralPath $Paths.Log) {
        $hit = Select-String -LiteralPath $Paths.Log -Pattern "Celestial Dial" -ErrorAction SilentlyContinue | Select-Object -Last 1
        if ($hit) { Write-Host "Last log hit   : $($hit.Line.Trim())" }
    }
    Write-Host ""
}

$paths = Resolve-Paths -Root $ValheimRoot

switch ($PSCmdlet.ParameterSetName) {
    "Install" {
        Ensure-Stopped
        $dll = Invoke-Build -Paths $paths
        [void][IO.Directory]::CreateDirectory($paths.Enabled)
        Copy-Item -LiteralPath $dll -Destination (Join-Path $paths.Enabled "CelestialDial.dll") -Force
        if (Test-Path -LiteralPath $paths.Disabled) { Remove-OwnedPath -Path $paths.Disabled | Out-Null }
        Write-Host "Installed Celestial Dial."
        Show-Status -Paths $paths
    }
    "Disable" {
        Ensure-Stopped
        Move-State -From $paths.Enabled -To $paths.Disabled -Label "Disabled"
        Show-Status -Paths $paths
    }
    "Enable" {
        Ensure-Stopped
        Move-State -From $paths.Disabled -To $paths.Enabled -Label "Enabled"
        Show-Status -Paths $paths
    }
    "Uninstall" {
        Ensure-Stopped
        $count = 0
        if (Remove-OwnedPath -Path $paths.Enabled) { $count++ }
        if (Remove-OwnedPath -Path $paths.Disabled) { $count++ }
        Write-Host "Uninstalled Celestial Dial. Removed $count owned path(s)."
        Show-Status -Paths $paths
    }
    "Status" { Show-Status -Paths $paths }
    default { Invoke-Build -Paths $paths | Out-Null }
}
