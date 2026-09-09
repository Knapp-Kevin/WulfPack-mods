<#
.SYNOPSIS
    Local verification for Rune Compass: angle conventions + Section 4 Razor limits.
.DESCRIPTION
    This repository uses zero GitHub Actions and has no automated test framework.
    This script is the local stand-in: it loads the COMPILED RuneCompass.dll and
    invokes the real methods by reflection, comparing every result against a value
    computed by hand.

    Every expected value below is a literal. No assertion compares one project
    function against another -- an earlier revision did exactly that and the
    resulting identity could not fail, admitting an implementation family that
    rendered the compass upside down (SHADOW_GENOME Failure #6).

    What this CANNOT verify: it cannot construct Unity GameObjects or
    RectTransforms, so it never observes that _rose receives its rotation, that the
    wind needle is parented to _rose, or that anything renders. Those claims belong
    to the in-game checklist in STATUS.md.
.PARAMETER ValheimRoot
    Optional Valheim install path; auto-detected from Steam when omitted.
#>
[CmdletBinding()]
param([string]$ValheimRoot = "")

$ErrorActionPreference = "Stop"

$script:Failures = New-Object System.Collections.Generic.List[string]

function Assert-Value {
    param([string]$Name, $Actual, $Expected, [double]$Tolerance = 0.0001)
    $ok = $false
    if ($Expected -is [string] -or $Expected -is [bool]) { $ok = ($Actual -eq $Expected) }
    else { $ok = ([math]::Abs([double]$Actual - [double]$Expected) -le $Tolerance) }
    if ($ok) {
        Write-Host ("  [PASS] {0,-46} = {1}" -f $Name, $Actual)
    } else {
        Write-Host ("  [FAIL] {0,-46} = {1}  (expected {2})" -f $Name, $Actual, $Expected) -ForegroundColor Red
        $script:Failures.Add($Name)
    }
}

function Get-SteamRoot {
    foreach ($key in @("HKCU:\SOFTWARE\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam")) {
        try { $p = Get-ItemProperty -Path $key -ErrorAction Stop } catch { continue }
        foreach ($name in @("SteamPath", "InstallPath")) {
            $value = $p.$name
            if ($value -and (Test-Path -LiteralPath $value)) { return ([string]$value).Replace("/", "\") }
        }
    }
    return $null
}

function Find-Managed {
    param([string]$Root)
    if (-not [string]::IsNullOrWhiteSpace($Root)) { return (Join-Path $Root "valheim_Data\Managed") }
    $candidates = New-Object System.Collections.Generic.List[string]
    $steam = Get-SteamRoot
    if ($steam) {
        $candidates.Add((Join-Path $steam "steamapps\common\Valheim"))
        $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
        if (Test-Path -LiteralPath $vdf) {
            foreach ($m in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $candidates.Add((Join-Path ($m.Groups[1].Value -replace '\\\\', '\') "steamapps\common\Valheim"))
            }
        }
    }
    foreach ($c in $candidates) {
        $managed = Join-Path $c "valheim_Data\Managed"
        if (Test-Path -LiteralPath (Join-Path $managed "UnityEngine.CoreModule.dll")) { return $managed }
    }
    throw "Valheim installation not found. Pass -ValheimRoot 'C:\path\to\Valheim'."
}

# ---------------------------------------------------------------- bearings ----

$managed = Find-Managed -Root $ValheimRoot
$dll = Join-Path $PSScriptRoot "bin\Release\netstandard2.1\RuneCompass.dll"
if (-not (Test-Path -LiteralPath $dll)) {
    throw "RuneCompass.dll not found. Run .\RuneCompass\build-local.ps1 first."
}

[void][Reflection.Assembly]::LoadFrom((Join-Path $managed "UnityEngine.CoreModule.dll"))
$rc = [Reflection.Assembly]::LoadFrom($dll)

$V3 = [Reflection.Assembly]::LoadFrom((Join-Path $managed "UnityEngine.CoreModule.dll")).GetType("UnityEngine.Vector3")
$V3Ctor = $V3.GetConstructor(@([single], [single], [single]))
function New-V3 { param($x, $y, $z) $V3Ctor.Invoke(@([single]$x, [single]$y, [single]$z)) }

$flags = [Reflection.BindingFlags]"Public,NonPublic,Static"
$B = $rc.GetType("WulfPack.RuneCompass.Bearing")
if ($null -eq $B) { throw "Type WulfPack.RuneCompass.Bearing not found in $dll." }
$mTryBearing = $B.GetMethod("TryBearingFromDirection", $flags)
$mCardinal   = $B.GetMethod("Cardinal", $flags)
$mNormalize  = $B.GetMethod("Normalize", $flags)
$mBearingZ   = $B.GetMethod("BearingRotationZ", $flags)
foreach ($pair in @(@("TryBearingFromDirection", $mTryBearing), @("Cardinal", $mCardinal),
                    @("Normalize", $mNormalize), @("BearingRotationZ", $mBearingZ))) {
    if ($null -eq $pair[1]) { throw "Bearing.$($pair[0]) not found." }
}
foreach ($gone in @("Relative", "RoseRotationZ", "WindRotationZ")) {
    if ($null -ne $B.GetMethod($gone, $flags)) {
        throw "Bearing.$gone exists. North-up uses a single BearingRotationZ mapping; the heading-up pair and the dead Relative helper must not return."
    }
}

function Get-Bearing {
    param($x, $y, $z)
    $args = [object[]]@((New-V3 $x $y $z), $null)
    $ok = $mTryBearing.Invoke($null, $args)
    return [pscustomobject]@{ Ok = [bool]$ok; Degrees = [double]$args[1] }
}
function Invoke-F { param($Method, $Value) return [double]$Method.Invoke($null, @([single]$Value)) }

Write-Host ""
Write-Host "=== Rune Compass: angle conventions ==="
Write-Host "--- rows 1-2: bearing convention (matches Valheim Utils.YawFromDirection) ---"
Assert-Value "TryBearingFromDirection(+Z) north"  (Get-Bearing 0 0 1).Degrees    0
Assert-Value "TryBearingFromDirection(+X) east"   (Get-Bearing 1 0 0).Degrees    90
Assert-Value "TryBearingFromDirection(-Z) south"  (Get-Bearing 0 0 -1).Degrees   180
Assert-Value "TryBearingFromDirection(-X) west"   (Get-Bearing -1 0 0).Degrees   270
Assert-Value "TryBearingFromDirection(+X+Z) NE"   (Get-Bearing 1 0 1).Degrees    45
Assert-Value "TryBearingFromDirection(+X-Z) SE"   (Get-Bearing 1 0 -1).Degrees   135
Assert-Value "TryBearingFromDirection(-X-Z) SW"   (Get-Bearing -1 0 -1).Degrees  225
Assert-Value "TryBearingFromDirection(-X+Z) NW"   (Get-Bearing -1 0 1).Degrees   315

Write-Host "--- rows 3-4: pitch invariance and degenerate rejection ---"
Assert-Value "steep pitch (0,-9,1) still north"   (Get-Bearing 0 -9 1).Degrees   0
Assert-Value "straight up (0,5,0) is rejected"    (Get-Bearing 0 5 0).Ok         $false

Write-Host "--- row 5: 8-point boundary rounding ---"
Assert-Value "Cardinal(22.4)"    ($mCardinal.Invoke($null, @([single]22.4)))    "N"
Assert-Value "Cardinal(22.6)"    ($mCardinal.Invoke($null, @([single]22.6)))    "NE"
Assert-Value "Cardinal(337.6)"   ($mCardinal.Invoke($null, @([single]337.6)))   "N"
Assert-Value "Cardinal(359.9)"   ($mCardinal.Invoke($null, @([single]359.9)))   "N"

Write-Host "--- rows 6-11: bearing rotation, pinned term by term ---"
# North-up: N is fixed at 12 o'clock, so a world bearing is an absolute screen position and
# every indicator shares one mapping, z = -bearing. Rows at 37.5 and 212.5 are load-bearing:
# they are not multiples of 45, so they reject 8-point snapping and any smooth periodic
# error whose terms vanish at the quadrant points.
Assert-Value "BearingRotationZ(0)"      (Invoke-F $mBearingZ 0)      0
Assert-Value "BearingRotationZ(90)"     (Invoke-F $mBearingZ 90)     -90
Assert-Value "BearingRotationZ(180)"    (Invoke-F $mBearingZ 180)    -180
Assert-Value "BearingRotationZ(270)"    (Invoke-F $mBearingZ 270)    -270
Assert-Value "BearingRotationZ(37.5)"   (Invoke-F $mBearingZ 37.5)   -37.5
Assert-Value "BearingRotationZ(212.5)"  (Invoke-F $mBearingZ 212.5)  -212.5

Write-Host "--- row 12: derivation record (terms already pinned above) ---"
# An indicator handed bearing b must appear at clockwise b from screen-up, because the
# card is fixed to world north. Hand-computed: bearing 135 renders down-right at 135.
$screenCw = [double]$mNormalize.Invoke($null, @([single](-(Invoke-F $mBearingZ 135))))
Assert-Value "screen CW angle for bearing 135" $screenCw 135

# ------------------------------------------------------------------ skins ----

# A skin bundle is data, and data rots quietly: a renamed file, a typo'd manifest key or a
# wrong-sized canvas all produce a silently unskinned or misaligned compass at runtime
# rather than an error. The loader falls back rather than throwing, which is right for
# players and useless for catching mistakes, so the bundles are checked here instead.
Write-Host ""
Write-Host "=== Skin bundles ==="

$skinsRoot = Join-Path $PSScriptRoot "Assets\Skins"
$skinDirs = @()
if (Test-Path -LiteralPath $skinsRoot) {
    $skinDirs = Get-ChildItem -LiteralPath $skinsRoot -Directory | Sort-Object Name
}

if ($skinDirs.Count -eq 0) {
    Write-Host "  [FAIL] no skin folders found under Assets\Skins" -ForegroundColor Red
    $script:Failures.Add("no skin folders")
}
else {
    Write-Host ("  found {0} skin bundle(s)" -f $skinDirs.Count)
}

# The loader treats these two as mandatory; the rest are optional with a fallback.
$requiredKeys = @("ringTexture", "windPointerTexture")

foreach ($dir in $skinDirs) {
    $name = $dir.Name
    $manifestPath = Join-Path $dir.FullName "skin.json"
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        Assert-Value "$name has skin.json" $false $true
        continue
    }

    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json }
    catch {
        Write-Host ("  [FAIL] {0}: skin.json is not valid JSON" -f $name) -ForegroundColor Red
        $script:Failures.Add("$name skin.json parse")
        continue
    }

    Assert-Value "$name declares a name"        ([bool]$manifest.name)                 $true
    Assert-Value "$name defaultScale > 0"       ($manifest.defaultScale -gt 0)          $true

    foreach ($key in $requiredKeys) {
        Assert-Value "$name declares $key" ([bool]$manifest.$key) $true
    }

    # Every declared texture must exist and be a centred 512x512 RGBA canvas.
    foreach ($key in @("baseTexture","ringTexture","headingPointerTexture","windPointerTexture","lubberMarkerTexture")) {
        $file = $manifest.$key
        if (-not $file) { continue }
        $path = Join-Path $dir.FullName $file
        if (-not (Test-Path -LiteralPath $path)) {
            Write-Host ("  [FAIL] {0}: {1} -> {2} is missing" -f $name, $key, $file) -ForegroundColor Red
            $script:Failures.Add("$name $key missing")
            continue
        }
        $bytes = [IO.File]::ReadAllBytes($path)
        if ($bytes.Length -lt 26 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47) {
            Write-Host ("  [FAIL] {0}: {1} is not a PNG" -f $name, $file) -ForegroundColor Red
            $script:Failures.Add("$name $file not png"); continue
        }
        $w = [BitConverter]::ToUInt32(($bytes[16..19])[3..0], 0)
        $h = [BitConverter]::ToUInt32(($bytes[20..23])[3..0], 0)
        $colourType = $bytes[25]
        if ($w -ne 512 -or $h -ne 512) {
            Write-Host ("  [FAIL] {0}: {1} is {2}x{3}, expected 512x512" -f $name, $file, $w, $h) -ForegroundColor Red
            $script:Failures.Add("$name $file size")
        }
        else {
            # What matters is an alpha channel, not a specific encoding: a layer without one
            # renders opaque corners over the world. RGBA (6) and grey+alpha (4) carry alpha
            # directly; indexed colour (3) carries it in a tRNS chunk. Anything else does not.
            $hasTrns = $false
            $head = [Text.Encoding]::ASCII.GetString($bytes[0..([Math]::Min(4095, $bytes.Length - 1))])
            if ($head -match 'tRNS') { $hasTrns = $true }
            $hasAlpha = ($colourType -eq 6) -or ($colourType -eq 4) -or ($colourType -eq 3 -and $hasTrns)
            if (-not $hasAlpha) {
                Write-Host ("  [FAIL] {0}: {1} has no alpha channel (colour type {2}); corners would render opaque" -f $name, $file, $colourType) -ForegroundColor Red
                $script:Failures.Add("$name $file no alpha")
            }
        }
    }

    $heading = if ($manifest.headingPointerTexture) { "own heading art" } else { "primitive heading fallback" }
    Write-Host ("  [PASS] {0,-14} scale {1,-5} {2}" -f $name, $manifest.defaultScale, $heading)
}

# ------------------------------------------------------------------ razor ----

Write-Host ""
Write-Host "=== Section 4 Razor (limits: method 40 whole lines, file 250) ==="

$maxMethod = 40
$maxFile = 250
foreach ($file in (Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.cs | Sort-Object Name)) {
    $lines = Get-Content -LiteralPath $file.FullName
    if ($lines.Count -gt $maxFile) {
        Write-Host ("  [FAIL] {0,-26} file = {1} lines (limit {2})" -f $file.Name, $lines.Count, $maxFile) -ForegroundColor Red
        $script:Failures.Add("$($file.Name) file length")
    } else {
        Write-Host ("  [PASS] {0,-26} file = {1} lines" -f $file.Name, $lines.Count)
    }

    # Whole-line method measurement: signature line through its matching close brace.
    $worstName = ""; $worstLen = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^\s{4}(public|private|internal|protected|static)') { continue }
        if ($lines[$i] -match ';\s*$' -or $lines[$i] -match '=>') { continue }
        if ($lines[$i] -notmatch '\)\s*$') { continue }
        if ($i + 1 -ge $lines.Count -or $lines[$i + 1].Trim() -ne '{') { continue }
        $depth = 0; $j = $i + 1
        while ($j -lt $lines.Count) {
            $depth += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($depth -eq 0) { break }
            $j++
        }
        $len = $j - $i + 1
        if ($len -gt $worstLen) { $worstLen = $len; $worstName = $lines[$i].Trim() }
        $i = $j
    }
    if ($worstLen -gt $maxMethod) {
        Write-Host ("         [FAIL] longest method = {0} lines (limit {1}): {2}" -f $worstLen, $maxMethod, $worstName) -ForegroundColor Red
        $script:Failures.Add("$($file.Name) method length")
    } elseif ($worstLen -gt 0) {
        Write-Host ("         longest method = {0} lines" -f $worstLen)
    }
}

Write-Host ""
if ($script:Failures.Count -eq 0) {
    Write-Host "RESULT: ALL CHECKS PASSED" -ForegroundColor Green
    exit 0
}
Write-Host ("RESULT: {0} FAILURE(S): {1}" -f $script:Failures.Count, ($script:Failures -join ', ')) -ForegroundColor Red
exit 1
