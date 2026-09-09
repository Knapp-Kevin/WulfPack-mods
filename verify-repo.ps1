<#
.SYNOPSIS
    Verifies the mod risk-tier table in README.md against what the mods actually do.
.DESCRIPTION
    Repository-level checks. This repository uses zero GitHub Actions and no hosted CI, so
    this runs locally alongside each mod's own build-local.ps1 and verify-local.ps1.

    Three failures are possible:

      1. Coverage      - a mod folder with no row, or a row naming no folder.
      2. Tier honesty  - a mod declared read-only that references Harmony or carries patch
                         attributes. The claim is checked against source, not trusted.
      3. Empty parse   - the table produced zero rows. A gate that reads nothing has not
                         passed; it has not run. This repository has been bitten by exactly
                         that before, so it is a hard failure rather than a silent success.
.PARAMETER RepoRoot
    Repository root. Defaults to the directory containing this script.
#>
[CmdletBinding()]
param([string]$RepoRoot = $PSScriptRoot)

$ErrorActionPreference = "Stop"

$script:Failures = New-Object System.Collections.Generic.List[string]
function Fail { param([string]$Message) Write-Host "  [FAIL] $Message" -ForegroundColor Red; $script:Failures.Add($Message) }
function Pass { param([string]$Message) Write-Host "  [PASS] $Message" }

$readmePath = Join-Path $RepoRoot "README.md"
if (-not (Test-Path -LiteralPath $readmePath)) { throw "README.md not found at $RepoRoot." }

# ---------------------------------------------------------------- discover mods ----
# A mod is a top-level folder carrying a Thunderstore manifest.
$mods = Get-ChildItem -LiteralPath $RepoRoot -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "manifest.json") } |
    Select-Object -ExpandProperty Name |
    Sort-Object

Write-Host ""
Write-Host "=== Mod risk tiers ==="
Write-Host ("mod folders found: {0}" -f ($mods -join ", "))

# ------------------------------------------------------------------ parse table ----
# Rows look like: | **Name** | tier | touches | gate |
$declared = @{}
$inTable = $false
foreach ($line in (Get-Content -LiteralPath $readmePath)) {
    if ($line -match '^\|\s*Mod\s*\|\s*Tier\s*\|') { $inTable = $true; continue }
    if ($inTable) {
        if ($line -notmatch '^\|') { break }
        if ($line -match '^\|\s*[-\s|]+\|?\s*$') { continue }
        $cells = ($line.Trim('|') -split '\|') | ForEach-Object { $_.Trim() }
        if ($cells.Count -lt 2) { continue }
        $name = $cells[0] -replace '\*', ''
        $tier = $cells[1] -replace '\*', ''
        # Table names are human-readable ("Rune Compass"); folders are not ("RuneCompass").
        # Match on the space-stripped form so the table stays readable.
        $key = ($name -replace '\s', '')
        if ($key) { $declared[$key] = @{ Tier = $tier.Trim(); Display = $name.Trim() } }
    }
}

Write-Host ("table rows parsed : {0}" -f $declared.Count)

# A parser that reads nothing must not report success.
if ($declared.Count -eq 0) {
    Fail "risk-tier table parsed ZERO rows - the table is missing or its shape changed, and this check would otherwise pass forever"
    Write-Host ""
    Write-Host "RESULT: 1 FAILURE(S)" -ForegroundColor Red
    exit 1
}

$validTiers = @("read-only", "state-touching", "persistent")

# -------------------------------------------------------------------- coverage ----
Write-Host ""
Write-Host "--- coverage ---"
foreach ($mod in $mods) {
    if ($declared.ContainsKey($mod)) { Pass "$mod has a tier row ($($declared[$mod].Tier))" }
    else { Fail "$mod has no row in the risk-tier table" }
}
foreach ($key in $declared.Keys) {
    if ($mods -notcontains $key) {
        Fail "risk-tier table lists '$($declared[$key].Display)', which matches no mod folder"
    }
    if ($validTiers -notcontains $declared[$key].Tier) {
        Fail "$($declared[$key].Display) declares tier '$($declared[$key].Tier)'; expected one of: $($validTiers -join ', ')"
    }
}

# --------------------------------------------------------------- tier honesty ----
# A read-only claim is checked against the code. Harmony is the reliable signal: a
# reference in the csproj, or a patch attribute / bootstrap call in source. The persistent
# tier stays operator-declared because writes to save state are not detectable this way,
# and a check that pretends otherwise would be worse than none.
Write-Host ""
Write-Host "--- tier honesty (read-only claims checked against source) ---"
foreach ($mod in $mods) {
    if (-not $declared.ContainsKey($mod)) { continue }
    if ($declared[$mod].Tier -ne "read-only") {
        Write-Host "  [skip] $mod is $($declared[$mod].Tier); no read-only claim to check"
        continue
    }

    $modPath = Join-Path $RepoRoot $mod
    $evidence = New-Object System.Collections.Generic.List[string]

    foreach ($proj in (Get-ChildItem -LiteralPath $modPath -Filter *.csproj -File -ErrorAction SilentlyContinue)) {
        if ((Get-Content -LiteralPath $proj.FullName -Raw) -match '0Harmony|HarmonyX') {
            $evidence.Add("$($proj.Name) references Harmony")
        }
    }
    foreach ($src in (Get-ChildItem -LiteralPath $modPath -Filter *.cs -File -Recurse -ErrorAction SilentlyContinue)) {
        $text = Get-Content -LiteralPath $src.FullName -Raw
        if ($text -match '\[HarmonyPatch|HarmonyPrefix|HarmonyPostfix|HarmonyTranspiler|CreateAndPatchAll|PatchAll\(') {
            $evidence.Add("$($src.Name) contains Harmony patch code")
        }
    }

    if ($evidence.Count -gt 0) {
        Fail "$mod is declared read-only but patches the game: $($evidence -join '; ')"
    } else {
        Pass "$mod read-only claim holds (no Harmony reference or patch code)"
    }
}

Write-Host ""
if ($script:Failures.Count -eq 0) {
    Write-Host ("RESULT: ALL CHECKS PASSED ({0} mods, {1} tier rows)" -f $mods.Count, $declared.Count) -ForegroundColor Green
    exit 0
}
Write-Host ("RESULT: {0} FAILURE(S)" -f $script:Failures.Count) -ForegroundColor Red
exit 1
