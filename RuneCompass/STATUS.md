# Rune Compass status

## Current status

First playable technical proof implemented on `feat/rune-compass-playable`. It has **not yet been compiled or observed in-game against the local Valheim installation**, so no acceptance gate is claimed complete yet.

Implemented in this slice:

- BepInEx plugin lifecycle and configuration
- client-side primitive compass HUD
- camera heading provider
- No Map visibility using `Game.m_noMap`
- distinct heading and wind indicators
- wind provider that resolves the installed `EnvMan` wind direction API at runtime (`GetWindDir()` first, `m_windDir` fallback)
- explicit wind convention: direction the wind is blowing **toward**
- configurable enable, scale, opacity, X/Y offset, and heading calibration offset
- local build/install/disable/enable/status/uninstall helper
- no save or world mutation
- no GitHub Actions

The primitive HUD is deliberate. Final image-backed skins should be wired only after heading, wind, visibility, and lifecycle behavior are validated in-game.

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

## Next validation gate

Run locally against the installed Valheim + BepInEx assemblies:

```powershell
.\RuneCompass\build-local.ps1
.\RuneCompass\build-local.ps1 -Install
```

Then verify:

1. clean compile and plugin load
2. HUD appears in No Map mode and hides otherwise by default
3. N/E/S/W alignment through a full turn
4. wind indicator changes with live Valheim wind and uses the documented toward convention
5. display configuration
6. disable/enable lifecycle
7. uninstall leaves other plugins and world/character data untouched

Any API drift found by the local compile or runtime test should be corrected against the installed assemblies, not papered over from historical mod examples.
