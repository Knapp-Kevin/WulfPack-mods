# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

Its job is intentionally narrow: show orientation and wind direction without becoming a minimap, GPS, route planner, or hidden-information overlay.

> Rune Compass gives you direction, not information.

## Initial release target

- Show cardinal orientation and player heading.
- Show wind direction as a distinct secondary indicator.
- Show by default only when No Map mode is active, with a configurable override.
- Support interchangeable visual skins without changing compass behavior.
- Support configurable position, scale, and opacity.
- Remain client-side where practical.
- Avoid save mutation, world-state mutation, prefabs, progression hooks, map markers, route guidance, and server authority in the first release.
- Install, disable, re-enable, inspect status, and uninstall cleanly with local tooling.
- Use zero GitHub Actions.

## Wind behavior

Wind is a first-class Rune Compass signal, not decorative polish.

The compass should clearly distinguish:

- **heading / cardinal orientation**: where the player is facing relative to north; and
- **wind direction**: where the current world wind is blowing.

The default design should visualize the direction the wind is blowing **toward**. The implementation and UI copy must make that convention explicit so the indicator cannot be mistaken for the meteorological "coming from" convention.

Heading and wind calculations should remain separate in code even if both ultimately drive rotating UI elements.

## Skin system

Skins control presentation only. They must not change gameplay behavior.

A skin may define:

- base / face texture
- outer ring texture
- heading pointer texture
- wind pointer texture
- optional north marker
- pointer pivot and visual offsets
- default visual scale or opacity where needed for alignment

The selected skin should come from normal BepInEx configuration. Runtime hot-swapping is optional for the first release; changing config and restarting is acceptable.

Planned initial skin families:

1. **Classic Wood**: carved wooden face, restrained metal framing, simple pointer.
2. **Rune Ring**: darker runic ring treatment with stronger Norse ornament.
3. **Minimal Nordic**: compact, highly readable treatment for players who want less HUD weight.

Existing compass concept art from the project discussion should be curated into these roles rather than copied wholesale into every skin.

## Proposed structure

```text
RuneCompass/
├── Plugin.cs
├── CompassController.cs
├── CompassUI.cs
├── HeadingProvider.cs
├── WindProvider.cs
├── SkinDefinition.cs
├── SkinLoader.cs
├── RuneCompass.csproj
├── build-local.ps1
├── manifest.json
├── README.md
└── Assets/
    └── Skins/
        ├── ClassicWood/
        ├── RuneRing/
        └── MinimalNordic/
```

Only create abstraction where the working implementation earns it. The first technical proof should get one skin rendering and rotating correctly before the skin loader grows teeth.

## v0.1 acceptance criteria

- [ ] Builds locally against the installed Valheim + BepInEx assemblies.
- [ ] Loads with no BepInEx plugin errors.
- [ ] Compass appears in No Map mode by default.
- [ ] Compass stays hidden in normal-map mode by default.
- [ ] Config override can show the compass outside No Map mode.
- [ ] Heading indicator rotates correctly through a full 360 degrees.
- [ ] Cardinal orientation is correct at N, E, S, and W.
- [ ] Wind indicator is visually distinct from heading.
- [ ] Wind indicator rotates correctly as Valheim wind changes.
- [ ] Display position, scale, and opacity are configurable.
- [ ] At least two skins can be selected without changing behavior logic.
- [ ] Disable suppresses the UI completely.
- [ ] Uninstall leaves no save or world dependency.
- [ ] After uninstall, Rune Compass is absent from active BepInEx plugin paths and logs.
- [ ] Other installed BepInEx plugins are untouched.
- [ ] No GitHub Actions are added or run.

## Explicitly out of scope for v0.1

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
- animated or behavior-specific skins

Those may be discussed later. They are not prerequisites for proving Rune Compass.
