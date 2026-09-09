# Celestial Dial status

## Current state

**Framework and concept slice established. Gate 0 discovery tooling is now implemented; runtime implementation has not started.**

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
- intended repository risk tier recorded as **read-only**
- `discover-time-api.ps1` added as a read-only installed-assembly probe for `EnvMan`, `Hud`, and `Minimap`

## Current gate: authoritative Valheim 1.0 API discovery

Run:

```powershell
.\CelestialDial\discover-time-api.ps1 -OutFile ".\celestial-dial-api-discovery.txt"
```

The probe records the installed `assembly_valheim.dll` SHA-256 and exact matching member signatures. It specifically reports whether historical candidates such as `EnvMan.GetCurrentDay` and `m_smoothDayFraction` still exist without treating their presence as proof of semantics.

### Still required before Gate 0 can close

- run the probe against the user's installed Valheim 1.0 build
- record the authoritative current-day seam
- record the authoritative normalized cycle-position seam
- verify selected values in-game through day/night progression and a sleep/time skip
- confirm stable HUD/minimap anchoring and lifecycle
- confirm multiplayer clients already receive the required world-time state

No runtime code should freeze a historical member name until those checks are complete.

## Persistent toggle contract

The toggle is a standalone HUD affordance near the upper-right of the minimap/instrument region.

- map-enabled world: it will switch minimap ↔ Celestial Dial without destroying minimap state
- No Map + Rune Compass: it will switch Rune Compass ↔ Celestial Dial
- the toggle itself remains visible while either instrument surface is active
- Rune Compass and Celestial Dial remain independently installable
- optional sibling-mod interop must fail safely when the other mod is absent

This contract is documented but **not yet implemented**.

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
