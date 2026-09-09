# Celestial Dial persistent toggle acceptance protocol

This protocol validates the presentation-only instrument switch introduced for Celestial Dial and optional Rune Compass interop. It is a human in-game acceptance pass because the behavior depends on the live Valheim HUD and No Map state.

## Preconditions

1. Use the same installed Valheim 1.0 build recorded by `discover-time-api.ps1`.
2. Quit Valheim before installing plugin files.
3. From the repository root, build and install both candidates:

```powershell
.\RuneCompass\build-local.ps1
.\CelestialDial\build-local.ps1
.\RuneCompass\build-local.ps1 -Install
.\CelestialDial\build-local.ps1 -Install
```

4. Confirm both plugins report clean load lines in `BepInEx/LogOutput.log`.
5. Do not treat screenshots as proof for rows whose success depends on restoration after a transition. Record the observed result in the table.

## Acceptance rows

| # | Scenario | Expected result | Result |
| --- | --- | --- | --- |
| T1 | Normal map world, initial entry | Normal minimap is visible. Persistent instrument toggle is visible near the upper-right HUD region. Celestial Dial is not selected by default. | pending |
| T2 | Normal map: select Celestial Dial | Small minimap hides, Celestial Dial appears, toggle remains visible. No large-map or map-data behavior changes. | pending |
| T3 | Normal map: return from Celestial Dial | Celestial Dial hides and the minimap returns to exactly the active state it had before T2. | pending |
| T4 | Repeat map ↔ dial 20 times | Exactly one toggle exists. No duplicate callbacks, stacked dial objects, flicker, or increasing input response. | pending |
| T5 | Enter No Map world with Celestial Dial only | Toggle remains visible despite the minimap being absent. Selecting/deselecting the dial never creates a minimap. | pending |
| T6 | No Map with Rune Compass + Celestial Dial | Rune Compass is initially visible. Selecting Celestial Dial hides Rune Compass, shows the dial, and keeps the toggle visible. | pending |
| T7 | No Map: return to Rune Compass | Celestial Dial hides and Rune Compass resumes its own normal visibility/configuration behavior. | pending |
| T8 | Rune Compass absent | Celestial Dial toggle and dial work without errors or a hard dependency on Rune Compass. | pending |
| T9 | Time source unavailable or rejected | Attempting to select Celestial Dial fails closed, restores minimap/Rune Compass presentation, and does not strand the player with an empty HUD region. | pending |
| T10 | Leave world while dial selected | During world exit the dial and toggle disappear and any minimap/Rune Compass suppression owned by Celestial Dial is released. | pending |
| T11 | Re-enter a world after T10 | Toggle returns once a local player exists. If dial selection persisted, the current world surfaces are reacquired cleanly; otherwise the normal instrument remains. No duplicate toggle appears. | pending |
| T12 | Disable Celestial Dial, restart | Rune Compass and/or vanilla minimap behave normally with no dependency on Celestial Dial. | pending |
| T13 | Re-enable Celestial Dial, restart | Toggle returns exactly once and switching works as before. | pending |
| T14 | Uninstall Celestial Dial, restart | Vanilla minimap and Rune Compass behave normally. No Celestial Dial object, config dependency, or presentation suppression remains. | pending |

## Minimap seam evidence

Record the exact installed member used for the small minimap surface:

```text
assembly SHA-256:
Minimap type:
small-root field:
field type:
```

The implementation candidate currently accepts `m_smallRoot` or `_smallRoot`. If the installed build exposes neither, do not broaden reflection by guessing hierarchy names during the test. Treat T2 as failed and update the discovery evidence first.

## Failure interpretation

- A missing toggle in No Map is a product failure, not a cosmetic issue.
- A minimap that fails to return to its prior state is a restoration failure.
- Rune Compass remaining visible under Celestial Dial is an interop failure.
- Rune Compass failing to return after deselection is an interop restoration failure.
- Multiple toggle controls or multiple responses per click are lifecycle failures.
- Any save/world/map-data mutation is a risk-tier violation and blocks acceptance.

## Exit gate

Gate 2 is accepted only when T1 through T14 pass on the installed Valheim 1.0 build and the observed minimap seam is recorded in `STATUS.md`.
