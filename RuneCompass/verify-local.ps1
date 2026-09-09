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
.PARAMETER Seal
    Additionally fail while the IndependentLayerInterference comparison toggle survives
    anywhere in the source. /qor-substantiate runs this; a seal cannot pass until the
    comparison has been judged in a live storm and the losing branch deleted.
.PARAMETER Il
    Print the IL behind every binary-sourced Locked Decision, so the evidence for the
    storm predicate and the facing source can be regenerated rather than trusted.
#>
[CmdletBinding()]
param(
    [string]$ValheimRoot = "",
    [switch]$Seal,
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
foreach ($banned in @("CreateWindNeedle", "_rose")) {
    if ($sourceAll -match [regex]::Escape($banned)) {
        throw "$banned reappeared. The centre-mounted wind pointer and the rotating rose belong to the superseded heading-up model."
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
$mDeflect = $I.GetMethod("Deflection", $flags)
if ($null -eq $mWander -or $null -eq $mDeflect) { throw "Interference methods not found." }

function Invoke-Wander { param($T, $P) return [double]$mWander.Invoke($null, @([single]$T, [single]$P)) }
function Invoke-Deflect {
    param($Level, $Max, $T, $P)
    return [double]$mDeflect.Invoke($null, @([single]$Level, [single]$Max, [single]$T, [single]$P))
}

# Hand-computed: at t = 0 and phase 0 every sine term is sin(0) = 0.
Assert-Value "Wander(0, 0) is zero" (Invoke-Wander 0 0) 0

# Hand-computed from 0.60*sin(0.37t+p) + 0.30*sin(0.91t+p) + 0.10*sin(2.30t+p).
# t = 1, p = 0:  0.60*sin(0.37) + 0.30*sin(0.91) + 0.10*sin(2.30)
#             =  0.60*0.36161543 + 0.30*0.78950374 + 0.10*0.74570521
#             =  0.21696926 + 0.23685112 + 0.07457052 = 0.52839090
Assert-Value "Wander(1, 0) matches hand value" (Invoke-Wander 1 0) 0.52839090 0.00001

# Bounded by construction: the weights sum to exactly 1, so the magnitude never exceeds 1.
# Swept densely rather than argued, because that bound is what makes MaxDeflectionDegrees
# mean what its name says.
$worst = 0.0
for ($k = 0; $k -lt 10000; $k++) {
    $t = $k * 0.017
    $v = [math]::Abs((Invoke-Wander $t 1.3))
    if ($v -gt $worst) { $worst = $v }
}
if ($worst -le 1.0) {
    Write-Host ("  [PASS] {0,-46} = {1:N6}" -f "Wander magnitude stays within 1 (10000 pts)", $worst)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N6} exceeds 1" -f "Wander magnitude bound", $worst) -ForegroundColor Red
    $script:Failures.Add("Wander bound")
}

# Deterministic: the same (t, seed) must give the same answer, or nothing above repeats.
Assert-Value "Wander is deterministic on repeat" (Invoke-Wander 12.5 2.1) (Invoke-Wander 12.5 2.1)

# Distinct seeds must actually separate the layers, or independent mode is a no-op.
$spread = [math]::Abs((Invoke-Wander 7.0 0) - (Invoke-Wander 7.0 2.1))
if ($spread -gt 0.01) {
    Write-Host ("  [PASS] {0,-46} = {1:N6}" -f "distinct seeds separate layers at t=7", $spread)
} else {
    Write-Host ("  [FAIL] {0,-46} = {1:N6}" -f "distinct seeds do not separate layers", $spread) -ForegroundColor Red
    $script:Failures.Add("phase seed separation")
}

Write-Host ""
Write-Host "=== Storm interference: calm-weather regression ==="
# The guarantee behind protocol row S1. At envelope level 0 the interference term must
# contribute exactly nothing, whatever the time or seed, so clear weather renders every
# layer on its true bearing. This is the deflection term only; the calm HUD's appearance
# changed by design when wind moved out to the rim.
Assert-Value "Deflection at level 0 (t=0)"      (Invoke-Deflect 0 22 0 0)       0
Assert-Value "Deflection at level 0 (t=137.9)"  (Invoke-Deflect 0 22 137.9 4.7) 0
Assert-Value "Deflection at level 0, max 35"    (Invoke-Deflect 0 35 61.25 2.1) 0

# Hand-computed: 0.5 * 22 * Wander(1,0) = 11 * 0.52839090 = 5.81229990
Assert-Value "Deflection is level x max x wander" (Invoke-Deflect 0.5 22 1 0) 5.81229990 0.00001

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
    param($Envelope, [bool]$Storm, [double]$Dt, [double]$Ramp, [int]$Times = 1)
    for ($k = 0; $k -lt $Times; $k++) {
        $null = $mTick.Invoke($Envelope, @([bool]$Storm, [single]$Dt, [single]$Ramp))
    }
    return [double]$pLevel.GetValue($Envelope)
}

# Rises to exactly 1 after rampSeconds of storm, and no further.
$env1 = New-Envelope
Assert-Value "envelope starts clear" ([double]$pLevel.GetValue($env1)) 0
Assert-Value "envelope at half ramp"  (Step-Envelope $env1 $true 0.1 4.0 20) 0.5 0.0001
Assert-Value "envelope at full ramp"  (Step-Envelope $env1 $true 0.1 4.0 20) 1.0 0.0001
Assert-Value "envelope does not exceed 1" (Step-Envelope $env1 $true 0.1 4.0 50) 1.0 0.0001

# Falls symmetrically and settles at exactly 0: protocol row S5 expects no residual offset.
Assert-Value "envelope releases to half"  (Step-Envelope $env1 $false 0.1 4.0 20) 0.5 0.0001
Assert-Value "envelope releases to clear" (Step-Envelope $env1 $false 0.1 4.0 20) 0.0 0.0001
Assert-Value "envelope does not go below 0" (Step-Envelope $env1 $false 0.1 4.0 50) 0.0 0.0001

# A storm ending mid-attack must release from where it actually reached, not from 1.
# One second into a four-second attack the level is 0.25, and one second of release clears it.
$env2 = New-Envelope
Assert-Value "mid-attack level after 1s"      (Step-Envelope $env2 $true 0.1 4.0 10) 0.25 0.0001
Assert-Value "mid-attack release is symmetric" (Step-Envelope $env2 $false 0.1 4.0 10) 0.0 0.0001

Write-Host ""
Write-Host "=== Config clamps and storm-set parsing ==="

$S = $rc.GetType("WulfPack.RuneCompass.CompassSettings")
if ($null -eq $S) { throw "CompassSettings type not found." }
$mClampDef = $S.GetMethod("ClampDeflection", $flags)
$mClampRamp = $S.GetMethod("ClampRamp", $flags)
$mParse = $S.GetMethod("ParseNames", $flags)
if ($null -eq $mClampDef -or $null -eq $mClampRamp -or $null -eq $mParse) {
    throw "CompassSettings clamp/parse methods not found."
}

# The ceiling is what stops an operator reviving the inverted-glyph defect the north-up
# dial was adopted to fix. A default alone would not: the value is live-reloaded from a
# hand-edited file.
Assert-Value "deflection 22 passes through" ([double]$mClampDef.Invoke($null, @([single]22))) 22
Assert-Value "deflection 180 clamps to 35"  ([double]$mClampDef.Invoke($null, @([single]180))) 35
Assert-Value "deflection -5 clamps to 0"    ([double]$mClampDef.Invoke($null, @([single](-5)))) 0

# The floor is what keeps the predicate's ~2s lead over the visible sky imperceptible.
# A faster ramp turns the compass into a storm early-warning device.
Assert-Value "ramp 4.0 passes through" ([double]$mClampRamp.Invoke($null, @([single]4.0))) 4.0
Assert-Value "ramp 0.5 clamps to 3"    ([double]$mClampRamp.Invoke($null, @([single]0.5))) 3.0
Assert-Value "ramp 10 passes through"  ([double]$mClampRamp.Invoke($null, @([single]10))) 10.0

# The storm set is hand-edited, so parsing must forgive spacing and empty entries.
$parsed = $mParse.Invoke($null, @([string]" ThunderStorm , ,SnowStorm "))
Assert-Value "ParseNames drops blank entries" ([int]$parsed.Length) 2
Assert-Value "ParseNames trims entry 0" ([string]$parsed[0]) "ThunderStorm"
Assert-Value "ParseNames trims entry 1" ([string]$parsed[1]) "SnowStorm"
Assert-Value "ParseNames on blank input" ([int]($mParse.Invoke($null, @([string]"  "))).Length) 0

# Wind immunity as a structural fact rather than a comment: the wind layer is built with
# amplitude zero, so Point annihilates any deflection handed to it.
$UI = $rc.GetType("WulfPack.RuneCompass.CompassUI")
$windAmp = $UI.GetField("WindAmplitude", $flags)
if ($null -eq $windAmp) { throw "CompassUI.WindAmplitude not found." }
Assert-Value "wind layer amplitude is zero" ([double]$windAmp.GetRawConstantValue()) 0

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


# ------------------------------------------------------------------- seal ----
#
# The comparison toggle exists so the two interference styles can be judged against each
# other in a live storm. Once judged, the loser is deleted and the winner becomes plain
# behaviour. Intent alone would not enforce that -- the mod's own history records two
# controls that existed on paper and caught nothing -- so the seal refuses while the
# toggle is still in the tree.

if ($Seal) {
    Write-Host ""
    Write-Host "=== Seal readiness ==="
    $toggle = "IndependentLayerInterference"
    $hits = Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.cs |
        Select-String -SimpleMatch -Pattern $toggle
    if ($hits) {
        Write-Host ("  [FAIL] {0} still present in {1} place(s):" -f $toggle, $hits.Count) -ForegroundColor Red
        foreach ($hit in $hits) {
            Write-Host ("         {0}:{1}" -f $hit.Filename, $hit.LineNumber) -ForegroundColor Red
        }
        Write-Host "         Judge the comparison in a live storm (STATUS.md row S10), fix the winner, delete the toggle." -ForegroundColor Red
        $script:Failures.Add("seal: $toggle survives")
    } else {
        Write-Host ("  [PASS] {0,-46} = absent" -f "comparison toggle removed")
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

Write-Host ""
if ($script:Failures.Count -eq 0) {
    Write-Host "RESULT: ALL CHECKS PASSED" -ForegroundColor Green
    exit 0
}
Write-Host ("RESULT: {0} FAILURE(S): {1}" -f $script:Failures.Count, ($script:Failures -join ', ')) -ForegroundColor Red
exit 1
