# Pied Piper status

## Current state

Rebased onto current `main`, compiles clean against the shipped **Valheim 1.0** assemblies,
loads with its Harmony patches applied and no exceptions, and passes lifecycle containment.

**Not yet exercised in play.** Nobody has pressed E on a tamed creature with this installed.
The command path is verified by static analysis of the game's own code, not by observation.

Tier: **state-touching** (see the root README risk-tier table).

## Environment

| Fact | Value |
|---|---|
| Valheim | **1.0** |
| Unity | 6000.0.61f1 |
| BepInEx | 5.4.22.0 / BepInExPack Valheim 5.4.2202 |

## What the patches do

| Patch | Effect |
|---|---|
| `Tameable.Awake` postfix | sets `m_commandable = true` on every `Tameable` |
| `Tameable.Interact` prefix/postfix | temporarily forces `m_commandable = false` for saddled rideables, restores it afterwards |

The rideable guard fails closed: if `HaveSaddle` cannot be resolved by reflection, or the
invocation throws, commanding is suppressed rather than allowed.

## Save-state analysis

The state-touching tier exists to answer one question: can this reach the save? Verified
against the installed `assembly_valheim.dll` with Mono.Cecil rather than assumed.

**The patch itself persists nothing.**

- `Tameable.m_commandable` is a plain public instance `Boolean`.
- It is **read by exactly one method** (`Tameable.Interact`) and **written by no game code**.
- **No method in `Tameable` references `m_commandable` and a `ZDO` together.**

It is a prefab-authored field re-applied on every `Awake`, so removing the mod restores
vanilla behaviour the next time the component wakes. Nothing survives the session.

**But it unlocks a vanilla path that does persist.** `Tameable.Command` invokes
`RPC_Command`, which calls `ZDO::Set(int, string)` and `BaseAI::SetPatrolPoint()`, and
`SetPatrolPoint` / `ResetPatrolPoint` / `GetPatrolPoint` all read or write the ZDO.

So telling a creature to **stay** writes a patrol point into world state, through the game's
own mechanism, on a creature vanilla would not have let you command.

### Uninstall hazard

This is the one thing that could outlive the mod.

A creature left on **stay** keeps its ZDO patrol point after Pied Piper is removed. Its
`m_commandable` reverts to the prefab value, so it can no longer be commanded — and it
cannot be told to follow again, because that is the very interaction the mod was providing.
The creature stays anchored to its patrol point permanently.

**Recovery exists while the mod is installed**: commanding a creature back to *follow* calls
`ResetPatrolPoint`, clearing the ZDO value. The safe removal procedure is therefore:

1. Command every Pied-Piper-commanded creature back to **follow**.
2. Then `.\PiedPiper\build-local.ps1 -Uninstall`.

This belongs in the README before release, and is the main reason this mod is not
`read-only`.

## Validated

| Check | Result |
|---|---|
| `build-local.ps1` | clean, 0 warnings / 0 errors |
| `-Install` / `-Status` | clean, no PowerShell errors |
| BepInEx load | clean, 5 plugins, zero Pied Piper exceptions |
| Harmony patch application | both targets resolve on Valheim 1.0 — `PatchAll` would throw otherwise |
| Lifecycle containment | disable / enable / uninstall, SHA-256 diff: **105 save files, all sibling plugin and config files byte-identical** |

## Defect fixed during validation

`build-local.ps1:127` used `Write-Host "$Label: $To"`. PowerShell reads `$Label:` as a
drive-qualified variable, so the script failed at **parse** time in every mode — build,
install, status, disable, enable and uninstall. It had never been run.

The same defect exists in Rune Compass at the same line, because the script was copied from
there. `RestedWhispers` and `VidarShrugged` carry the correct form.

## Not yet done

- **No in-play verification.** Pressing E on a tamed creature, confirming Follow/Stay
  toggles, and confirming a saddled rideable is still ridden rather than commanded.
- **No save-integrity diff across a play session with the patches live.** That is the gate
  this tier requires, and it needs someone to play with tamed creatures present. The
  install/uninstall diff above does not substitute for it: it proves the files are clean,
  not that a session with active patches leaves the world unchanged.
- The uninstall hazard above is analysed, not observed.
