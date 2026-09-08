<#
.SYNOPSIS
    Build, install, disable, enable, uninstall, or inspect Rested Whispers.
.DESCRIPTION
    Local helper; this repository uses no GitHub Actions. Manages Rested
    Whispers and nothing else - it never touches another BepInEx plugin or any
    Thunderstore Mod Manager profile. Reference: RestedWhispers/README.md.
#>
[CmdletBinding(DefaultParameterSetName = "Build")]
param(
    [string]$ValheimRoot = "",
    [Parameter(ParameterSetName = "Install")]   [switch]$Install,
    [Parameter(ParameterSetName = "Uninstall")] [switch]$Uninstall,
    [Parameter(ParameterSetName = "Enable")]    [switch]$Enable,
    [Parameter(ParameterSetName = "Disable")]   [switch]$Disable,
    [Parameter(ParameterSetName = "Status")]    [switch]$Status
)

$ErrorActionPreference = "Stop"

# The only three paths this script owns. Nothing else is ever a write target.
$OwnedPluginLeaf = "RestedWhispers"
$OwnedConfigLeaf = "com.wulfpack.restedwhispers.cfg"

function Get-SteamLibraryRoot {
    foreach ($key in @("HKCU:\SOFTWARE\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam")) {
        try { $p = Get-ItemProperty -Path $key -ErrorAction Stop } catch { continue }
        foreach ($prop in @("SteamPath", "InstallPath")) {
            $v = $p.$prop
            if ($v -and (Test-Path -LiteralPath $v)) { return ([string]$v).Replace("/", "\") }
        }
    }
    return $null
}

function Find-ValheimRoot {
    $candidates = New-Object System.Collections.Generic.List[string]
    $steam = Get-SteamLibraryRoot
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
    $candidates.AddRange([string[]]@("C:\Program Files (x86)\Steam\steamapps\common\Valheim",
                                    "C:\Program Files\Steam\steamapps\common\Valheim"))
    foreach ($c in $candidates) {
        if ((Test-Path -LiteralPath $c) -and (Test-Path -LiteralPath (Join-Path $c "valheim_Data\Managed\assembly_valheim.dll"))) {
            return $c
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
    $bepInEx = Join-Path $Root "BepInEx"
    if (-not (Test-Path -LiteralPath $bepInEx)) {
        throw "No BepInEx directory under '$Root'. Install BepInExPack for Valheim first."
    }
    return [pscustomobject]@{
        Root        = $Root
        Managed     = Join-Path $Root "valheim_Data\Managed"
        Core        = Join-Path $bepInEx "core"
        PluginsDir  = Join-Path $bepInEx "plugins"
        PluginDir   = Join-Path $bepInEx "plugins\$OwnedPluginLeaf"
        DisabledDir = Join-Path $bepInEx "plugins-disabled\$OwnedPluginLeaf"
        ConfigFile  = Join-Path $bepInEx "config\$OwnedConfigLeaf"
        Log         = Join-Path $bepInEx "LogOutput.log"
    }
}

# The ONLY function permitted to delete. Fails closed on anything unexpected.
function Remove-OwnedPath {
    param([string]$Path, [string]$ExpectedLeaf)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Get-Item -LiteralPath $Path -Force
    # Resolve-Path does NOT dereference reparse points, so a leaf-name check
    # alone would pass on a junction pointing somewhere else. This attribute
    # test is what actually detects redirection.
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing to delete '$Path': it is a reparse point ($($item.LinkType))."
    }
    # Guards against a mistyped or truncated path, not against links.
    $full = $item.FullName
    if ((Split-Path $full -Leaf) -ne $ExpectedLeaf) {
        throw "Refusing to delete '$full': leaf is not '$ExpectedLeaf'."
    }
    Remove-Item -LiteralPath $full -Recurse -Force
    return $true
}

# We create BepInEx\plugins-disabled; leave none of it behind once empty.
# Removes the directory only when it is genuinely empty, so a plugin someone
# else parked there is never destroyed.
function Remove-EmptyDisabledParent {
    param([string]$DisabledDir)
    $parent = Split-Path $DisabledDir -Parent
    if (-not (Test-Path -LiteralPath $parent)) { return }
    if ((Split-Path $parent -Leaf) -ne "plugins-disabled") { return }
    $item = Get-Item -LiteralPath $parent -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { return }
    if ((Get-ChildItem -LiteralPath $parent -Force | Measure-Object).Count -ne 0) { return }
    Remove-Item -LiteralPath $item.FullName -Force
    Write-Host "Removed empty directory: $parent"
}

function Move-OwnedDir {
    param([string]$From, [string]$To)
    $item = Get-Item -LiteralPath $From -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing to move '$From': it is a reparse point ($($item.LinkType))."
    }
    $parent = Split-Path $To -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        [void][IO.Directory]::CreateDirectory($parent)
    }
    Move-Item -LiteralPath $From -Destination $To -Force
}

# Disable and Enable are the same move in opposite directions.
function Move-InstallState {
    param($Paths, [string]$From, [string]$To, [string]$DoneLabel,
          [string]$AlreadyLabel, [string]$Note = "")
    if (Test-Path -LiteralPath $From) {
        if (Test-Path -LiteralPath $To) {
            throw "Both '$From' and '$To' exist. Resolve manually."
        }
        Move-OwnedDir -From $From -To $To
        Write-Host "$DoneLabel. Moved to: $To"
        if ($Note) { Write-Host $Note }
        Remove-EmptyDisabledParent -DisabledDir $Paths.DisabledDir
    }
    elseif (Test-Path -LiteralPath $To) { Write-Host "${AlreadyLabel}: $To" }
    else { Write-Host "Not installed; nothing to do. Run with -Install." }
    Show-Status -Paths $Paths
}

function Invoke-Build {
    param($Paths)
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK was not found on PATH. Install a .NET SDK before building Rested Whispers."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Paths.Managed "assembly_valheim.dll"))) {
        throw "assembly_valheim.dll was not found under $($Paths.Managed)."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Paths.Core "BepInEx.dll"))) {
        throw "BepInEx.dll was not found under $($Paths.Core). Install BepInExPack for Valheim first."
    }
    $env:VALHEIM_MANAGED = $Paths.Managed
    $env:BEPINEX_CORE = $Paths.Core
    # Out-Host, not bare invocation: dotnet's stdout would otherwise land in
    # this function's return value alongside $dll.
    dotnet build (Join-Path $PSScriptRoot "RestedWhispers.csproj") -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Rested Whispers build failed." }
    $dll = Join-Path $PSScriptRoot "bin\Release\netstandard2.1\RestedWhispers.dll"
    if (-not (Test-Path -LiteralPath $dll)) {
        throw "Build reported success, but RestedWhispers.dll was not found at $dll."
    }
    Write-Host "Built: $dll"
    return $dll
}

function Get-InstallState {
    param($Paths)
    $enabled = Test-Path -LiteralPath (Join-Path $Paths.PluginDir "RestedWhispers.dll")
    $disabled = Test-Path -LiteralPath (Join-Path $Paths.DisabledDir "RestedWhispers.dll")
    if ($enabled) { return "INSTALLED (enabled)" }
    if ($disabled) { return "INSTALLED (disabled)" }
    return "NOT INSTALLED"
}

function Show-Status {
    param($Paths)
    $cfg = if (Test-Path -LiteralPath $Paths.ConfigFile) { $Paths.ConfigFile } else { "(none)" }
    Write-Host ""
    Write-Host "Rested Whispers : $(Get-InstallState -Paths $Paths)"
    Write-Host "Valheim root    : $($Paths.Root)"
    Write-Host "Plugin path     : $($Paths.PluginDir)"
    Write-Host "Disabled path   : $($Paths.DisabledDir)"
    Write-Host "Config file     : $cfg"
    Write-Host ""
    Write-Host "Other entries in BepInEx\plugins (NOT managed by this script):"
    $others = Get-ChildItem -LiteralPath $Paths.PluginsDir -EA SilentlyContinue |
        Where-Object { $_.Name -ne $OwnedPluginLeaf }
    if ($others) { $others | ForEach-Object { Write-Host "  - $($_.Name)" } } else { Write-Host "  (none)" }
    Write-Host ""
    $hit = if (Test-Path -LiteralPath $Paths.Log) {
        Select-String -LiteralPath $Paths.Log -Pattern "Rested Whispers" -EA SilentlyContinue | Select-Object -Last 1
    } else { $null }
    if ($hit) { Write-Host "Last BepInEx log mention: $($hit.Line.Trim())" }
    else { Write-Host "BepInEx log has no Rested Whispers entry (or no log yet)." }
    Write-Host ""
    Write-Host "The log reflects the LAST run of the game. Restart Valheim after any change before trusting it."
}

# ---------------------------------------------------------------- dispatch --

$paths = Resolve-Paths -Root $ValheimRoot
# BepInEx memory-maps loaded plugin DLLs, so copying, moving or deleting one
# fails while the game is open. Fail with guidance, not a raw IOException.
if ($PSCmdlet.ParameterSetName -in @("Install", "Uninstall", "Enable", "Disable") -and
    (Get-Process -Name "valheim" -ErrorAction SilentlyContinue)) {
    throw "Valheim is running. Quit the game, then re-run this command."
}

switch ($PSCmdlet.ParameterSetName) {
    "Status" { Show-Status -Paths $paths }
    "Disable" {
        Move-InstallState -Paths $paths -From $paths.PluginDir -To $paths.DisabledDir `
            -DoneLabel "Disabled" -AlreadyLabel "Already disabled" `
            -Note "plugins-disabled is a sibling of plugins, so BepInEx does not scan it."
    }
    "Enable" {
        Move-InstallState -Paths $paths -From $paths.DisabledDir -To $paths.PluginDir `
            -DoneLabel "Enabled" -AlreadyLabel "Already enabled"
    }
    "Uninstall" {
        $removed = 0
        if (Remove-OwnedPath -Path $paths.PluginDir   -ExpectedLeaf $OwnedPluginLeaf) { $removed++; Write-Host "Removed: $($paths.PluginDir)" }
        if (Remove-OwnedPath -Path $paths.DisabledDir -ExpectedLeaf $OwnedPluginLeaf) { $removed++; Write-Host "Removed: $($paths.DisabledDir)" }
        if (Remove-OwnedPath -Path $paths.ConfigFile  -ExpectedLeaf $OwnedConfigLeaf) { $removed++; Write-Host "Removed: $($paths.ConfigFile)" }
        Remove-EmptyDisabledParent -DisabledDir $paths.DisabledDir
        if ($removed -eq 0) { Write-Host "Nothing to remove; Rested Whispers was not installed." }
        else { Write-Host "Uninstalled Rested Whispers ($removed path(s) removed). No other plugin was touched." }
        Show-Status -Paths $paths
    }
    "Install" {
        $dll = Invoke-Build -Paths $paths
        [void][IO.Directory]::CreateDirectory($paths.PluginDir)
        try { Copy-Item -LiteralPath $dll -Destination (Join-Path $paths.PluginDir "RestedWhispers.dll") -Force }
        catch [System.IO.IOException] { throw "The installed DLL is still locked. Fully quit Valheim, wait a few seconds, then re-run." }
        Write-Host "Installed: $($paths.PluginDir)\RestedWhispers.dll"
        # Never leave an enabled copy and a disabled copy side by side.
        if (Remove-OwnedPath -Path $paths.DisabledDir -ExpectedLeaf $OwnedPluginLeaf) { Write-Host "Cleared stale disabled copy: $($paths.DisabledDir)" }
        Remove-EmptyDisabledParent -DisabledDir $paths.DisabledDir
        Show-Status -Paths $paths
    }
    default { Invoke-Build -Paths $paths | Out-Null }
}
