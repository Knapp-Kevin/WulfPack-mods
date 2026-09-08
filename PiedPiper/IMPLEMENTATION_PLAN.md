# Pied Piper implementation plan

## Objective

Add one consistent Follow / Stay command to eligible tamed Valheim creatures while preserving native behavior, avoiding custom AI where possible, and keeping the mod cleanly removable.

## Phase 0: repository mesh

Establish the mod as a first-class repository resident before gameplay code begins.

Deliverables:

- dedicated top-level `PiedPiper/` directory
- product README
- implementation plan
- status document
- Thunderstore manifest scaffold
- root README registration
- issue #6 as the authoritative implementation/validation tracker

Completion gate: the repository clearly defines what Pied Piper is, what it is not, and what must be proven before release.

## Phase 1: authoritative API discovery and single-creature proof

1. Inspect the currently installed Valheim assemblies.
2. Identify the live contracts involved in tame state, interaction, follow targets, and command state.
3. Determine exactly how vanilla wolves enter and leave Follow.
4. Determine whether boar, hen/chicken, lox, and asksvin already carry dormant compatible machinery or require a small adapter.
5. Identify the current mounted/saddled state contract for lox and asksvin.
6. Implement the smallest possible proof on one non-wolf tameable.
7. Confirm uninstall leaves no persisted dependency.

Preferred first proof: **boar**, unless assembly inspection reveals another creature is materially simpler.

Completion gate: one tamed non-wolf creature reliably toggles Follow / Stay using native state machinery or the narrowest justified patch.

## Phase 2: common command path

1. Extract a small eligibility check shared across supported tameables.
2. Add one configurable input command.
3. Preserve vanilla wolf behavior rather than replacing it unnecessarily.
4. Extend the proven command path to hen/chicken.
5. Extend it to lox and asksvin with mounted/saddled guards.
6. Keep wild and untamed creatures strictly out of scope.

Completion gate: the same user command produces the same Follow / Stay semantics across all supported creatures.

## Phase 3: lifecycle and compatibility hardening

- add local build/install/disable/enable/status/uninstall tooling
- verify the helper manages only Pied Piper-owned files
- confirm no save or world migration dependency
- validate interaction with vanilla rename/pet/ride behavior
- validate disabled/uninstalled behavior after restart
- smoke-test multiplayer client behavior where mods are permitted
- document exact current-game API seams relied upon

Completion gate: Pied Piper can be installed, used, disabled, and removed without leaving the character/world dependent on it.

## Phase 4: release polish

- add mod-specific icon
- verify manifest metadata
- tighten user-facing configuration wording
- document supported creature matrix
- record known compatibility constraints
- close issue #6 only after in-game validation is complete

## Architecture target

Keep the implementation smaller than the diagram if possible.

```text
Plugin
  ├── configuration / input
  └── lifecycle

FollowCommand
  ├── target acquisition
  ├── tame eligibility
  ├── mounted-state guard
  └── Follow / Stay toggle

NativeFollowAdapter
  └── Valheim tame / AI follow contracts
```

Do not create per-creature behavior classes unless current Valheim APIs genuinely require creature-specific seams.

## Safety and scope boundaries

- No GitHub Actions.
- No custom save schema.
- No world mutation beyond normal creature AI state already used by Valheim.
- No custom pathfinding unless native follow machinery proves unusable.
- No teleport behavior.
- No global mass-command system in v0.1.
- No pet stat, combat, breeding, or inventory changes.
- Never override active saddle/riding authority.
- Installed Valheim assemblies are authoritative.
