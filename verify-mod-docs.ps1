<#
.SYNOPSIS
    Verifies the repository-wide mod documentation contract.
.DESCRIPTION
    Every top-level mod carrying manifest.json must have a README.md and CONCEPT.md.
    CONCEPT.md must keep concept art separate from runtime screenshots and contain the
    standard section headings defined in MOD_DOCUMENTATION_STANDARD.md.
#>
[CmdletBinding()]
param([string]$RepoRoot)

$ErrorActionPreference = "Stop"

if (-not $RepoRoot) {
    $RepoRoot = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
}

$failures = New-Object System.Collections.Generic.List[string]
function Fail { param([string]$Message) Write-Host "  [FAIL] $Message" -ForegroundColor Red; $failures.Add($Message) }
function Pass { param([string]$Message) Write-Host "  [PASS] $Message" }

$rootReadme = Join-Path $RepoRoot "README.md"
$standard = Join-Path $RepoRoot "MOD_DOCUMENTATION_STANDARD.md"
if (-not (Test-Path -LiteralPath $rootReadme)) { throw "README.md not found at $RepoRoot." }
if (-not (Test-Path -LiteralPath $standard)) { throw "MOD_DOCUMENTATION_STANDARD.md not found at $RepoRoot." }

$requiredHeadings = @(
    "## Product intent",
    "## Player experience",
    "## Visual language",
    "## Concept art",
    "## Screenshots",
    "## Constraints and non-goals"
)

$mods = Get-ChildItem -LiteralPath $RepoRoot -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "manifest.json") } |
    Sort-Object Name

if (-not $mods) {
    Fail "no mod folders were discovered; the verifier parsed nothing"
}

$rootText = Get-Content -LiteralPath $rootReadme -Raw

Write-Host ""
Write-Host "=== Mod documentation contract ==="
Write-Host ("mod folders found: {0}" -f (($mods | Select-Object -ExpandProperty Name) -join ", "))

foreach ($mod in $mods) {
    $readme = Join-Path $mod.FullName "README.md"
    $concept = Join-Path $mod.FullName "CONCEPT.md"

    Write-Host ""
    Write-Host ("--- {0} ---" -f $mod.Name)

    if (Test-Path -LiteralPath $readme) { Pass "$($mod.Name)/README.md exists" }
    else { Fail "$($mod.Name) has no README.md" }

    if (-not (Test-Path -LiteralPath $concept)) {
        Fail "$($mod.Name) has no CONCEPT.md"
        continue
    }
    Pass "$($mod.Name)/CONCEPT.md exists"

    $text = Get-Content -LiteralPath $concept -Raw
    $lastIndex = -1
    foreach ($heading in $requiredHeadings) {
        $index = $text.IndexOf($heading, [System.StringComparison]::Ordinal)
        if ($index -lt 0) {
            Fail "$($mod.Name)/CONCEPT.md is missing '$heading'"
            continue
        }
        if ($index -lt $lastIndex) {
            Fail "$($mod.Name)/CONCEPT.md has '$heading' out of standard order"
        }
        $lastIndex = $index
    }

    $conceptStart = $text.IndexOf("## Concept art", [System.StringComparison]::Ordinal)
    $screensStart = $text.IndexOf("## Screenshots", [System.StringComparison]::Ordinal)
    $constraintsStart = $text.IndexOf("## Constraints and non-goals", [System.StringComparison]::Ordinal)

    if ($conceptStart -ge 0 -and $screensStart -gt $conceptStart) {
        $conceptBlock = $text.Substring($conceptStart, $screensStart - $conceptStart)
        if ($conceptBlock -match 'Assets/Screenshots/') {
            Fail "$($mod.Name)/CONCEPT.md references a screenshot asset inside Concept art"
        }
    }

    if ($screensStart -ge 0) {
        $end = if ($constraintsStart -gt $screensStart) { $constraintsStart } else { $text.Length }
        $screensBlock = $text.Substring($screensStart, $end - $screensStart)
        $images = [regex]::Matches($screensBlock, '!\[[^\]]*\]\(([^)]+)\)')
        foreach ($image in $images) {
            $path = $image.Groups[1].Value
            if ($path -notmatch '^Assets/Screenshots/') {
                Fail "$($mod.Name)/CONCEPT.md screenshot image '$path' is not under Assets/Screenshots/"
            }
        }
    }

    $readmeLink = "[$($mod.Name) concept]($($mod.Name)/CONCEPT.md)"
    $simpleLink = "($($mod.Name)/CONCEPT.md)"
    if ($rootText.Contains($readmeLink) -or $rootText.Contains($simpleLink)) {
        Pass "root README links $($mod.Name)/CONCEPT.md"
    } else {
        Fail "root README does not link $($mod.Name)/CONCEPT.md"
    }
}

Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host ("RESULT: ALL DOCUMENTATION CHECKS PASSED ({0} mods)" -f $mods.Count) -ForegroundColor Green
    exit 0
}

Write-Host ("RESULT: {0} FAILURE(S)" -f $failures.Count) -ForegroundColor Red
exit 1
