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
$mRoseZ      = $B.GetMethod("RoseRotationZ", $flags)
$mWindZ      = $B.GetMethod("WindRotationZ", $flags)
foreach ($pair in @(@("TryBearingFromDirection", $mTryBearing), @("Cardinal", $mCardinal),
                    @("Normalize", $mNormalize), @("RoseRotationZ", $mRoseZ), @("WindRotationZ", $mWindZ))) {
    if ($null -eq $pair[1]) { throw "Bearing.$($pair[0]) not found." }
}
if ($null -ne $B.GetMethod("Relative", $flags)) {
    throw "Bearing.Relative exists. It was removed as dead code (audit finding F-T2) and must not return."
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

Write-Host "--- rows 6-10: rose rotation, pinned term by term ---"
# Rows 9 and 10 are load-bearing. Row 9 rejects the compensating-piecewise family
# (Rose(h)=h-90 above 180) that passed an earlier revision while rendering the card
# 90 degrees wrong. Row 10 rejects both 8-point snapping (37.5 -> 45) and smooth
# periodic error (sin vanishes at every multiple of 90, but not at 37.5).
Assert-Value "RoseRotationZ(0)"      (Invoke-F $mRoseZ 0)      0
Assert-Value "RoseRotationZ(90)"     (Invoke-F $mRoseZ 90)     90
Assert-Value "RoseRotationZ(180)"    (Invoke-F $mRoseZ 180)    180
Assert-Value "RoseRotationZ(270)"    (Invoke-F $mRoseZ 270)    270
Assert-Value "RoseRotationZ(37.5)"   (Invoke-F $mRoseZ 37.5)   37.5

Write-Host "--- rows 11-14: wind rotation, pinned term by term ---"
Assert-Value "WindRotationZ(0)"      (Invoke-F $mWindZ 0)      0
Assert-Value "WindRotationZ(90)"     (Invoke-F $mWindZ 90)     -90
Assert-Value "WindRotationZ(135)"    (Invoke-F $mWindZ 135)    -135
Assert-Value "WindRotationZ(212.5)"  (Invoke-F $mWindZ 212.5)  -212.5

Write-Host "--- row 15: derivation record (adds no detection; terms already pinned) ---"
$composed = [double]$mNormalize.Invoke($null, @([single](-((Invoke-F $mRoseZ 270) + (Invoke-F $mWindZ 135)))))
Assert-Value "screen CW angle, heading 270 / wind 135" $composed 225

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
