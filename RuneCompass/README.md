# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

Its job is intentionally narrow: show orientation and wind direction without becoming a minimap, GPS, route planner, or hidden-information overlay.

> Rune Compass gives you direction, not information.

## Current implementation

The first playable technical proof is implemented and merged into the repository. Local compile and in-game validation against the installed Valheim build remain tracked in issue #4.

Implemented now:

- camera-based heading calculation
- cardinal direction display
- No Map-aware visibility through Valheim's current `Game.m_noMap` state
- live wind direction provider using `EnvMan`
- separate heading and wind indicators
- wind semantics defined as direction **toward**
- configurable enable state, scale, opacity, X/Y position, and heading calibration
- local build / install / disable / enable / status / uninstall workflow
- no save or world-state mutation
- zero GitHub Actions

The current HUD is deliberately primitive. It exists to prove mechanics before final skin assets are bound.

## Local build and install

From the repository root:

```powershell
.\RuneCompass\build-local.ps1
.\RuneCompass\build-local.ps1 -Install
```

Other lifecycle commands:

```powershell
.\RuneCompass\build-local.ps1 -Status
.\RuneCompass\build-local.ps1 -Disable
.\RuneCompass\build-local.ps1 -Enable
.\RuneCompass\build-local.ps1 -Uninstall
```

These commands are local only. This repository does not use GitHub Actions.

## Wind behavior

Wind is a first-class Rune Compass signal, not decorative polish.

The compass distinguishes:

- **heading / cardinal orientation**: where the player is facing relative to north; and
- **wind direction**: where the current world wind is blowing.

The default design visualizes the direction the wind is blowing **toward**. That convention is explicit so it cannot be mistaken for the meteorological "coming from" convention.

Heading and wind calculations remain separate in code even though both drive directional UI elements.

## Skin system

Skins control presentation only. They must not change gameplay behavior.

A skin may eventually define:

- base / face texture
- outer ring texture
- heading pointer texture
- wind pointer texture
- optional north marker
- pointer pivot and visual offsets
- default visual scale or opacity where needed for alignment

Planned initial skin families:

1. **Classic Wood**: carved wooden face, restrained metal framing, simple pointer.
2. **Rune Ring**: darker runic ring treatment with stronger Norse ornament.
3. **Minimal Nordic**: compact, highly readable treatment for players who want less HUD weight.

Existing compass concept art should be curated into these roles rather than copied wholesale into every skin.

## Structure

```text
RuneCompass/
├── Plugin.cs
├── CompassController.cs
├── CompassUI.cs
├── HeadingProvider.cs
├── WindProvider.cs
├── RuneCompass.csproj
├── build-local.ps1
├── manifest.json
├── icon.png
├── README.md
├── IMPLEMENTATION_PLAN.md
├── STATUS.md
└── Assets/
    └── Skins/
```

`SkinDefinition` and `SkinLoader` are intentionally not implemented yet. The working compass behavior should earn the abstraction before it is introduced.

## Validation checklist

- [ ] Builds locally against the installed Valheim + BepInEx assemblies.
- [ ] Loads with no BepInEx plugin errors.
- [ ] Compass appears in No Map mode by default.
- [ ] Compass stays hidden in normal-map mode by default.
- [ ] Config override can show the compass outside No Map mode.
- [ ] Heading indicator rotates correctly through a full 360 degrees.
- [ ] Cardinal orientation is correct at N, E, S, and W.
- [ ] Wind indicator is visually distinct from heading.
- [ ] Wind indicator rotates correctly as Valheim wind changes.
- [ ] Wind direction matches the documented **toward** convention.
- [ ] Display position, scale, and opacity are configurable.
- [ ] Disable suppresses the UI completely.
- [ ] Enable restores it cleanly.
- [ ] Uninstall leaves no save or world dependency.
- [ ] After uninstall, Rune Compass is absent from active BepInEx plugin paths and logs.
- [ ] Other installed BepInEx plugins are untouched.
- [ ] No GitHub Actions are added or run.

## Explicitly out of scope for the current slice

- minimap replacement
- map pins or markers
- boss / trader / player tracking
- route guidance
- home bearing
- Vegvisir bearing
- progression gating
- physical equippable compass item
- multiplayer config synchronization
- server-side state
- world/save persistence
- final multi-skin implementation

Those may be considered later. They are not prerequisites for proving Rune Compass.
