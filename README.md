# WulfPack Mods

A monorepo for small, focused Valheim mods developed under the WulfPack name.

## Repository structure

Each mod lives in its own top-level folder and should remain independently buildable, installable, removable, packageable, and releasable.

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
- no expiry message because Valheim already announces that itself
- native Valheim messages
- client-side only
- BepInEx 5
- no world-state changes
- one-command install, disable, re-enable, status, and uninstall workflow

See [`RestedWhispers/README.md`](RestedWhispers/README.md) for the full install / disable / re-enable / uninstall workflow and the procedure for verifying the mod is no longer loaded before joining a server that prohibits third-party mods.

### Rune Compass

A lightweight, immersive compass for No Map play. Rune Compass gives the player directional orientation without turning No Map into a minimap or GPS system.

Initial design target:

- cardinal direction and player heading
- wind direction as a first-class indicator
- interchangeable visual skins
- No Map aware by default
- configurable visibility, scale, position, and opacity
- client-side only where practical
- no map markers, route guidance, player tracking, or hidden world information
- no save or world-state dependency for the initial release
- clean local install, disable, re-enable, status, and uninstall workflow

The product rule is simple: **Rune Compass gives you direction, not information.**

See [`RuneCompass/README.md`](RuneCompass/README.md) for the current design and implementation boundary.

## Design rules

- Prefer small, well-bounded mods over sprawling feature bundles.
- Avoid save mutation unless the feature genuinely requires it.
- Treat multiplayer synchronization and world persistence as explicit complexity boundaries.
- Prefer native Valheim UI and behavior where practical.
- Keep each mod independently removable without damaging a vanilla character or world whenever possible.
- New features should teach one meaningful new class of integration without dragging in unrelated complexity.

## Build and validation policy

**GitHub Actions are prohibited in this repository. The GitHub Actions budget is zero.**

Do not add `.github/workflows`, hosted CI jobs, scheduled Actions, release Actions, or Actions-based validation. Builds, tests, packaging, and in-game validation are performed locally unless an explicitly approved non-Actions mechanism is introduced later.

Each mod should provide its own local build instructions or helper scripts so validation remains reproducible without hosted CI.

Mods target the **installed** game, never historical API signatures. Interface contracts are verified against the assemblies actually present on the machine before they are relied upon.

## Status

Rested Whispers is the first validated proof-of-capability mod. Rune Compass is the second project and the first WulfPack mod intended to introduce custom HUD rendering, orientation logic, asset-driven skins, and wind-direction visualization while preserving clean removal and a narrow client-side scope.
