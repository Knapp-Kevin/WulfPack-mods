# Pied Piper

Pied Piper is a deliberately small Valheim quality-of-life mod that gives eligible tamed creatures one consistent command: **Follow / Stay**.

> One command. One behavior. Follow or stay.

**Current state: functionally complete and operator-validated on Valheim 1.0.** The mod compiles cleanly, loads cleanly, passes lifecycle containment, and its claimed interaction behavior has been confirmed in play. Remaining work is release polish rather than gameplay implementation, including a Thunderstore-ready `icon.png`.

See [CONCEPT.md](CONCEPT.md) for the product/visual contract and [STATUS.md](STATUS.md) for validation evidence.

## How it works

Pied Piper uses Valheim's own tameable command path rather than introducing custom AI or pathfinding.

1. A `Tameable.Awake` postfix enables Valheim's existing commandable path on tameables.
2. The player uses Valheim's normal **Use / interact** action, `E` by default.
3. `Tameable.Interact` keeps Valheim's native tame-state checks, pet effect, messaging, command RPC, follow target, and patrol behavior.
4. Wild creatures remain unaffected.

There is no separate command key, command wheel, selection mode, or pet-management framework.

## Input behavior

```text
Look at tamed creature
        ↓
Press Valheim Use / interact
       E by default
        ↓
Valheim native Follow / Stay command
```

If the player remaps Valheim's normal Use action, Pied Piper follows that native interaction path.

## Confirmed rideable behavior

Riding is untouched because the saddle owns its own interaction path.

- interact with the **creature's body**: Follow / Stay toggles;
- interact with the **saddle**: mounting still works normally.

An earlier implementation tried to block commanding whenever a saddle was attached. Operator testing proved that was the wrong model: `Sadle` handles mounting independently, so the guard only broke body interaction. It was removed.

## Supported scope

The current product applies the native command path consistently to tameables, including the target families that motivated the mod:

- wolf
- boar
- hen / chicken
- lox
- asksvin

Future tameables should inherit the same behavior when they use Valheim's compatible `Tameable` contract rather than requiring a creature-by-creature custom system.

## Before you uninstall

**Command every Pied Piper-commanded creature back to Follow first.**

Telling a creature to **Stay** uses Valheim's own command RPC and writes a patrol point into world state. That patrol point can outlive the mod. Once Pied Piper is removed, a creature whose prefab is not normally commandable loses the interaction needed to clear that Stay state.

Safe removal order:

1. Command every Pied Piper-commanded creature back to **Follow**.
2. Quit Valheim.
3. Run:

```powershell
.\PiedPiper\build-local.ps1 -Uninstall
```

Returning the creature to Follow calls Valheim's `ResetPatrolPoint`, clearing the stored patrol point before removal.

## Validation summary

Confirmed against Valheim 1.0:

- Release build: 0 warnings, 0 errors.
- BepInEx load: clean, no Pied Piper exceptions.
- Follow / Stay on the normal interact key: pass.
- Saddled creature body interaction still commands: pass.
- Saddle interaction still mounts: pass.
- Wild creatures remain unaffected: pass.
- Lifecycle containment: pass.
- Save-integrity comparison after a live session: no loss, truncation, or orphaned worlds.

Detailed evidence and the persistence analysis are in [STATUS.md](STATUS.md).

## Explicit non-goals

Pied Piper does not provide:

- global army commands
- radius-based mass selection
- formations
- teleport-to-player behavior
- pet inventory
- pet stat changes
- breeding automation
- pet protection systems
- portal management
- map markers
- custom pathfinding
- a general tame overhaul

If a feature cannot be explained as part of Follow / Stay, it belongs somewhere else.

## Local lifecycle

From the repository root:

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
.\PiedPiper\build-local.ps1 -Status
.\PiedPiper\build-local.ps1 -Disable
.\PiedPiper\build-local.ps1 -Enable
.\PiedPiper\build-local.ps1 -Uninstall
```

The helper manages only Pied Piper-owned files and does not touch unrelated plugins.

## Repository layout

```text
PiedPiper/
├── Plugin.cs
├── TameablePatches.cs
├── PiedPiper.csproj
├── build-local.ps1
├── manifest.json
├── README.md
├── CONCEPT.md
├── IMPLEMENTATION_PLAN.md
└── STATUS.md
```

## Release readiness

Gameplay behavior is complete for the current scope. Before a Thunderstore release, finish release packaging and add a proper `icon.png`; do not reopen gameplay discovery merely because the old README said "API discovery next."

GitHub Actions are prohibited in WulfPack Mods. Pied Piper is built and validated locally with zero Actions runs.