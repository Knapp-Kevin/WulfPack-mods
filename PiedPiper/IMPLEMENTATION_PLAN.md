# Pied Piper implementation plan

## Objective

Provide one consistent Follow / Stay command for eligible tamed Valheim creatures while preserving native behavior, avoiding custom AI, and keeping the mod safely removable.

## Current state

Gameplay implementation for the current scope is **complete and operator-validated on Valheim 1.0**.

The remaining work is release polish, not API discovery or gameplay architecture:

- add a Thunderstore-ready `icon.png`;
- verify final manifest/package contents;
- repeat the save-integrity comparison after a clean game shutdown before public release.

See [STATUS.md](STATUS.md) for the detailed evidence and persistence analysis.

## Phase 0: repository mesh

Status: **complete**.

Established:

- dedicated `PiedPiper/` top-level directory;
- README, concept, implementation, and status documentation;
- Thunderstore manifest scaffold;
- root README registration;
- local build/install/disable/enable/status/uninstall tooling;
- issue #6 as the implementation/release tracker.

## Phase 1: authoritative native command seam

Status: **complete**.

Validated against the installed Valheim 1.0 assemblies:

1. `Tameable.Awake` is the smallest reliable seam for enabling the existing commandable path.
2. Valheim's normal Use / interact action, `E` by default, remains the input.
3. `Tameable.Interact` owns the native tame-state checks, pet effect, messaging, command RPC, follow target, and patrol behavior.
4. Pied Piper does not own custom target acquisition, pathfinding, or duplicated follow state.
5. Wild creatures remain unaffected.

The implementation is intentionally small: a Harmony postfix enables the native commandable path and lets Valheim do the rest.

## Phase 2: supported behavior validation

Status: **complete for the current product scope**.

Operator validation confirms:

- Follow / Stay works through the normal interact action;
- wolf behavior remains valid;
- non-wolf tameables gain the same native command path;
- wild creatures remain unaffected;
- body interaction on a saddled creature still toggles Follow / Stay;
- saddle interaction still mounts normally.

### Saddle-rule correction

The original plan proposed suppressing commands whenever a saddle was attached. In-game testing proved that model wrong.

`Sadle` owns mounting through its own interaction path. `Tameable.Interact` does not mount the creature. Blocking `Tameable.Interact` when a saddle existed therefore broke body commands without protecting riding.

The saddle guard was removed. The current rule is:

- creature body interaction controls Follow / Stay;
- saddle interaction controls mounting;
- Pied Piper never patches or replaces the saddle interaction path.

## Phase 3: lifecycle and persistence hardening

Status: **complete for gameplay acceptance, with one release rerun recommended**.

Verified:

- clean release build;
- clean BepInEx load;
- lifecycle containment across install, disable, enable, and uninstall;
- unrelated plugins/configs remain intact;
- no custom save schema or migration dependency;
- live-session save-integrity comparison reports no loss, truncation, or orphaned worlds.

### Native persistence hazard

Pied Piper itself does not persist custom state, but it unlocks Valheim's native Stay command on creatures that may not normally be commandable.

Valheim's Stay path writes a patrol point to world state. If a Pied Piper-commanded creature is left on Stay and the mod is removed, that patrol point can remain while the creature loses the interaction needed to clear it.

Safe removal procedure:

1. command every Pied Piper-commanded creature back to **Follow**;
2. quit Valheim;
3. uninstall Pied Piper.

Returning to Follow calls Valheim's `ResetPatrolPoint`, clearing the stored patrol point.

Before a public release, repeat the save-integrity comparison after a clean game shutdown so the final release record is not based only on a mid-session autosave comparison.

## Phase 4: release polish

Status: **remaining**.

- [ ] add a proper Thunderstore `icon.png`;
- [ ] verify final `manifest.json` metadata;
- [ ] verify local package contents;
- [ ] repeat save-integrity comparison after clean shutdown;
- [ ] update issue #6 with final release evidence and close it when release-ready.

None of these items reopen gameplay discovery. The Follow / Stay implementation is already proven for the current scope.

## Architecture

```text
Plugin
  └── Harmony lifecycle

TameablePatches
  └── Awake postfix: enable Valheim's existing commandable path

Valheim
  ├── normal Use / interact input
  ├── Tameable.Interact
  ├── native Command RPC
  └── native AI follow/patrol behavior
```

## Safety and scope boundaries

- No GitHub Actions.
- No custom save schema.
- No custom pathfinding.
- No pet teleporting.
- No global mass-command system.
- No pet stats, combat, breeding, or inventory changes.
- No replacement riding system.
- Installed Valheim assemblies are authoritative.
- The native Stay patrol-point persistence hazard must remain documented in user-facing release material.
