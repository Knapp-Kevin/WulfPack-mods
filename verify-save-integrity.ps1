<#
.SYNOPSIS
    Save-integrity gate for state-touching mods: snapshot before a session, compare after.
.DESCRIPTION
    The risk-tier table in README.md requires a state-touching mod to show that a play
    session with its patches live leaves character and world data sound. The
    install/uninstall diff in each mod's lifecycle check does not cover this: it proves the
    files are untouched when nobody is playing, which is a different claim.

    What this checks is corruption and loss, not change. A session that changes save files
    is a session where someone played -- that is expected and is not a finding. What would
    be a finding is a world or character that disappeared, was truncated to nothing, lost
    its backup, or shrank implausibly.

    Usage:

        .\verify-save-integrity.ps1 -Baseline     # before playing
        .\verify-save-integrity.ps1 -Compare      # after playing

    Local only. This repository uses zero GitHub Actions.
.PARAMETER Baseline
    Record the current state of the save directory.
.PARAMETER Compare
    Compare the current state against the recorded baseline.
.PARAMETER SavePath
    Valheim save directory. Defaults to the standard LocalLow location.
.PARAMETER ShrinkTolerance
    Fraction a file may shrink before it is treated as suspicious. Default 0.25 (25%).
#>
[CmdletBinding(DefaultParameterSetName = "Compare")]
param(
    [Parameter(ParameterSetName = "Baseline")] [switch]$Baseline,
    [Parameter(ParameterSetName = "Compare")] [switch]$Compare,
    [string]$SavePath = (Join-Path $env:USERPROFILE 'AppData\LocalLow\IronGate\Valheim'),
    [double]$ShrinkTolerance = 0.25
)

$ErrorActionPreference = "Stop"
$snapshotPath = Join-Path $PSScriptRoot ".save-baseline.json"

function Get-SaveState {
    param([string]$Root)
    if (-not (Test-Path -LiteralPath $Root)) { throw "Save directory not found: $Root" }
    $state = @{}
    Get-ChildItem -LiteralPath $Root -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -notin @('.log') } |
        ForEach-Object {
            $rel = $_.FullName.Substring($Root.Length).TrimStart('\')
            $state[$rel] = @{
                Length = $_.Length
                Hash   = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        }
    return $state
}

if ($PSCmdlet.ParameterSetName -eq "Baseline" -or $Baseline) {
    $state = Get-SaveState -Root $SavePath
    $payload = @{
        TakenUtc = (Get-Date).ToUniversalTime().ToString("o")
        SavePath = $SavePath
        Files    = $state
    }
    $payload | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $snapshotPath -Encoding utf8
    Write-Host ""
    Write-Host "Baseline recorded: $($state.Count) save files"
    Write-Host "  $snapshotPath"
    Write-Host ""
    Write-Host "Play a session with the mod's patches live, then run:"
    Write-Host "  .\verify-save-integrity.ps1 -Compare"
    exit 0
}

if (-not (Test-Path -LiteralPath $snapshotPath)) {
    throw "No baseline found. Run .\verify-save-integrity.ps1 -Baseline before the session."
}

$saved = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
$before = @{}
foreach ($p in $saved.Files.PSObject.Properties) { $before[$p.Name] = $p.Value }
$after = Get-SaveState -Root $SavePath

$failures = New-Object System.Collections.Generic.List[string]
$changed = 0
$added = 0

Write-Host ""
Write-Host "=== Save integrity ==="
Write-Host "baseline taken : $($saved.TakenUtc)"
Write-Host "files before   : $($before.Count)"
Write-Host "files after    : $($after.Count)"
Write-Host ""

# Loss and truncation are the findings. Change is not.
foreach ($rel in $before.Keys) {
    if (-not $after.ContainsKey($rel)) {
        $failures.Add("LOST: $rel disappeared during the session")
        continue
    }
    $b = [long]$before[$rel].Length
    $a = [long]$after[$rel].Length
    if ($a -eq 0 -and $b -gt 0) {
        $failures.Add("TRUNCATED: $rel is now empty (was $b bytes)")
    }
    elseif ($b -gt 0 -and $a -lt ($b * (1.0 - $ShrinkTolerance))) {
        $failures.Add("SHRANK: $rel $b -> $a bytes ($([int](100 - 100.0 * $a / $b))% smaller)")
    }
    elseif ($after[$rel].Hash -ne $before[$rel].Hash) { $changed++ }
}
foreach ($rel in $after.Keys) { if (-not $before.ContainsKey($rel)) { $added++ } }

Write-Host "modified       : $changed  (expected - someone played)"
Write-Host "added          : $added    (expected - new backups and saves)"
Write-Host ""

# Every world needs its metadata alongside its data, or it will not appear in the menu.
$worlds = $after.Keys | Where-Object { $_ -match '\.fwl$' } | ForEach-Object { $_ -replace '\.fwl$', '' }
foreach ($w in $worlds) {
    if (-not ($after.Keys -contains "$w.db")) { $failures.Add("ORPHANED: $w.fwl has no matching .db") }
}
$dbs = $after.Keys | Where-Object { $_ -match '\.db$' -and $_ -notmatch 'backup|_old' } | ForEach-Object { $_ -replace '\.db$', '' }
foreach ($d in $dbs) {
    if (-not ($after.Keys -contains "$d.fwl")) { $failures.Add("ORPHANED: $d.db has no matching .fwl") }
}

if ($failures.Count -eq 0) {
    Write-Host "RESULT: NO LOSS, NO TRUNCATION, NO ORPHANED WORLDS" -ForegroundColor Green
    Write-Host "Character and world data survived the session intact."
    exit 0
}

foreach ($f in $failures) { Write-Host "  [FAIL] $f" -ForegroundColor Red }
Write-Host ""
Write-Host ("RESULT: {0} INTEGRITY FAILURE(S)" -f $failures.Count) -ForegroundColor Red
exit 1
