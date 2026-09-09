# Celestial Dial status

## Current state

**A first read-only runtime proof candidate is implemented. Local compile and in-game acceptance are still required before calling it playable.**

Completed:

- product name locked as **Celestial Dial**
- Sól and Máni visual theme established
- semantic rule locked: **Sól on the upper/day half, Máni on the lower/night half**
- product scope limited to world day plus continuous day/night-cycle position
- minimap-footprint toggle interaction defined
- persistent toggle requirement defined for both map and No Map/Rune Compass play
- presentation-only skin architecture defined
- initial concept art captured in `Assets/Concept/`
- initial skin families identified
- repository risk tier recorded as **read-only**
- `discover-time-api.ps1` added as a read-only installed-assembly probe for `EnvMan`, `Hud`, and `Minimap`
- `CelestialDial.csproj` and BepInEx plugin lifecycle added
- local build/install/disable/enable/status/uninstall helper added
- fail-closed candidate time source added
- primitive circular day/cycle HUD added

## Runtime proof candidate

The candidate uses reflection for two long-lived historical members:

- `EnvMan.GetCurrentDay()`
- `EnvMan.m_smoothDayFraction`

That is deliberately not the same as declaring them authoritative for Valheim 1.0. The provider resolves them at runtime and refuses to render if either member is absent, returns the wrong type, throws, or produces a cycle value outside the expected normalized range.

The primitive HUD currently renders:

- `DAY N` in the center;
- Sól on the upper half;
- Máni on the lower half;
- dawn/dusk transition labels on the left/right axis;
- a moving perimeter marker for the normalized cycle fraction.

The current candidate mapping puts fraction `0.5` at the top, `0.0/1.0` at the bottom, `0.25` at the left transition, and `0.75` at the right transition. In-game observation must confirm those semantics before the mapping is frozen.

## Current gate: local Valheim 1.0 validation

Run:

```powershell
.\CelestialDial\discover-time-api.ps1 -OutFile ".\celestial-dial-api-discovery.txt"
.\CelestialDial\build-local.ps1
.\CelestialDial\build-local.ps1 -Install
```

Then load a world and record:

- assembly SHA-256 from the discovery report;
- exact resolved signatures for the day/fraction candidates;
- clean BepInEx plugin load;
- displayed day versus Valheim's own day announcement;
- fraction/marker progression through dawn, midday, dusk, midnight, and the next dawn;
- sleep/time-skip behavior;
- unload/reload behavior.

### Still required before the primitive proof is accepted

- local compile succeeds against the installed Valheim 1.0 and BepInEx assemblies
- plugin loads without Celestial Dial errors
- current world day matches the game
- cycle marker semantics match observed day/night progression
- sleep/time skips do not leave stale state
- disable/enable/uninstall lifecycle works cleanly
- repository read-only claim remains true

## Persistent toggle contract

The toggle is a standalone HUD affordance near the upper-right of the minimap/instrument region.

- map-enabled world: it will switch minimap ↔ Celestial Dial without destroying minimap state
- No Map + Rune Compass: it will switch Rune Compass ↔ Celestial Dial
- the toggle itself remains visible while either instrument surface is active
- Rune Compass and Celestial Dial remain independently installable
- optional sibling-mod interop must fail safely when the other mod is absent

This contract is **not yet implemented**. The primitive proof intentionally does not hide the minimap or reach into Rune Compass before the installed HUD/minimap seam is validated.

## Next implementation tranche after local proof

1. freeze the accepted Valheim 1.0 time seam and cycle mapping;
2. resolve the installed `Minimap` small-HUD root and HUD recreation lifecycle;
3. implement the persistent top-right toggle as its own HUD object;
4. preserve and restore the minimap's prior visibility state rather than destroying it;
5. add optional Rune Compass interop without creating a hard dependency;
6. validate repeated switching in normal-map and No Map play.

## Evidence required before calling it playable

- local compile succeeds
- plugin loads in Valheim 1.0
- current world day matches the game
- cycle indicator tracks a full day/night transition
- sleep/time skip behavior is correct
- minimap/dial toggle survives repeated use and HUD recreation
- No Map/Rune Compass switching keeps the toggle available
- no Harmony patch or gameplay-state mutation is introduced
- disable/uninstall restores vanilla UI behavior cleanly
