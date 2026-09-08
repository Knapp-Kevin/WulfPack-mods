# Rune Compass status

## Current status

Scaffolded. No gameplay implementation has been claimed yet.

## Confirmed product decisions

- Name: Rune Compass.
- Primary use case: No Map navigation.
- Core rule: direction, not hidden information.
- Heading and cardinal orientation are required.
- Wind direction is required and treated as a first-class signal.
- Wind indicator defaults to direction the wind is blowing toward.
- Visual skins are interchangeable and presentation-only.
- Initial skin families: Classic Wood, Rune Ring, Minimal Nordic.
- Initial release should remain client-side where current Valheim APIs allow it.
- No save/world persistence for v0.1.
- No GitHub Actions.

## Next implementation gate

Build the Phase 1 technical proof from `IMPLEMENTATION_PLAN.md` against the currently installed Valheim + BepInEx assemblies. Do not implement progression, map markers, route guidance, or physical-item mechanics until the compass HUD, heading accuracy, wind behavior, skin separation, and clean-removal path are proven.
