# Celestial Dial status

## Current state

**The primitive read-only runtime and persistent instrument-toggle candidates are implemented. Local compile and in-game acceptance are still required before calling Celestial Dial playable.**

Completed in the repository:

- product name locked as **Celestial Dial**
- Sól and Máni visual theme established
- semantic rule locked: **Sól on the upper/day half, Máni on the lower/night half**
- product scope limited to world day plus continuous day/night-cycle position
- presentation-only skin architecture defined
- corrected concept art stored under `Assets/Concept/`
- `discover-time-api.ps1` read-only installed-assembly probe
- BepInEx plugin/project boundary and local lifecycle helper
- fail-closed candidate time source
- primitive circular day/cycle HUD
- standalone persistent top-right instrument toggle candidate
- conservative small-minimap visibility adapter that restores prior state
- optional Rune Compass presentation interop with no compile-time dependency

## Runtime proof candidate

The time candidate resolves two historical `EnvMan` members through reflection:

- `EnvMan.GetCurrentDay()`
- `EnvMan.m_smoothDayFraction`

Those member names are still candidates, not accepted Valheim 1.0 semantics. The provider refuses to render if either member is absent, returns the wrong type, throws, or produces a fraction outside the expected normalized range.

The primitive HUD renders:

- `DAY N` in the center;
- Sól on the upper half;
- Máni on the lower half;
- dawn/dusk transition labels on the left/right axis;
- a moving perimeter marker for the normalized cycle fraction.

The current candidate mapping puts fraction `0.5` at the top, `0.0/1.0` at the bottom, `0.25` at the left transition, and `0.75` at the right transition. In-game observation must confirm those semantics before they are frozen.

## Persistent toggle candidate

The toggle is its own HUD surface rather than a child of the minimap. That is the key No Map invariant.

Current implementation behavior:

- with a normal minimap, selecting Celestial Dial conservatively resolves the small minimap root, remembers whether it was active, hides it, and restores that exact state when Celestial Dial is deselected;
- in No Map play, the toggle remains independent of the absent minimap surface;
- when Rune Compass is loaded, Celestial Dial discovers its optional `RuneCompassInterop` bridge through reflection and suppresses only Rune Compass presentation while the dial is selected;
- Rune Compass has no compile-time dependency on Celestial Dial and Celestial Dial has no compile-time dependency on Rune Compass;
- if Rune Compass is absent, interop is a safe no-op;
- if Rune Compass appears loaded but does not expose the compatible bridge, the selection fails closed rather than leaving competing instruments visible;
- leaving the world or disposing Celestial Dial restores any minimap/Rune Compass presentation state it owns.

The implementation deliberately does not destroy the minimap, change map data, alter Rune Compass configuration, or mutate gameplay state.

## Current gate: local Valheim 1.0 validation

Run from the repository root:

```powershell
.\CelestialDial\discover-time-api.ps1 -OutFile ".\celestial-dial-api-discovery.txt"
.\CelestialDial\build-local.ps1
.\RuneCompass\build-local.ps1
.\CelestialDial\build-local.ps1 -Install
.\RuneCompass\build-local.ps1 -Install
```

Then execute [TOGGLE_TEST.md](TOGGLE_TEST.md) and record the results.

### Time evidence still required

- assembly SHA-256 from the discovery report
- exact resolved signatures for the day/fraction candidates
- clean BepInEx plugin load
- displayed day versus Valheim's own day announcement
- fraction/marker progression through dawn, midday, dusk, midnight, and next dawn
- sleep/time-skip behavior
- unload/reload behavior

### Toggle evidence still required

- exact small-minimap field resolved on the installed Valheim 1.0 build
- normal-map map → dial → map restoration
- repeated switching without duplicate controls or callbacks
- No Map toggle remains present with no minimap
- No Map + Rune Compass switches Rune Compass ↔ Celestial Dial correctly
- world leave/re-entry restores the expected surface
- disabling/uninstalling Celestial Dial leaves vanilla/Rune Compass presentation intact

## Risk posture

Celestial Dial remains **read-only**. The toggle changes presentation visibility only. It does not write save data, world data, ZDOs, map data, environment state, network state, or Rune Compass configuration.

## Evidence required before calling it playable

- local compile succeeds for both touched mods
- repository verification passes
- plugin loads in Valheim 1.0
- current world day matches the game
- cycle indicator tracks a full day/night transition
- sleep/time skip behavior is correct
- minimap/dial toggle survives repeated use and HUD recreation
- No Map/Rune Compass switching keeps the toggle available
- no gameplay-state mutation is introduced
- disable/uninstall restores vanilla and sibling UI behavior cleanly
