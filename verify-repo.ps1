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
param([string]$RepoRoot)

$ErrorActionPreference = "Stop"

# The default cannot live in the param block. Under [CmdletBinding()], PowerShell binds
# parameter defaults before $PSScriptRoot is populated in script scope, so
# `param([string]$RepoRoot = $PSScriptRoot)` binds empty -- but only when the script is
# invoked as `powershell -File .\verify-repo.ps1`. Called the documented interactive
# way (`.\verify-repo.ps1`) it binds correctly, which is why this survived: the form
# that breaks is the form automation uses, and the one that works is the form a human uses.
if (-not $RepoRoot) {
    $RepoRoot = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
}

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

# ------------------------------------------------- documented commands are runnable ----
# Authoring escape processing turns a lost backslash into a control character: a "\v"
# becomes a vertical tab, a "\b" a backspace, and the letter goes with it. Rendered markdown
# still looks almost right, so a README can spend its whole life instructing readers to run
# a command that does not exist. This repository shipped exactly that in three places.
#
# So: control characters in tracked text are a hard failure, and every .ps1 a document names
# must resolve to a real file. Paths are normalised to forward slashes before matching, which
# is also why this check contains no doubled backslash of its own -- the bug it exists to
# catch is a bug about backslashes surviving one layer of processing too few.
Write-Host ""
Write-Host "--- documented commands ---"

$bs = [string][char]92
$textPattern  = '\.(md|ps1|cs|csproj|json|txt|yml|yaml)$'
$controlChars = [regex]::new('[\x00-\x08\x0B\x0C\x0E-\x1F]')
$scriptRefs   = [regex]::new('[A-Za-z0-9_./-]*[A-Za-z0-9_-]\.ps1')
$seenRef = $false
$failuresBefore = $script:Failures.Count

# Scope is what git tracks. docs/, qor/ and .claude/ are gitignored local governance
# surfaces, not part of the published repository, and holding them to the repository's
# documentation contract would report failures nobody can act on from a clone.
$tracked = & git -C $RepoRoot ls-files
if ($LASTEXITCODE -ne 0 -or -not $tracked) {
    Fail "could not list tracked files via git - documentation check cannot establish its scope"
    $tracked = @()
}

# Any .ps1 anywhere in the repository is a legitimate target: build-local.ps1 lives once
# per mod, and a document beside one may name it bare.
$scriptsByName = @{}
foreach ($ps1 in (Get-ChildItem -LiteralPath $RepoRoot -File -Recurse -Filter *.ps1 -ErrorAction SilentlyContinue)) {
    $scriptsByName[$ps1.Name] = $true
}

foreach ($relRaw in $tracked) {
    $file = Get-Item -LiteralPath (Join-Path $RepoRoot $relRaw) -ErrorAction SilentlyContinue
    if (-not $file) { continue }
    $full = $file.FullName.Replace($bs, '/')
    if ($file.Name -notmatch $textPattern) { continue }

    $text = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $text) { continue }
    $rel = $full.Substring($RepoRoot.Replace($bs, '/').Length).TrimStart('/')

    if ($controlChars.IsMatch($text)) {
        $codes = ($controlChars.Matches($text) |
            ForEach-Object { "0x{0:X2}" -f [int][char]$_.Value } |
            Select-Object -Unique) -join ", "
        Fail "$rel contains control characters ($codes) - almost certainly a backslash eaten by escape processing"
    }

    # Only documentation makes a promise to a reader. Source and script files mention .ps1
    # names incidentally -- including this gate, which names itself in a comment -- and
    # counting those would keep $seenRef permanently true, making the parsed-nothing guard
    # below unreachable. A guard that cannot fire is not a guard.
    if ($file.Name -notmatch '\.md$') { continue }

    # Normalise separators so a mod-qualified path and a repo-relative one reduce to the
    # same shape, then require the named script to exist somewhere in the repository.
    foreach ($m in $scriptRefs.Matches($text.Replace($bs, '/'))) {
        $target = $m.Value.TrimStart('.', '/')
        if (-not $target) { continue }
        $seenRef = $true
        $beside = Join-Path $file.DirectoryName $target
        $atRoot = Join-Path $RepoRoot $target
        $byName = $scriptsByName.ContainsKey((Split-Path $target -Leaf))
        if (-not $byName -and -not (Test-Path -LiteralPath $beside) -and -not (Test-Path -LiteralPath $atRoot)) {
            Fail "$rel documents '$($m.Value)', which names no script in this repository"
        }
    }
}

if (-not $seenRef) {
    Fail "no .ps1 invocation named in any markdown file - this check parsed nothing and would pass forever"
} elseif ($script:Failures.Count -eq $failuresBefore) {
    Pass "every script named in documentation resolves; no control characters in tracked text"
}

Write-Host ""
if ($script:Failures.Count -eq 0) {
    Write-Host ("RESULT: ALL CHECKS PASSED ({0} mods, {1} tier rows)" -f $mods.Count, $declared.Count) -ForegroundColor Green
    exit 0
}
Write-Host ("RESULT: {0} FAILURE(S)" -f $script:Failures.Count) -ForegroundColor Red
exit 1
