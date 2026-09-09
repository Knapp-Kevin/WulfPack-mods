<#
.SYNOPSIS
    Inspect the installed Valheim assemblies for Celestial Dial time and HUD seams.
.DESCRIPTION
    Read-only discovery helper. It does not launch Valheim, patch game methods, or mutate
    game/config/save data. The purpose is to replace historical API assumptions with exact
    signatures from the user's installed Valheim build before runtime code is written.
#>
[CmdletBinding()]
param(
    [string]$ValheimRoot = "",
    [string]$OutFile = ""
)

$ErrorActionPreference = "Stop"

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

function Resolve-ValheimRoot {
    param([string]$Root)
    if ([string]::IsNullOrWhiteSpace($Root)) { $Root = Find-ValheimRoot }
    if ([string]::IsNullOrWhiteSpace($Root) -or -not (Test-Path -LiteralPath $Root)) {
        throw "Valheim installation not found. Pass -ValheimRoot 'C:\path\to\Valheim'."
    }

    $managed = Join-Path $Root "valheim_Data\Managed"
    $assembly = Join-Path $managed "assembly_valheim.dll"
    if (-not (Test-Path -LiteralPath $assembly)) {
        throw "assembly_valheim.dll was not found under '$managed'."
    }

    [pscustomobject]@{ Root = $Root; Managed = $managed; Assembly = $assembly }
}

function Get-LoadableTypes {
    param([Reflection.Assembly]$Assembly)
    try { return $Assembly.GetTypes() }
    catch [Reflection.ReflectionTypeLoadException] {
        return $_.Exception.Types | Where-Object { $_ -ne $null }
    }
}

function Find-Type {
    param([Type[]]$Types, [string]$Name)
    $hit = $Types | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
    if (-not $hit) { throw "Type '$Name' was not found in assembly_valheim.dll." }
    return $hit
}

function Format-Method {
    param([Reflection.MethodInfo]$Method)
    $scope = if ($Method.IsStatic) { "static" } else { "instance" }
    $access = if ($Method.IsPublic) { "public" } else { "nonpublic" }
    $args = ($Method.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    return "method   [$access $scope] $($Method.ReturnType.Name) $($Method.Name)($args)"
}

function Format-Field {
    param([Reflection.FieldInfo]$Field)
    $scope = if ($Field.IsStatic) { "static" } else { "instance" }
    $access = if ($Field.IsPublic) { "public" } else { "nonpublic" }
    return "field    [$access $scope] $($Field.FieldType.Name) $($Field.Name)"
}

function Format-Property {
    param([Reflection.PropertyInfo]$Property)
    return "property $($Property.PropertyType.Name) $($Property.Name)"
}

function Get-RelevantMembers {
    param([Type]$Type, [string]$Pattern)
    $flags = [Reflection.BindingFlags]"Public,NonPublic,Instance,Static,DeclaredOnly"
    $rows = New-Object System.Collections.Generic.List[string]

    foreach ($field in ($Type.GetFields($flags) | Where-Object { $_.Name -match $Pattern } | Sort-Object Name)) {
        $rows.Add((Format-Field $field))
    }
    foreach ($property in ($Type.GetProperties($flags) | Where-Object { $_.Name -match $Pattern } | Sort-Object Name)) {
        $rows.Add((Format-Property $property))
    }
    foreach ($method in ($Type.GetMethods($flags) | Where-Object { $_.Name -match $Pattern } | Sort-Object Name)) {
        $rows.Add((Format-Method $method))
    }
    return $rows
}

function Add-Section {
    param(
        [Collections.Generic.List[string]]$Report,
        [string]$Title,
        [string[]]$Rows
    )
    $Report.Add("")
    $Report.Add("=== $Title ===")
    if (-not $Rows -or $Rows.Count -eq 0) {
        $Report.Add("(no matching members)")
        return
    }
    foreach ($row in $Rows) { $Report.Add($row) }
}

$paths = Resolve-ValheimRoot -Root $ValheimRoot
$report = New-Object System.Collections.Generic.List[string]
$report.Add("Celestial Dial Valheim API discovery")
$report.Add("Valheim root: $($paths.Root)")
$report.Add("assembly_valheim.dll: $($paths.Assembly)")
$report.Add("assembly SHA-256: $((Get-FileHash -LiteralPath $paths.Assembly -Algorithm SHA256).Hash)")
$report.Add("discovered: $([DateTime]::UtcNow.ToString('u')) UTC")

$managed = $paths.Managed
$resolver = [ResolveEventHandler]{
    param($sender, $args)
    $name = ([Reflection.AssemblyName]::new($args.Name)).Name + ".dll"
    $candidate = Join-Path $managed $name
    if (Test-Path -LiteralPath $candidate) {
        return [Reflection.Assembly]::LoadFrom($candidate)
    }
    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $assembly = [Reflection.Assembly]::LoadFrom($paths.Assembly)
    $types = @(Get-LoadableTypes -Assembly $assembly)
    $envMan = Find-Type -Types $types -Name "EnvMan"
    $hud = Find-Type -Types $types -Name "Hud"
    $minimap = Find-Type -Types $types -Name "Minimap"

    Add-Section -Report $report -Title "EnvMan day/time candidates" -Rows @(
        Get-RelevantMembers -Type $envMan -Pattern '(?i)(day|time|fraction|cycle|smooth|morning|night|dawn|dusk)')

    Add-Section -Report $report -Title "Hud anchoring candidates" -Rows @(
        Get-RelevantMembers -Type $hud -Pattern '(?i)(hud|root|map|mini|canvas|element)')

    Add-Section -Report $report -Title "Minimap anchoring/state candidates" -Rows @(
        Get-RelevantMembers -Type $minimap -Pattern '(?i)(map|root|small|large|mode|canvas|element|wind)')

    $flags = [Reflection.BindingFlags]"Public,NonPublic,Instance,Static"
    $dayMethod = $envMan.GetMethod("GetCurrentDay", $flags)
    $fractionField = $envMan.GetField("m_smoothDayFraction", $flags)

    $report.Add("")
    $report.Add("=== Historical candidate checks (reference only) ===")
    $report.Add($(if ($dayMethod) { "GetCurrentDay: PRESENT -> $(Format-Method $dayMethod)" } else { "GetCurrentDay: ABSENT" }))
    $report.Add($(if ($fractionField) { "m_smoothDayFraction: PRESENT -> $(Format-Field $fractionField)" } else { "m_smoothDayFraction: ABSENT" }))
    $report.Add("")
    $report.Add("This report proves member presence/signatures only. Runtime semantics still require an in-game validation pass.")
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}

$text = $report -join [Environment]::NewLine
Write-Host $text

if (-not [string]::IsNullOrWhiteSpace($OutFile)) {
    $parent = Split-Path $OutFile -Parent
    if ($parent -and -not (Test-Path -LiteralPath $parent)) {
        [void][IO.Directory]::CreateDirectory($parent)
    }
    Set-Content -LiteralPath $OutFile -Value $text -Encoding UTF8
    Write-Host ""
    Write-Host "Wrote discovery report: $OutFile"
}
