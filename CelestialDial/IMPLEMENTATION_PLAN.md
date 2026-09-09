# Celestial Dial implementation plan

## Goal

Implement a client-side, read-only BepInEx mod that shows the current world day and continuous day/night-cycle position in a toggleable circular surface anchored to the minimap region.

## Gate 0: authoritative API discovery

Verify against the installed Valheim 1.0 assemblies:

- authoritative source for current world day
- authoritative source for normalized day/night-cycle position
- lifecycle and availability of those values during menu, loading, world entry, sleep, portal travel, and disconnect
- authoritative minimap/HUD hierarchy and a stable anchoring strategy
- whether the required state is already synchronized correctly for multiplayer clients

Do not freeze older `EnvMan` assumptions without checking the current assemblies.

### Discovery probe

Run from the repository root:

```powershell
.\CelestialDial\discover-time-api.ps1 -OutFile ".\celestial-dial-api-discovery.txt"
```

The probe records the installed `assembly_valheim.dll` SHA-256 and enumerates exact matching members on `EnvMan`, `Hud`, and `Minimap`. It explicitly reports whether historical `GetCurrentDay` and `m_smoothDayFraction` candidates still exist, but member presence is not semantic proof.

**Implementation state:** discovery tooling is merged. A fail-closed candidate adapter is also merged.  
**Exit remains open:** installed-assembly signatures and live semantic confirmation are still required.

## Gate 1: read-only technical proof

The repository now contains a primitive runtime candidate:

- BepInEx plugin/project lifecycle
- current-day center readout
- normalized cycle marker on a primitive circular dial
- Sól on the upper/day half and Máni on the lower/night half
- no Harmony dependency
- no save, world, ZDO, environment, or network mutation
- local build/install/disable/enable/status/uninstall workflow

**Implementation state:** complete in repository.  
**Exit remains open:** local build/load/lifecycle containment and in-game time semantics must pass.

## Gate 2: persistent instrument toggle

The toggle is its own HUD affordance rather than a child of the minimap so it can survive No Map play.

The implementation candidate now includes:

- standalone top-right toggle surface
- normal-map suppression through a conservative small-minimap adapter
- preservation/restoration of the minimap's prior active state
- no minimap destruction or map-data mutation
- optional Rune Compass presentation suppression through a public reflection bridge
- no compile-time dependency between Rune Compass and Celestial Dial
- safe no-op when Rune Compass is absent
- fail-closed behavior when a loaded sibling exposes no compatible bridge
- restoration of owned UI state on deselection, world exit, and disposal

Rune Compass exposes only one interop capability: external presentation suppression. Its normal visibility/configuration logic remains authoritative when suppression is released.

**Implementation state:** candidate implemented.  
**Exit remains open:** run [TOGGLE_TEST.md](TOGGLE_TEST.md) against Valheim 1.0 and prove normal-map, No Map, Rune Compass, repeated-toggle, and world-transition behavior.

## Gate 3: semantic dial

Introduce the stable component model:

- cycle ring
- Sól/day treatment on the upper half
- Máni/night treatment on the lower half
- dawn marker
- dusk marker
- celestial indicator
- centered day plate
- toggle glyph

The component semantics must exist independently of any skin.

The primitive candidate already carries these meanings structurally, but final component binding waits until Gates 0 through 2 are accepted.

**Exit:** a primitive skin can be replaced without changing timekeeping or toggle logic.

## Gate 4: skin binding

Bind presentation assets through the documented skin contract rather than hard-coding one art set.

Initial target families:

- Classic Wood
- Rune Stone
- Bronze Astrolabe
- Frostborn

Skin selection should be a user configuration option and should update without changing gameplay state.

**Exit:** at least two materially different skins render from the same semantic state and switching them does not alter behavior.

## Gate 5: acceptance

Validate:

- world day increments correctly
- cycle indicator crosses dawn, day, dusk, and night correctly
- sleep/time skips do not leave the indicator stale
- multiplayer client behavior remains correct
- map/dial toggle is responsive at common UI scales
- No Map + Rune Compass switching preserves the persistent toggle
- repeated switching creates no duplicate buttons/callbacks
- HUD/world recreation restores the correct surface
- no hidden information is introduced
- no persistent game state changes
- disable, enable, and uninstall leave vanilla and sibling UI intact

## Explicit non-goals

- time control
- time acceleration or pausing
- day-length configuration
- weather/environment control
- alarms or reminders
- modern clock time
- calendars or season systems
- map pin or navigation features
- server-authoritative behavior
