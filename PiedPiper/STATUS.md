# Pied Piper status

## Current state

Rebased onto current `main`, compiles clean against the shipped **Valheim 1.0** assemblies,
loads with its Harmony patches applied and no exceptions, and passes lifecycle containment.

**Confirmed working in play** by the operator across every behaviour this mod claims, and
the save-integrity gate its tier requires has been run and passed. One defect was found
during testing and fixed: see "Saddle guard removed".

Tier: **state-touching** (see the root README risk-tier table).

## Environment

| Fact | Value |
|---|---|
| Valheim | **1.0** |
| Unity | 6000.0.61f1 |
| BepInEx | 5.4.22.0 / BepInExPack Valheim 5.4.2202 |

## What the patches do

One patch: a `Tameable.Awake` postfix setting `m_commandable = true`, so every tame accepts
a Follow / Stay command instead of only the prefabs authored for it.

### Saddle guard removed

An earlier revision also patched `Tameable.Interact` to suppress commanding whenever the
creature carried a saddle, so as not to interfere with riding.

Operator testing found the consequence: on a saddled creature, interacting with the body did
nothing instead of toggling Follow. Their reasoning — riding is reached through the saddle,
so the body interaction should still command — is what the game does:

- `Sadle` is its own `Interactable`, with `Interact(Humanoid, bool, bool)`.
- `Tameable.Interact` contains **no saddle or mount reference anywhere in its call graph**.
  It does name, tamed check, effect, `Command`, message, and nothing else.

The guard was therefore suppressing commands on a path that cannot mount. It could not
protect riding, which was its whole purpose, and it did break petting on every saddled
creature. Removed, along with the `HaveSaddle` reflection, the fail-closed catch and the
guard state struct. Riding is untouched because this mod does not patch `Sadle`.

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

## Operator acceptance

Confirmed in play against Valheim 1.0:

| # | Check | Result |
|---|---|---|
| 1 | Follow / Stay toggles on the interact key | **pass** |
| 2 | Petting a saddled creature toggles Follow | **pass** (this was the defect; fixed by removing the saddle guard) |
| 3 | Interacting with the saddle still mounts | **pass** — riding is untouched, as predicted from `Sadle` owning its own `Interact` |
| 4 | Untamed creatures show no command prompt | **pass** |

## Save-integrity gate

Required by the state-touching tier. `verify-save-integrity.ps1` snapshots the save
directory before a session and compares after; it reports loss, truncation, implausible
shrinkage and orphaned worlds, and deliberately does not fail on files merely changing.

Result after a session with the patches live:

```text
files before   : 105
files after    : 105
modified       : 2   (expected - someone played)
RESULT: NO LOSS, NO TRUNCATION, NO ORPHANED WORLDS
```

**Caveat**: taken mid-session with the game still running, so it reflects an autosave rather
than a clean shutdown. Worth re-running after a quit before this is treated as final.

## Live hazard in the operator's world

A creature was commanded to **stay** during testing, so a ZDO patrol point exists in that
world now.

**Command it back to Follow before uninstalling Pied Piper.** Once the mod is gone,
`m_commandable` reverts to the prefab value, the creature can no longer be commanded, and
there is no way to release it from its patrol point — it stays anchored permanently.
Commanding back to Follow calls `ResetPatrolPoint`, which clears the ZDO value; that path
only exists while the mod is installed.

This belongs in the README before release. It is the single behaviour that outlives removal.
