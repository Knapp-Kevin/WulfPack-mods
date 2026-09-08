# Rune Compass implementation plan

## Objective

Graduate from Rested Whispers into a second mod that adds one meaningful new class of integration: a persistent custom HUD driven by player orientation and live wind state, while remaining cleanly removable and avoiding save/world persistence.

## Phase 1: technical proof

1. Create the BepInEx plugin project against the installed Valheim assemblies.
2. Render one compass face using one approved skin asset set.
3. Read player or camera heading from the live game.
4. Rotate the heading indicator correctly through 360 degrees.
5. Verify cardinal directions against known world orientation.
6. Add the same local install / disable / enable / status / uninstall ergonomics proven by Rested Whispers.

Completion gate: one skin renders correctly, heading is accurate, and uninstall returns the install to its prior state without save/world impact.

## Phase 2: No Map visibility and wind

1. Detect current No Map state from the installed game's actual API.
2. Show Rune Compass only in No Map mode by default.
3. Add a configuration override for always-visible behavior.
4. Identify Valheim's authoritative live wind vector from installed assemblies/runtime behavior.
5. Render a visually distinct wind indicator.
6. Define and test the convention as wind direction **toward**, not meteorological source direction.

Completion gate: heading and wind are both correct and cannot be visually confused.

## Phase 3: interchangeable skins

1. Extract visual asset selection from compass behavior.
2. Implement a small `SkinDefinition` model.
3. Load the selected skin from normal BepInEx configuration.
4. Validate at least two complete skins against the same behavior implementation.
5. Add scale, position, and opacity configuration.

Completion gate: changing skins changes appearance only. No navigation or gameplay logic is skin-specific.

## Phase 4: polish and release validation

- curate final initial skins
- package icon and Thunderstore metadata
- test several resolutions/aspect ratios
- test UI scaling
- smoke-test multiplayer client behavior where mods are permitted
- verify disable/uninstall before joining no-third-party-mod servers
- document exact local validation evidence

## Architecture target

```text
Plugin
  ├── configuration
  └── lifecycle

CompassController
  ├── HeadingProvider
  ├── WindProvider
  └── visibility rules

CompassUI
  ├── heading indicator
  ├── wind indicator
  └── SkinRenderer

SkinLoader
  └── SkinDefinition
```

This is a target, not a commandment. Keep the implementation smaller if the installed Valheim APIs make a simpler design sufficient.

## Safety and scope boundaries

- No GitHub Actions.
- No save mutation in v0.1.
- No world mutation in v0.1.
- No server authority in v0.1 unless current APIs prove a client-only implementation invalid.
- No minimap or marker data.
- No hidden-location discovery.
- No progression or physical-item system until the compass itself is proven.
- Treat installed Valheim assemblies as authoritative and document API drift rather than guessing from old mod source.
