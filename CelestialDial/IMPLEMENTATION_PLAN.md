# Celestial Dial implementation plan

## Goal

Implement a client-side, read-only BepInEx mod that shows the current world day and continuous day/night-cycle position in a toggleable circular surface anchored to the minimap region.

## Gate 0: authoritative API discovery

Before implementation, verify against the installed Valheim 1.0 assemblies:

- authoritative source for current world day
- authoritative source for normalized day/night-cycle position
- lifecycle and availability of those values during menu, loading, world entry, sleep, portal travel, and disconnect
- authoritative minimap/HUD hierarchy and a stable anchoring strategy
- whether the required state is already synchronized correctly for multiplayer clients

Do not freeze older `EnvMan` assumptions without checking the current assemblies.

**Exit:** documented API seams with evidence from Valheim 1.0.

## Gate 1: read-only technical proof

Build the smallest functional surface:

- BepInEx plugin loads and unloads cleanly
- current day renders as plain text
- normalized cycle position updates continuously
- a primitive circular indicator reflects that normalized value
- no Harmony patches
- no save, world, ZDO, environment, or network mutation

**Exit:** local build/load/lifecycle containment passes and in-game values behave correctly across at least one complete day/night transition.

## Gate 2: minimap toggle

Add the product interaction without skin complexity:

- anchor a Celestial Dial panel to the minimap region
- add one small map/dial toggle control near the upper-right of that region
- preserve the minimap object and its state
- ensure repeated toggling does not leak UI objects or duplicate callbacks
- survive HUD recreation, world changes, and configuration reloads

**Exit:** map and dial switch predictably without affecting map behavior.

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

**Exit:** a primitive skin can be replaced without changing timekeeping logic.

## Gate 4: skin binding

Bind presentation assets through a skin contract rather than hard-coding one art set.

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
- no hidden information is introduced
- no persistent game state changes
- disable, enable, and uninstall leave vanilla UI and saves intact

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
