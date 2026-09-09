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

### Discovery probe

Run the repository-owned read-only probe from the repository root:

```powershell
.\CelestialDial\discover-time-api.ps1
```

If Valheim is installed somewhere the Steam/registry discovery does not find:

```powershell
.\CelestialDial\discover-time-api.ps1 -ValheimRoot "D:\SteamLibrary\steamapps\common\Valheim"
```

To keep a local evidence file without committing machine-specific output:

```powershell
.\CelestialDial\discover-time-api.ps1 -OutFile ".\celestial-dial-api-discovery.txt"
```

The probe records the installed `assembly_valheim.dll` SHA-256 and enumerates exact matching members on `EnvMan`, `Hud`, and `Minimap`. It explicitly reports whether the historical `GetCurrentDay` and `m_smoothDayFraction` candidates still exist, but **presence is not semantic proof**. The in-game pass must still establish that the selected members mean what Celestial Dial needs across a real day/night cycle and time skips.

**Exit:** documented API seams with evidence from Valheim 1.0, including the installed assembly hash and runtime semantic confirmation.

## Gate 1: read-only technical proof

Build the smallest functional surface:

- BepInEx plugin loads and unloads cleanly
- current day renders as plain text
- normalized cycle position updates continuously
- a primitive circular indicator reflects that normalized value
- no Harmony patches
- no save, world, ZDO, environment, or network mutation

**Exit:** local build/load/lifecycle containment passes and in-game values behave correctly across at least one complete day/night transition.

## Gate 2: persistent instrument toggle

Add the product interaction without skin complexity. The toggle is its own HUD affordance rather than a child of the minimap surface so it remains available when the map itself is absent.

- anchor a Celestial Dial panel to the minimap/instrument region
- add one small map/dial toggle control near the upper-right of that region
- preserve the minimap object and its state
- in a normal map world, switch between minimap and Celestial Dial
- in No Map play with Rune Compass installed, switch between Rune Compass and Celestial Dial while keeping the toggle visible
- keep Rune Compass and Celestial Dial independently installable; optional interop must fail safely when the sibling mod is absent
- ensure repeated toggling does not leak UI objects or duplicate callbacks
- survive HUD recreation, world changes, and configuration reloads

**Exit:** map/dial and Rune Compass/dial switching behave predictably without destroying sibling UI state or making the toggle disappear.

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
- No Map + Rune Compass switching preserves the persistent toggle
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
