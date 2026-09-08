# Pied Piper implementation plan

## Objective

Add one consistent Follow / Stay command to eligible tamed Valheim creatures while preserving native behavior, avoiding custom AI, and keeping the mod cleanly removable.

## Phase 0: repository mesh

Status: **complete**.

Established:

- dedicated top-level `PiedPiper/` directory
- product README
- implementation plan
- status document
- Thunderstore manifest scaffold
- root README registration
- issue #6 as the authoritative implementation/validation tracker

## Phase 1: native command seam and first playable proof

Status: **implementation candidate written; local validation pending**.

Public decompiled Valheim source shows that `Tameable` already contains the machinery Pied Piper needs:

- `m_commandable`
- native `Command` RPC registration
- tame-state gating in `Interact`
- native `MonsterAI` follow-target behavior behind the command path
- saddle state on rideable tameables

The Phase 1 candidate therefore does not implement a custom command engine. It:

1. patches `Tameable.Awake` and enables `m_commandable`;
2. leaves the actual Follow / Stay transition to Valheim;
3. uses the normal Use / interact key, matching wolves' existing interaction model;
4. temporarily suppresses commandability during interaction when a tameable currently has a saddle attached;
5. restores commandability immediately after that interaction;
6. adds local build/install/disable/enable/status/uninstall tooling.

This generic seam is deliberately preferable to per-creature patches. If current installed assemblies confirm it, boar, hen/chicken, lox, asksvin, wolf, and compatible future tameables can all use the same implementation.

### Phase 1 local validation

1. Build against the currently installed Valheim + BepInEx assemblies.
2. Correct any API drift in `Tameable`, Harmony, saddle state, or method signatures.
3. Install locally.
4. Verify boar Follow / Stay first.
5. Verify hen/chicken.
6. Verify wolf behavior is unchanged.
7. Verify lox and asksvin while unsaddled.
8. Verify an attached saddle blocks Follow / Stay switching without breaking pet/rename behavior.
9. Disable and uninstall, restart, and verify vanilla behavior is restored.

Completion gate: at least one non-wolf tameable follows and stays correctly through Valheim's native command machinery, with no custom AI or persisted dependency.

## Phase 2: supported-creature validation matrix

If Phase 1 proves the generic seam, Phase 2 is primarily validation rather than additional architecture.

- wolf: confirm no regression
- boar: Follow / Stay
- hen/chicken: Follow / Stay
- lox: Follow / Stay while unsaddled; blocked while saddle is attached
- asksvin: Follow / Stay while unsaddled; blocked while saddle is attached
- future compatible tameables: inherit the generic behavior unless a real incompatibility is discovered

Do not create per-creature behavior classes unless the installed game proves they are necessary.

Completion gate: the same normal Use interaction produces consistent Follow / Stay semantics across the supported creature matrix.

## Phase 3: lifecycle and compatibility hardening

- verify the helper manages only Pied Piper-owned files
- confirm no save or world migration dependency
- validate interaction with vanilla rename / pet / ride behavior
- validate disabled / uninstalled behavior after restart
- smoke-test multiplayer client behavior where mods are permitted
- document exact current-game API seams relied upon
- test coexistence with Rested Whispers and Rune Compass

Completion gate: Pied Piper can be installed, used, disabled, and removed without leaving the character/world dependent on it or altering unrelated plugins.

## Phase 4: release polish

- add mod-specific icon
- verify manifest metadata
- document the final supported creature matrix
- record known compatibility constraints
- package locally
- close issue #6 only after in-game validation is complete

## Architecture

The discovery work collapsed the original proposed architecture into something much smaller:

```text
Plugin
  └── Harmony lifecycle

TameablePatches
  ├── Awake: expose native commandability
  └── Interact: saddle guard

Valheim
  └── Tameable Command RPC + MonsterAI follow state
```

That is the preferred shape. Pied Piper should not own target acquisition, pathfinding, or a duplicate follow-state machine when Valheim already has them.

## Safety and scope boundaries

- No GitHub Actions.
- No custom save schema.
- No world mutation beyond normal creature AI state already used by Valheim.
- No custom pathfinding unless the native command path is proven unusable.
- No teleport behavior.
- No global mass-command system in v0.1.
- No pet stat, combat, breeding, or inventory changes.
- Never override saddle/riding authority.
- Installed Valheim assemblies are authoritative.
