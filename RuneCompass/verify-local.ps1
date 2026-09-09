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
    RectTransforms, so it never observes that a layer receives its rotation, that the
    wind rune orbits the rim, or that anything renders. Those claims belong to the
    in-game storm protocol in STATUS.md.
.PARAMETER ValheimRoot
    Optional Valheim install path; auto-detected from Steam when omitted.
.PARAMETER Il
    Print the IL behind every binary-sourced Locked Decision, so the evidence for the
    storm predicate and the facing source can be regenerated rather than trusted.
#>
[CmdletBinding()]
param(
    [string]$ValheimRoot = "",
    [switch]$Il
)

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
# Deliberately NOT assembly_valheim.dll: this script runs under Windows PowerShell, whose
# .NET Framework runtime refuses it ("non-abstract, non-.cctor method in an interface" -- it
# uses C# 8 default interface members). Anything asserted here must therefore avoid Valheim
# types in its signature, which is why CompassSettings.ParseBiomes returns int.
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

# Source-level guard for the superseded shapes that have no compiled surface to probe.
$sourceAll = (Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.cs |
    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
foreach ($banned in @("CreateWindNeedle", "_rose", "SetNorth", "CardAmplitude",
                      "CreateNorthNeedle", "SetNeedle", "AmplitudeNeedle", "WindPointsToward",
                      "MaxDeflectionDegrees", "ClampDeflection")) {
    # IndependentLayerInterference is deliberately NOT here. It shipped as a player setting
    # rather than a comparison instrument, so it is not superseded and must not be refused.
    if ($sourceAll -match [regex]::Escape($banned)) {
        throw "$banned reappeared. It belongs to a superseded model: the rotating rose and centre-mounted wind pointer (heading-up), the separate north needle (the character arrow is the needle), or the additive deflection cap (interference is capture, not offset)."
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


# ------------------------------------------------------------- wind source ----
#
# The rune marks the quarter the wind comes FROM, so it renders at the reciprocal of the
# toward-bearing EnvMan reports. Presentation, not a contract change: GetWindDir() still
# returns a toward-vector, proven three ways in WindProvider.cs. Expected values below are
# hand-computed reciprocals, including the wraparound cases where an off-by-360 would hide.

Write-Host ""
Write-Host "=== Wind rune marks the source quarter ==="

function Get-WindSourceBearing {
    param([double]$Toward)
    return [double]$mNormalize.Invoke($null, @([single]($Toward + 180)))
}

Assert-Value "wind toward 090 -> rune at 270" (Get-WindSourceBearing 90)  270
Assert-Value "wind toward 270 -> rune at 090" (Get-WindSourceBearing 270)  90
Assert-Value "wind toward 000 -> rune at 180" (Get-WindSourceBearing 0)   180
Assert-Value "wind toward 350 -> rune at 170" (Get-WindSourceBearing 350) 170
Assert-Value "wind toward 181 -> rune at 001" (Get-WindSourceBearing 181)   1
Assert-Value "wind toward 179 -> rune at 359" (Get-WindSourceBearing 179) 359

# The reciprocal must be an involution: applying it twice returns the original bearing.
Assert-Value "reciprocal applied twice is identity" (Get-WindSourceBearing (Get-WindSourceBearing 137)) 137

# --------------------------------------------------------- storm interference ----
#
# Every expected value below is computed by hand from the documented formula, never by
# calling another project function. Failure #6 was an assertion that compared the code
# against itself and therefore could not fail.

Write-Host ""
Write-Host "=== Storm interference: wander waveform ==="

$I = $rc.GetType("WulfPack.RuneCompass.Interference")
if ($null -eq $I) { throw "Interference type not found." }
$mWander = $I.GetMethod("Wander", $flags)
$mCaptured = $I.GetMethod("Captured", $flags)
$mStormBearing = $I.GetMethod("StormBearing", $flags)
$mLurch = $I.GetMethod("Lurch", $flags)
foreach ($pair in @(@("Captured", $mCaptured), @("StormBearing", $mStormBearing), @("Lurch", $mLurch))) {
    if ($null -eq $pair[1]) { throw "Interference.$($pair[0]) not found." }
}

function Invoke-Wander { param($T, $P) return [double]$mWander.Invoke($null, @([single]$T, [single]$P)) }
function Invoke-Captured {
    param($Bearing, $Capture, $T, $P, $Turb = 1)
    return [double]$mCaptured.Invoke($null, @([single]$Bearing, [single]$Capture, [single]$T, [single]$P, [single]$Turb))
}
function Invoke-StormBearing {
    param($T, $P, $Turb = 1)
    return [double]$mStormBearing.Invoke($null, @([single]$T, [single]$P, [single]$Turb))
}
function Invoke-Lurch {
    param($T, $P, $Turb = 1)
    return [double]$mLurch.Invoke($null, @([single]$T, [single]$P, [single]$Turb))
}

# Hand-computed: at t = 0 and phase 0 every sine term is sin(0) = 0.
Assert-Value "Wander(0, 0) is zero" (Invoke-Wander 0 0) 0

# Hand-computed from 0.60*sin(0.37t) + 0.30*sin(0.91t) + 0.10*sin(2.30t) at t = 1:
#   0.60*0.36161543 + 0.30*0.78950374 + 0.10*0.74570521 = 0.52839090
Assert-Value "Wander(1, 0) matches hand value" (Invoke-Wander 1 0) 0.52839090 0.00001

$worst = 0.0
for ($k = 0; $k -lt 10000; $k++) {
    $v = [math]::Abs((Invoke-Wander ($k * 0.017) 1.3))
    if ($v -gt $worst) { $worst = $v }
}
if ($worst -le 1.0) {
    Write-Host ("  [PASS] {0,-46} = {1:N6}" -f "Wander magnitude stays within 1 (10000 pts)", $worst)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N6} exceeds 1" -f "Wander magnitude bound", $worst) -ForegroundColor Red
    $script:Failures.Add("Wander bound")
}

Write-Host ""
Write-Host "=== Storm interference: lurch ==="

# Deterministic, or nothing above is reproducible.
Assert-Value "Lurch is deterministic on repeat" (Invoke-Lurch 12.5 2.1) (Invoke-Lurch 12.5 2.1)

# Bounded by the configured span (120 deg at turbulence 1), and it must actually FIRE
# somewhere -- a lurch term that is always ~0 is not a lurch, and the sweep is what
# distinguishes "implemented" from "present".
$lurchMax = 0.0
for ($k = 0; $k -lt 10000; $k++) {
    $v = [math]::Abs((Invoke-Lurch ($k * 0.011) 0.7))
    if ($v -gt $lurchMax) { $lurchMax = $v }
}
if ($lurchMax -le 120.0001) {
    Write-Host ("  [PASS] {0,-46} = {1:N3} deg" -f "lurch stays within its 120 deg span", $lurchMax)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N3}" -f "lurch exceeds its span", $lurchMax) -ForegroundColor Red
    $script:Failures.Add("lurch bound")
}
# The envelope must return to zero at every pulse boundary. It previously began each pulse
# at full magnitude, so the term stepped discontinuously every LurchPeriod -- a jerk in the
# storm's own bearing, independent of anything the player did. Sampled either side of a
# boundary: a step here is the defect returning.
$worstBoundary = 0.0
for ($n = 1; $n -le 40; $n++) {
    $t = $n * 3.1
    $before = Invoke-Lurch ($t - 0.001) 0
    $after  = Invoke-Lurch ($t + 0.001) 0
    $step = [math]::Abs($after - $before)
    if ($step -gt $worstBoundary) { $worstBoundary = $step }
}
if ($worstBoundary -lt 2.0) {
    Write-Host ("  [PASS] {0,-46} = {1:N4} deg step" -f "lurch is continuous across pulse boundaries", $worstBoundary)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N3} deg step" -f "lurch jumps at pulse boundaries", $worstBoundary) -ForegroundColor Red
    $script:Failures.Add("lurch boundary discontinuity")
}

# And the storm bearing as a whole must be CONTINUOUS in time, which is what the compass
# shows. Note the distinction: a lurch is meant to be fast, so a large per-sample step is not
# by itself a defect. The discriminator is how the step behaves when the sample interval is
# halved -- continuous motion halves its maximum step, a genuine discontinuity does not
# shrink at all. Asserting a step threshold instead would have forbidden the kick.
function Measure-BearingStep {
    param([double]$Dt)
    $prevB = $null
    $worst = 0.0
    for ($k = 0; $k -lt 20000; $k++) {
        $cur = Invoke-StormBearing ($k * $Dt) 1.3
        if ($null -ne $prevB) {
            $d = [math]::Abs($cur - $prevB)
            if ($d -gt 180) { $d = 360 - $d }
            if ($d -gt $worst) { $worst = $d }
        }
        $prevB = $cur
    }
    return $worst
}

$coarse = Measure-BearingStep 0.008
$fine = Measure-BearingStep 0.004
$ratio = if ($fine -gt 0.0001) { $coarse / $fine } else { 0 }
if ($ratio -gt 1.6) {
    Write-Host ("  [PASS] {0,-46} = step halves with dt (ratio {1:N2})" -f "storm bearing is continuous in time", $ratio)
} else {
    Write-Host ("  [FAIL] {0,-46} = ratio {1:N2}; a step that survives halving is a jump" -f "storm bearing is discontinuous", $ratio) -ForegroundColor Red
    $script:Failures.Add("storm bearing discontinuity")
}

if ($lurchMax -gt 20.0) {
    Write-Host ("  [PASS] {0,-46} = {1:N3} deg" -f "lurch actually fires", $lurchMax)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N3} deg - never kicks" -f "lurch is inert", $lurchMax) -ForegroundColor Red
    $script:Failures.Add("lurch never fires")
}

Write-Host ""
Write-Host "=== Storm interference: capture ==="
# The guarantee behind protocol row S1: at capture 0 the true bearing is returned EXACTLY,
# for any time and seed. This is now a property of the interpolation rather than a branch.
Assert-Value "capture 0 returns bearing 137"   (Invoke-Captured 137 0 0 0)      137
Assert-Value "capture 0 at t=137.9"            (Invoke-Captured 42 0 137.9 4.7)  42
Assert-Value "capture 0 at high turbulence"    (Invoke-Captured 311 0 61.25 2.1 3) 311

# THE assertion this whole amendment exists for: at full capture the shown bearing does not
# depend on the true bearing at all. If a player could recover direction by turning, these
# three would differ.
$capA = Invoke-Captured 0   1 33.0 2.1
$capB = Invoke-Captured 90  1 33.0 2.1
$capC = Invoke-Captured 270 1 33.0 2.1
Assert-Value "full capture ignores bearing (0 vs 90)"  $capA $capB 0.0001
Assert-Value "full capture ignores bearing (0 vs 270)" $capA $capC 0.0001
Assert-Value "full capture equals the storm bearing"   $capA (Invoke-StormBearing 33.0 2.1) 0.0001

# The storm's own bearing takes time and seed only -- it has no bearing parameter to pass.
if ($mStormBearing.GetParameters().Count -eq 3) {
    Write-Host ("  [PASS] {0,-46} = time, seed, turbulence only" -f "StormBearing takes no bearing")
} else {
    Write-Host "  [FAIL] StormBearing signature changed; it may now depend on the player" -ForegroundColor Red
    $script:Failures.Add("StormBearing signature")
}

# Continuity as the player turns. LerpAngle took the shortest arc between the two bearings,
# and that arc flips direction as they pass 180 apart -- so the shown angle jumped, at EVERY
# capture level, and because the player's bearing is an input, moving triggered it. That is
# the jerkiness reported in game. A weighted vector sum has no such seam.
#
# Swept at the capture levels the compass actually spends its time at. Exactly 0.5 is
# excluded and pinned separately below: two opposing fields of equal strength genuinely
# cancel, which is physics rather than a defect, and it is a single point rather than a seam.
function Measure-CaptureStep {
    param([double]$Capture)
    $prev = $null
    $worst = 0.0
    for ($b = 0; $b -le 720; $b++) {
        $cur = Invoke-Captured ($b * 0.5) $Capture 41.0 1.7
        if ($null -ne $prev) {
            $step = [math]::Abs($cur - $prev)
            if ($step -gt 180) { $step = 360 - $step }
            if ($step -gt $worst) { $worst = $step }
        }
        $prev = $cur
    }
    return $worst
}

foreach ($c in @(0.15, 0.3, 0.45, 0.55, 0.7, 0.9)) {
    $worstStep = Measure-CaptureStep $c
    if ($worstStep -lt 12.0) {
        Write-Host ("  [PASS] {0,-46} = {1:N3} deg max step" -f "continuous while turning at capture $c", $worstStep)
    } else {
        Write-Host ("  [FAIL] {0,-46} = {1:N3} deg jump" -f "jumps while turning at capture $c", $worstStep) -ForegroundColor Red
        $script:Failures.Add("capture continuity at $c")
    }
}

# The balance point, pinned rather than hidden. At exactly equal weights the resultant of two
# opposing unit vectors is zero and has no direction; the guard must return the storm bearing
# rather than NaN or a silent "north". This is the case the general sweep deliberately skips.
$balanced = Invoke-Captured 180 0.5 41.0 1.7
$stormAt = Invoke-StormBearing 41.0 1.7
if (-not [double]::IsNaN($balanced) -and $balanced -ge 0 -and $balanced -lt 360) {
    Write-Host ("  [PASS] {0,-46} = {1:N3}" -f "balanced opposing fields stay finite", $balanced)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1}" -f "balanced opposing fields degenerate", $balanced) -ForegroundColor Red
    $script:Failures.Add("balance point")
}

# The shared/independent toggle must actually REACH the layers. It previously did not: the
# model that read it was replaced, the helper holding the read was deleted with it, and the
# setting became decoration nothing consumed. Assert the plumbing, not only the maths --
# every arithmetic assertion above passed while the toggle was inert.
$DLType = $rc.GetType("WulfPack.RuneCompass.DirectionLayer")
$stormStateType = $rc.GetType("WulfPack.RuneCompass.StormState")
if ($null -eq $stormStateType) { throw "StormState type not found." }
if ($null -eq $stormStateType.GetField("IndependentLayers")) {
    Write-Host "  [FAIL] StormState carries no IndependentLayers; the toggle cannot reach a layer" -ForegroundColor Red
    $script:Failures.Add("toggle not carried")
} else {
    Write-Host ("  [PASS] {0,-46} = carried on StormState" -f "shared/independent toggle reaches layers")
}

$pointParams = $DLType.GetMethod("Point").GetParameters()
if ($pointParams.Count -eq 2 -and $pointParams[1].ParameterType.Name -eq "StormState") {
    Write-Host ("  [PASS] {0,-46} = Point(bearing, StormState)" -f "layers receive the whole storm state")
} else {
    Write-Host "  [FAIL] DirectionLayer.Point does not take StormState; a layer may be reading settings itself" -ForegroundColor Red
    $script:Failures.Add("Point signature")
}

# Distinct seeds must separate the storm bearings, or independent mode is a no-op.
$seedA = Invoke-Captured 12 1 55.0 0
$seedB = Invoke-Captured 12 1 55.0 4.7
if ([math]::Abs($seedA - $seedB) -gt 0.5) {
    Write-Host ("  [PASS] {0,-46} = {1:N3} deg apart" -f "seeds separate storm bearings", [math]::Abs($seedA - $seedB))
} else {
    Write-Host "  [FAIL] phase seeds do not separate storm bearings; independent mode is a no-op" -ForegroundColor Red
    $script:Failures.Add("seed separation")
}


Write-Host ""
Write-Host "=== Storm interference: envelope ==="

$E = $rc.GetType("WulfPack.RuneCompass.InterferenceEnvelope")
if ($null -eq $E) { throw "InterferenceEnvelope type not found." }
$iflags = [Reflection.BindingFlags]"Public,NonPublic,Instance"
$mTick = $E.GetMethod("Tick", $iflags)
$pLevel = $E.GetProperty("Level", $iflags)
if ($null -eq $mTick -or $null -eq $pLevel) { throw "InterferenceEnvelope members not found." }

function New-Envelope { return [System.Activator]::CreateInstance($E, $true) }
function Step-Envelope {
    param($Envelope, [bool]$Storm, [double]$Dt, [double]$Attack, [double]$Release, [int]$Times = 1)
    for ($k = 0; $k -lt $Times; $k++) {
        $null = $mTick.Invoke($Envelope, @([bool]$Storm, [single]$Dt, [single]$Attack, [single]$Release))
    }
    return [double]$pLevel.GetValue($Envelope)
}

# Attack and release are separate durations now. Asserted with attack 4 and release 12, so
# a symmetric implementation would fail: the release takes three times as many steps.
$env1 = New-Envelope
Assert-Value "envelope starts clear" ([double]$pLevel.GetValue($env1)) 0
Assert-Value "attack reaches half at 2s"  (Step-Envelope $env1 $true 0.1 4.0 12.0 20) 0.5 0.0001
Assert-Value "attack reaches full at 4s"  (Step-Envelope $env1 $true 0.1 4.0 12.0 20) 1.0 0.0001
Assert-Value "attack does not exceed 1"   (Step-Envelope $env1 $true 0.1 4.0 12.0 50) 1.0 0.0001

# Release is slower: after 4s of clear weather a symmetric ramp would already be at zero,
# whereas a 12s release should still be holding two thirds of its grip.
Assert-Value "release still gripping at 4s" (Step-Envelope $env1 $false 0.1 4.0 12.0 40) 0.66667 0.001
Assert-Value "release reaches clear at 12s" (Step-Envelope $env1 $false 0.1 4.0 12.0 80) 0.0 0.0001
Assert-Value "release does not go below 0"  (Step-Envelope $env1 $false 0.1 4.0 12.0 50) 0.0 0.0001

# A storm ending mid-attack must release from where it actually reached, not from 1.
$env2 = New-Envelope
Assert-Value "mid-attack level after 1s"   (Step-Envelope $env2 $true 0.1 4.0 12.0 10) 0.25 0.0001
Assert-Value "mid-attack releases from there" (Step-Envelope $env2 $false 0.1 4.0 12.0 30) 0.0 0.0001

Write-Host ""
Write-Host "=== Config clamps and storm-set parsing ==="

$S = $rc.GetType("WulfPack.RuneCompass.CompassSettings")
if ($null -eq $S) { throw "CompassSettings type not found." }
$mClampTurb = $S.GetMethod("ClampTurbulence", $flags)
$mClampRamp = $S.GetMethod("ClampRamp", $flags)
$mClampRel = $S.GetMethod("ClampRelease", $flags)
if ($null -eq $mClampRel) { throw "CompassSettings.ClampRelease not found." }
$mParse = $S.GetMethod("ParseNames", $flags)
$mClampAmp = $S.GetMethod("ClampAmplitude", $flags)
if ($null -eq $mClampAmp) { throw "CompassSettings.ClampAmplitude not found." }
if ($null -eq $mClampTurb -or $null -eq $mClampRamp -or $null -eq $mParse) {
    throw "CompassSettings clamp/parse methods not found."
}

# A sanity bound only. The degree cap this replaced was argued twice and retired twice --
# first by fixing the card, then by the operator asking for exactly the spinning it forbade.
Assert-Value "turbulence 1 passes through" ([double]$mClampTurb.Invoke($null, @([single]1))) 1
Assert-Value "turbulence 9 clamps to 3"    ([double]$mClampTurb.Invoke($null, @([single]9))) 3
Assert-Value "turbulence -1 clamps to 0"   ([double]$mClampTurb.Invoke($null, @([single](-1)))) 0

# The floor is what keeps the predicate's ~2s lead over the visible sky imperceptible.
# A faster ramp turns the compass into a storm early-warning device.
Assert-Value "attack 4.0 passes through" ([double]$mClampRamp.Invoke($null, @([single]4.0))) 4.0
Assert-Value "attack 0.5 clamps to 3"  ([double]$mClampRamp.Invoke($null, @([single]0.5))) 3.0
Assert-Value "attack 10 passes through" ([double]$mClampRamp.Invoke($null, @([single]10))) 10.0

# The release has no information-leak constraint, so its floor is sanity only and much lower.
# A shared clamp would have forced the release up to the attack's floor for no reason.
Assert-Value "release 12 passes through" ([double]$mClampRel.Invoke($null, @([single]12))) 12.0
Assert-Value "release 0.1 clamps to 0.5" ([double]$mClampRel.Invoke($null, @([single]0.1))) 0.5
Assert-Value "release 1 passes through"  ([double]$mClampRel.Invoke($null, @([single]1))) 1.0

# The storm set is hand-edited, so parsing must forgive spacing and empty entries.
$parsed = $mParse.Invoke($null, @([string]" ThunderStorm , ,SnowStorm "))
Assert-Value "ParseNames drops blank entries" ([int]$parsed.Length) 2
Assert-Value "ParseNames trims entry 0" ([string]$parsed[0]) "ThunderStorm"
Assert-Value "ParseNames trims entry 1" ([string]$parsed[1]) "SnowStorm"
Assert-Value "ParseNames on blank input" ([int]($mParse.Invoke($null, @([string]"  "))).Length) 0

# Biomes are the second storm axis. Heightmap.Biome is a flags enum, so a list of names
# collapses to one mask: Mistlands 512, Swamp 2, Mountain 4. Hand-computed sums below.
$mParseBiomes = $S.GetMethod("ParseBiomes", $flags)
if ($null -eq $mParseBiomes) { throw "CompassSettings.ParseBiomes not found." }
function Invoke-ParseBiomes { param([string]$Csv) return [int]$mParseBiomes.Invoke($null, @([string]$Csv)) }

Assert-Value "ParseBiomes Mistlands"          (Invoke-ParseBiomes "Mistlands") 512
Assert-Value "ParseBiomes is case-insensitive" (Invoke-ParseBiomes "mistlands") 512
Assert-Value "ParseBiomes ORs a list"          (Invoke-ParseBiomes "Mistlands, Swamp, Mountain") 518
Assert-Value "ParseBiomes on blank is None"    (Invoke-ParseBiomes "  ") 0

# A typo must cost that one biome, not the whole feature: this file is hand-edited.
Assert-Value "ParseBiomes skips an unknown name" (Invoke-ParseBiomes "Mistlands, Narnia") 512
Assert-Value "ParseBiomes of only garbage is None" (Invoke-ParseBiomes "Narnia") 0

# The mod holds its own copy of the biome bits, because a method naming a Valheim type
# cannot be invoked under this runtime at all. A copy is only safe if it is checked, so read
# the REAL enum out of the installed assembly with Mono.Cecil -- which reads metadata rather
# than loading it -- and fail on any disagreement.
$cecilPath = Join-Path (Split-Path (Split-Path $managed -Parent) -Parent) "BepInEx\core\Mono.Cecil.dll"
if (Test-Path -LiteralPath $cecilPath) {
    Add-Type -Path $cecilPath
    $va = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "assembly_valheim.dll"))
    $biomeEnum = $null
    foreach ($t in $va.MainModule.Types) {
        foreach ($n in $t.NestedTypes) { if ($n.FullName -eq "Heightmap/Biome") { $biomeEnum = $n } }
    }
    if ($null -eq $biomeEnum) { throw "Heightmap/Biome enum not found in the installed assembly." }
    $gameBits = @{}
    foreach ($f in $biomeEnum.Fields) { if ($f.HasConstant) { $gameBits[$f.Name] = [int]$f.Constant } }

    $expected = @{ Meadows = 1; Swamp = 2; Mountain = 4; BlackForest = 8; Plains = 16;
                   AshLands = 32; DeepNorth = 64; Ocean = 256; Mistlands = 512 }
    $drift = @()
    foreach ($k in $expected.Keys) {
        if (-not $gameBits.ContainsKey($k)) { $drift += "$k absent from the game enum" }
        elseif ($gameBits[$k] -ne $expected[$k]) { $drift += "$k is $($gameBits[$k]) in game, $($expected[$k]) in the mod" }
    }
    if ($drift.Count -eq 0) {
        Write-Host ("  [PASS] {0,-46} = {1} names match the game enum" -f "biome table agrees with assembly_valheim", $expected.Count)
    } else {
        Write-Host ("  [FAIL] biome table has drifted: {0}" -f ($drift -join "; ")) -ForegroundColor Red
        $script:Failures.Add("biome table drift")
    }
} else {
    Write-Host "  [FAIL] Mono.Cecil not found; the biome table cannot be checked against the game" -ForegroundColor Red
    $script:Failures.Add("biome table unverified")
}

# Wind immunity as a structural fact rather than a comment. This assertion was retargeted
# when the other amplitudes became live-tunable: it used to read a CompassUI constant, and
# that constant no longer carries the invariant. Testing the thing that used to hold a
# property, after the property moved, is how a check silently stops checking anything.
$DL = $rc.GetType("WulfPack.RuneCompass.DirectionLayer")
if ($null -eq $DL) { throw "DirectionLayer type not found." }
$immune = $DL.GetField("Immune", $flags)
if ($null -eq $immune) { throw "DirectionLayer.Immune not found - wind immunity has no carrier." }
$immuneFn = $immune.GetValue($null)
Assert-Value "DirectionLayer.Immune returns zero" ([double]$immuneFn.Invoke()) 0

# And no config key may exist that could reach it.
$cfgSrc = Get-Content -LiteralPath (Join-Path $PSScriptRoot "CompassConfig.cs") -Raw
if ($cfgSrc -match "AmplitudeWind") {
    Write-Host "  [FAIL] an AmplitudeWind config key exists; wind immunity is no longer structural" -ForegroundColor Red
    $script:Failures.Add("wind amplitude is configurable")
} else {
    Write-Host ("  [PASS] {0,-46} = absent" -f "no AmplitudeWind config key")
}

# Per-layer amplitude clamp: no config value can drive a layer past full deflection.
Assert-Value "amplitude 0.85 passes through" ([double]$mClampAmp.Invoke($null, @([single]0.85))) 0.85
Assert-Value "amplitude 1.4 clamps to 1"     ([double]$mClampAmp.Invoke($null, @([single]1.4))) 1
Assert-Value "amplitude -0.2 clamps to 0"    ([double]$mClampAmp.Invoke($null, @([single](-0.2)))) 0

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


# --------------------------------------------------------------------- il ----
#
# The storm predicate and the facing source were derived from IL in the installed game
# assembly, which no grep can re-execute. This regenerates that evidence on demand so the
# reasoning behind those decisions can be rechecked rather than taken on trust.

if ($Il) {
    Write-Host ""
    Write-Host "=== Locked Decision evidence (IL from the installed assembly) ==="
    $cecil = Join-Path (Split-Path $managed -Parent | Split-Path -Parent) "BepInEx\core\Mono.Cecil.dll"
    if (-not (Test-Path -LiteralPath $cecil)) {
        Write-Host "  Mono.Cecil.dll not found under BepInEx/core; skipping." -ForegroundColor Yellow
    } else {
        Add-Type -Path $cecil
        $va = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "assembly_valheim.dll"))
        $targets = @(
            @("EnvMan", "InterpolateEnvironment", "predicate adopts the INCOMING name at blend 0"),
            @("EnvMan", "IsEnvironment", "the game's own weather-identity idiom"),
            @("EnvMan", "GetWindIntensity", "rejected as a storm gate"),
            @("Character", "GetLookDir", "eye forward - tracks the camera, NOT body facing"),
            @("Character", "GetLookYaw", "look yaw - also camera-tracking")
        )
        foreach ($t in $targets) {
            $type = $va.MainModule.Types | Where-Object { $_.FullName -eq $t[0] }
            if ($null -eq $type) { continue }
            foreach ($m in $type.Methods) {
                if ($m.Name -ne $t[1] -or -not $m.HasBody) { continue }
                Write-Host ""
                Write-Host ("--- {0}::{1}  [{2}] ---" -f $t[0], $t[1], $t[2])
                $n = 0
                foreach ($ins in $m.Body.Instructions) {
                    Write-Host ("    {0}" -f $ins.ToString())
                    $n++
                    if ($n -ge 24) { Write-Host "    ... (truncated)"; break }
                }
            }
        }
    }
}

# ------------------------------------------------------------ skins index ----
#
# SKINS_INDEX.md is generated by measurement. This refuses to pass if it has drifted from
# the filesystem, because an index nobody re-measures is exactly SHADOW_GENOME Failure #7 --
# a gate that had been reporting numbers it never read.

Write-Host ""
Write-Host "=== Skins index ==="

$skinsDir = Join-Path $PSScriptRoot "Assets\Skins"
$indexPath = Join-Path $skinsDir "SKINS_INDEX.md"
if (-not (Test-Path -LiteralPath $indexPath)) {
    Write-Host "  [FAIL] SKINS_INDEX.md is missing" -ForegroundColor Red
    $script:Failures.Add("skins index missing")
} else {
    $live = @("base.png", "ring.png", "character_arrow.png", "camera_wedge.png", "wind_gust.png")
    $skinDirs = Get-ChildItem -LiteralPath $skinsDir -Directory | Sort-Object Name
    $measuredPresent = 0
    $measuredMissing = 0
    foreach ($d in $skinDirs) {
        foreach ($a in $live) {
            if (Test-Path -LiteralPath (Join-Path $d.FullName $a)) { $measuredPresent++ }
            else { $measuredMissing++ }
        }
    }
    $indexText = Get-Content -LiteralPath $indexPath -Raw
    $claimedPresent = if ($indexText -match "(?m)^- Present: \*\*(\d+)\*\*") { [int]$Matches[1] } else { -1 }
    $claimedMissing = if ($indexText -match "(?m)^- Missing: \*\*(\d+)\*\*") { [int]$Matches[1] } else { -1 }
    $claimedSkins = if ($indexText -match "(?m)^- Skins: \*\*(\d+)\*\*") { [int]$Matches[1] } else { -1 }

    Assert-Value "index skin count matches disk"    $claimedSkins   $skinDirs.Count
    Assert-Value "index present count matches disk" $claimedPresent $measuredPresent
    Assert-Value "index missing count matches disk" $claimedMissing $measuredMissing
    Write-Host ("         regenerate with: python Assets\Skins\generate-index.py")
}

Write-Host ""
if ($script:Failures.Count -eq 0) {
    Write-Host "RESULT: ALL CHECKS PASSED" -ForegroundColor Green
    exit 0
}
Write-Host ("RESULT: {0} FAILURE(S): {1}" -f $script:Failures.Count, ($script:Failures -join ', ')) -ForegroundColor Red
exit 1
