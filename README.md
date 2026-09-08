# WulfPack Mods

A monorepo for small, focused Valheim mods developed under the WulfPack name.

## Repository structure

Each mod lives in its own top-level folder and should remain independently buildable, packageable, and releasable.

```text
WulfPack-mods/
├── RestedWhispers/
├── <FutureMod>/
└── README.md
```

Shared tooling may be added only when it clearly reduces duplication without coupling otherwise independent mods.

## Current mods

### Rested Whispers

A lightweight client-side quality-of-life mod that gently warns the player as the existing Rested effect begins to fade. It does not add a Tired debuff, alter balance, modify saves, or change multiplayer state.

Initial target:

- configurable first warning before Rested expires
- configurable final warning
- expiration notification
- native Valheim messages
- client-side only
- BepInEx 5
- no custom assets or world-state changes

It is also trivially reversible. One command installs it, one disables it
without deleting anything, and one removes it completely — deleting only its own
three paths and never touching another BepInEx plugin. See
[`RestedWhispers/README.md`](RestedWhispers/README.md) for the full
install / disable / re-enable / uninstall workflow and the procedure for
verifying you are mod-free before joining a server that prohibits mods.

## Design rules

- Prefer small, well-bounded mods over sprawling feature bundles.
- Avoid save mutation unless the feature genuinely requires it.
- Treat multiplayer synchronization and world persistence as explicit complexity boundaries.
- Prefer native Valheim UI and behavior where practical.
- Keep each mod independently removable without damaging a vanilla character or world whenever possible.

## Build and validation policy

**GitHub Actions are prohibited in this repository. The GitHub Actions budget is zero.**

Do not add `.github/workflows`, hosted CI jobs, scheduled Actions, release Actions, or Actions-based validation. Builds, tests, packaging, and in-game validation are performed locally unless an explicitly approved non-Actions mechanism is introduced later.

Each mod should provide its own local build instructions or helper scripts so validation remains reproducible without hosted CI.

Mods target the **installed** game, never historical API signatures. Interface
contracts are verified against the assemblies actually present on the machine
before they are relied upon.

## Status

This repository is experimental. Rested Whispers is the first proof-of-capability project for establishing the WulfPack Valheim mod development, testing, packaging, and maintenance workflow.
