# Celestial Dial status

## Current state

**Framework and concept slice established. Runtime implementation has not started.**

Completed:

- product name locked as **Celestial Dial**
- Sól and Máni visual theme established
- semantic rule locked: **Sól on the upper/day half, Máni on the lower/night half**
- product scope limited to world day plus continuous day/night-cycle position
- minimap-footprint toggle interaction defined
- presentation-only skin architecture defined
- initial concept art captured in `Assets/Concept/`
- initial skin families identified
- intended repository risk tier recorded as **read-only**

## Next gate

Authoritative Valheim 1.0 API discovery.

The implementation must not assume historical time APIs are unchanged. Verify the installed assemblies for current-day, normalized day/night-cycle state, and stable HUD/minimap anchoring before writing runtime code.

## Evidence required before calling it playable

- local compile succeeds
- plugin loads in Valheim 1.0
- current world day matches the game
- cycle indicator tracks a full day/night transition
- sleep/time skip behavior is correct
- minimap/dial toggle survives repeated use and HUD recreation
- no Harmony patch or gameplay-state mutation is introduced
- disable/uninstall restores vanilla UI behavior cleanly
