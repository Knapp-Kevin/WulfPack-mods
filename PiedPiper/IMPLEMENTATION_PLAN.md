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

Status: **revised implementation candidate written; local validation pending**.

Public decompiled Valheim source shows that `Tameable` already contains the machinery Pied Piper needs:

- native `Command` RPC registration
- tame-state gating
- native `MonsterAI` follow-target behavior behind the command path
- saddle state on rideable tameables

The first candidate enabled `m_commandable` globally. Review rejected that shape because it changes normal pet interaction on non-wolf tameables.

The revised candidate instead:

1. leaves vanilla `Tameable.Interact` untouched;
2. binds one configurable Pied Piper command key, default `G`;
3. raycasts a short configurable distance from the player camera;
4. accepts only a tamed `Tameable` target;
5. resolves the native private `Tameable.Command` method reflectively;
6. invokes that native command path directly;
7. rejects rideable tameables while a saddle is attached;
8. fails closed if saddle state cannot be verified;
9. leaves pathfinding and Follow / Stay state entirely to Valheim;
10. adds local build/install/disable/enable/status/uninstall tooling.

This keeps Pied Piper additive. It does not replace petting, rename, wolf interaction, saddle behavior, or the game's own AI.

### Phase 1 local validation

1. Build against the currently installed Valheim + BepInEx assemblies.
2. Correct any API drift in `Tameable.Command`, `HaveSaddle`, Unity raycast APIs, `KeyboardShortcut`, or component layout.
3. Install locally.
4. Verify boar Follow / Stay first with the Pied Piper key.
5. Verify normal boar petting remains vanilla.
6. Verify hen/chicken.
7. Verify wolf vanilla interaction is unchanged and the Pied Piper key also reaches the native command path.
8. Verify lox and asksvin while unsaddled.
9. Verify an attached saddle blocks Pied Piper commands.
10. Disable and uninstall, restart, and verify vanilla-only behavior is restored.

Completion gate: at least one non-wolf tameable follows and stays correctly through Valheim's native command machinery, with vanilla pet interaction preserved and no custom AI or persisted dependency.

## Phase 2: supported-creature validation matrix

If Phase 1 proves the generic seam, Phase 2 should remain mostly validation rather than new architecture.

- wolf: vanilla interaction unchanged; Pied Piper key works
- boar: Follow / Stay
- hen/chicken: Follow / Stay
- lox: Follow / Stay while unsaddled; blocked while saddle is attached
- asksvin: Follow / Stay while unsaddled; blocked while saddle is attached
- future compatible tameables: inherit the generic behavior unless a real incompatibility is discovered

Do not create per-creature behavior classes unless the installed game proves they are necessary.

Completion gate: the same Pied Piper key produces consistent Follow / Stay semantics across the supported creature matrix while normal Valheim interaction remains untouched.

## Phase 3: lifecycle and compatibility hardening

- verify the helper manages only Pied Piper-owned files
- confirm no save or world migration dependency
- validate vanilla rename / pet / ride behavior
- validate disabled / uninstalled behavior after restart
- smoke-test multiplayer client behavior where mods are permitted
- document exact current-game API seams relied upon
- test coexistence with Rested Whispers and Rune Compass
- verify command-key rebinding and command-distance configuration

Completion gate: Pied Piper can be installed, used, disabled, and removed without leaving the character/world dependent on it or altering unrelated plugins.

## Phase 4: release polish

- add mod-specific icon
- verify manifest metadata
- document the final supported creature matrix
- record known compatibility constraints
- package locally
- close issue #6 only after in-game validation is complete

## Architecture

The preferred architecture is intentionally small:

```text
Plugin
  ├── config: enabled / command key / command distance
  └── Update: short target raycast

NativeFollowCommand
  ├── tame eligibility
  ├── fail-closed saddle guard
  └── reflective native Command invocation

Valheim
  └── Tameable Command RPC + MonsterAI follow state
```

Pied Piper should not own pathfinding, duplicated follow state, or per-creature AI when Valheim already has those systems.

## Safety and scope boundaries

- No GitHub Actions.
- No custom save schema.
- No world mutation beyond normal creature AI state already used by Valheim.
- No custom pathfinding unless the native command path is proven unusable.
- No teleport behavior.
- No global mass-command system in v0.1.
- No pet stat, combat, breeding, or inventory changes.
- Never override saddle/riding authority.
- Preserve vanilla pet and rename interaction.
- Installed Valheim assemblies are authoritative.
