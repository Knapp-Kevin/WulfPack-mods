# WulfPack Mods

A monorepo for small, focused Valheim mods developed under the WulfPack name.

## Repository structure

Each mod lives in its own top-level folder and should remain independently buildable, packageable, and releasable.

```text
WulfPack-mods/
├── RestedWhispers/
├── RuneCompass/
└── README.md
```

Shared tooling may be added only when it clearly reduces duplication without coupling otherwise independent mods.

## Current mods

### Rested Whispers

A lightweight client-side quality-of-life mod that gently warns the player as the existing Rested effect begins to fade. It does not add a Tired debuff, alter balance, modify saves, or change multiplayer state.

Current behavior:

- configurable first warning before Rested expires
- configurable final warning
- no duplicate expiry message because Valheim already announces that itself
- native Valheim messages
- client-side only
- BepInEx 5
- no save or world-state changes
- local install / disable / enable / status / uninstall workflow

Rested Whispers has been built, tested, validated in-game, and accepted as the first completed WulfPack mod proof of capability. See [`RestedWhispers/README.md`](RestedWhispers/README.md) for usage and server-safety instructions.

### Rune Compass

Rune Compass is the second resident mod and the next complexity step. It is intended as an immersive navigation aid for No Map play without becoming a minimap or GPS overlay.

Initial target:

- cardinal orientation and player heading
- wind direction as a first-class, visually distinct signal
- wind shown by default as the direction it is blowing toward
- interchangeable presentation-only skins
- planned initial skin families: Classic Wood, Rune Ring, Minimal Nordic
- configurable visibility, position, scale, and opacity
- No Map aware by default
- no save/world persistence for v0.1
- clean local install / disable / enable / status / uninstall workflow

Rune Compass follows the rule: **direction, not hidden information.** It does not initially include map pins, route guidance, boss/trader tracking, player tracking, hidden-location discovery, or server-side state.

See [`RuneCompass/README.md`](RuneCompass/README.md), [`RuneCompass/IMPLEMENTATION_PLAN.md`](RuneCompass/IMPLEMENTATION_PLAN.md), and [`RuneCompass/STATUS.md`](RuneCompass/STATUS.md).

## Design rules

- Prefer small, well-bounded mods over sprawling feature bundles.
- Avoid save mutation unless the feature genuinely requires it.
- Treat multiplayer synchronization and world persistence as explicit complexity boundaries.
- Prefer native Valheim UI and behavior where practical.
- Keep each mod independently removable without damaging a vanilla character or world whenever possible.
- Keep presentation systems such as Rune Compass skins separate from gameplay logic.

## Build and validation policy

**GitHub Actions are prohibited in this repository. The GitHub Actions budget is zero.**

Do not add `.github/workflows`, hosted CI jobs, scheduled Actions, release Actions, or Actions-based validation. Builds, tests, packaging, and in-game validation are performed locally unless an explicitly approved non-Actions mechanism is introduced later.

Each mod should provide its own local build instructions or helper scripts so validation remains reproducible without hosted CI.

Mods target the **installed** game, never historical API signatures. Interface contracts are verified against the assemblies actually present on the machine before they are relied upon.

## Status

- **Rested Whispers:** implemented, tested, validated, and accepted.
- **Rune Compass:** scaffolded and ready for implementation.
