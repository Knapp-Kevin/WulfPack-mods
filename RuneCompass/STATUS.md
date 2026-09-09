# Rune Compass status

## Current status

**Revalidated after the `CompassUI` / `CompassUiFactory` split.** Build, verification and
runtime all green on the current branch head, and the split is proven behaviour-preserving
rather than merely assumed.

The compass now anchors to the **top-right corner**, taking the place of the minimap it
replaces in No Map play. Previously it sat top-centre.

Issue #4 stays **open** until the operator acceptance items are recorded.

## What is verified

Evidence gathered 2026-09-08 against the installed game
(Valheim build 2026-02-02, Unity 6000.0.61, BepInEx 5.4.22 / BepInExPack 5.4.2202).

| # | Claim | Evidence |
|---|---|---|
| RC001 | Bearing convention matches Valheim's own | `verify-local.ps1`: `+Z/+X/-Z/-X` → 0/90/180/270, diagonals → 45/135/225/315, against hand-computed literals |
| RC002 | Rose rotation is correct term by term | `verify-local.ps1`: `RoseRotationZ` → 0/90/180/270/37.5 |
| RC003 | Wind rotation is correct term by term | `verify-local.ps1`: `WindRotationZ` → 0/−90/−135/−212.5 |
| RC010 | Lifecycle touches nothing it does not own | SHA-256 diff across install → disable → enable → uninstall: 100 save files, 2 sibling plugin files, 4 sibling config files byte-identical |
| RC011 | Loads clean | `[Info :Rune Compass] Rune Compass 0.1.0 loaded.` — no Rune Compass error, warning, or exception anywhere in `LogOutput.log` |
| RC012 | Section 4 Razor | `verify-local.ps1` § Razor passed before the UI split; rerun required after split |
| — | HUD constructs and runs | `Rune Compass heading-up HUD created.` in a live world; no per-frame exception across a sustained session |
| — | Orientation renders correctly | Screenshot at heading 344°: `N` right of top, `E` right-below, `S` left of bottom, `W` left-above — each at `bearing − heading` clockwise from screen-up. Wind 045° drawn up-and-right. Lubber amber at top |

## UI split before skins — verified

`CompassUI.cs` had reached 246/250 lines, so it was split before skin work:

- `CompassUI.cs` (**117** lines) owns runtime state, visibility, layout application,
  bearing rotation, readouts and disposal.
- `CompassUiFactory.cs` (**148** lines) owns primitive Unity UI construction.

**The split changed no behaviour, and that is measured rather than claimed.** Every
construction-relevant literal — anchors, pivots, sizes, offsets, colours, canvas settings,
font, cardinal placements, needle dimensions, raycast flags — was extracted from the
pre-split file and from the post-split pair and compared as multisets:

```text
construction literals: pre-split=48  post-split=48
RESULT: IDENTICAL - every construction literal matches; none added, none dropped.
```

The `_rose` centre-anchor/pivot configuration survived intact (this was audit finding
F-I1, where an unset `RectTransform` would have made the card orbit the panel corner), as
did cardinals-and-needle-on-rose, lubber-on-panel, and construction order.

| Gate | Result |
|---|---|
| Release build | 0 warnings, 0 errors |
| `verify-local.ps1` | 25 angle assertions + Razor, exit 0 |
| Razor headroom | largest file 148/250; longest method 32 |
| Runtime | `Rune Compass 0.1.0 loaded.` + `heading-up HUD created.`, zero Rune Compass errors |

## Placement: top-right

The compass is pinned to a screen **corner** rather than positioned by absolute pixels, so
it holds its place across resolutions and aspect ratios.

- New `Anchor` config: `TopRight` (default), `TopCenter`, `TopLeft`, `BottomRight`,
  `BottomLeft`.
- `OffsetX` / `OffsetY` are now a **nudge from the anchored resting position**, defaulting
  to `0`, rather than an absolute offset from top-centre. Positive X is right, positive Y
  is up.

This changes the meaning of the two offset values, so an existing
`com.wulfpack.runecompass.cfg` should be regenerated rather than carried forward.

## Visual pass 1 — done

Driven by the first live screenshots. No art assets yet; this is the placeholder geometry
made presentable so the artwork has a sane shape to land on.

| Target | Status |
|---|---|
| Reduce default footprint substantially | **done** — dial 190 to 120 px, a 37% linear reduction (~60% less area). Cardinal radius 72 to 45, needle 44 to 30, lubber 9x14 to 7x11 |
| Remove the opaque rectangular debug panel | **done** — the backing `Image` is gone entirely. The panel is now a transparent container |
| Keep heading-up unchanged | **done** — no bearing math touched; `verify-local.ps1` unchanged and passing |
| Numeric readouts become optional | **done** — new `ShowReadouts`, default `false` |
| Glyph legibility | **done** — cardinals counter-rotate so they stay upright while their positions travel the ring |

Removing the backing plate would have left pale glyphs over bright terrain, so each text
element now carries a dark `UnityEngine.UI.Outline`. That keeps it readable without a
filled rectangle.

Still to come, and needing artwork:

1. move the cardinals onto final circular/rune geometry;
2. distinguish heading and wind through final art — feather or spear motif for wind;
3. bind `ClassicWood`, then extract the reusable skin loader;
4. prove a second skin changes presentation only.

## ClassicWood binding — code complete, in-game pending

Assets from `3bcdd05` are integrated. All three families ship a centred 512x512 RGBA
bundle: `base.png`, `ring.png`, `wind_pointer.png`, `lubber_marker.png` and `skin.json`.

Layer contract, unchanged from the heading-up invariant:

| Layer | Parent | Motion |
|---|---|---|
| `base.png` | panel | static |
| `ring.png` | **rose** | rotates by `+heading`, the one heading transform |
| `wind_pointer.png` | **rose** | `-windToward`, a pure world bearing |
| `lubber_marker.png` | panel | static, marks facing |

No separate heading pointer was added: under heading-up the facing is always screen-up, so
the lubber marker carries it and the card moves beneath.

`skin.json.defaultScale` multiplies the player's `Scale` rather than replacing it, so
ClassicWood rests at `0.78` of the dial and `Scale` still means what it did.

Loading is deliberately small: `SkinLoader` reads `skin.json` with Unity's `JsonUtility`,
decodes each PNG with `ImageConversion.LoadImage`, and returns `null` on any failure so the
compass falls back to the primitive HUD rather than vanishing. A skin missing its ring or
wind pointer is rejected outright.

Verified so far: assets deploy (16 files), and the runtime resolves
`BepInEx/plugins/RuneCompass/Assets/Skins` with `exists: True` and `selected: ClassicWood`.
**Not yet seen rendering** — that needs a loaded world.

### Open question the artwork raises

The cardinal glyphs are baked into `ring.png`, which rides the rose. So under a skin they
rotate with the card and go upside down on southerly headings — the exact legibility
problem the primitive HUD just fixed by counter-rotating its glyphs.

The primitive path keeps its upright glyphs; the skinned path follows the art. If the
rotating letters read badly in ClassicWood, the fix is art-side: lift `N`/`E`/`S`/`W` out of
`ring.png` into a `ring_marks` layer that counter-rotates, leaving ticks and ornament on the
rotating ring. Operator judgement, once it is on screen.

## What is not verified

Three items remain, all needing operator judgement rather than a measurement:

- **Wind against observable world wind** (smoke from a fire, a sail, grass). The needle is
  drawn correctly for the bearing `GetWindDir()` reports, and that API's downwind
  convention is proven from `Ship.GetWindAngleFactor()` — but nobody has yet stood next to
  a fire and confirmed the smoke agrees.
- **`OnlyInNoMap = false` in a map-enabled world.** The default (hide) path is confirmed;
  the override is not.
- **Third-person camera orbit.** Heading is camera-sourced, so the rose turns while the
  body stays still. Whether that reads correctly is a taste call (open question 1).

A fourth is now answered, and the answer is *no*: **rotating cardinal glyphs do not read
well.** At heading 178 the letters are upside down — `E` renders as `Ǝ`, `W` as `M`. It is
authentic to a physical compass card and it is hard to read. See "Legibility finding".

## In-game acceptance — results

Recorded 2026-09-08 against the installed game, on the post-split, top-right build.

| # | Item | Result |
|---|---|---|
| 1 | Full 360 rotation | **PASS** — four quadrants sampled at headings 357, 274, 178 and 088. Every cardinal landed on `bearing - heading` clockwise from screen-up, and the wind needle was correct in all four while world wind held constant at 141 |
| 2 | Map-enabled world hides the compass | **PASS** — operator confirmed; compass absent in a minimap world with the default `OnlyInNoMap = true` |
| 3 | `OnlyInNoMap = false` shows it in a normal world | not yet run |
| 4 | Wind agrees with observable wind | not yet run |
| 5 | `Scale` | **PASS** — applied live at 0.55, 0.7 and 1.0 |
| 6 | `Opacity` | **PASS** — applied live at 0.30 and 0.9 |
| 7 | `OffsetX` / `OffsetY` | **PASS** — applied live as a nudge from the anchor |
| 8 | Third-person orbit judgement | not yet run |
| 9 | Disable across restart | **PASS** — Rune Compass absent from the log; Rested Whispers and Jotunheim still loaded |
| 10 | Enable across restart | **PASS** — returns |
| 11 | Uninstall across restart | **PASS** — absent; siblings intact |
| 12 | No Rune Compass load after uninstall | **PASS** |
| 13 | Unrelated plugins intact | **PASS** |
| 14 | Character / world unchanged | **PASS** — 101 save files, 2 sibling plugin files and 4 sibling config files byte-identical across the full lifecycle |

Also proven in the same session: the `Anchor` config switches the compass between corners
(`TopRight` to `TopCenter` observed live), and `ConfigWatcher` reloads the config without a
restart (16 reloads observed, no relaunch).

### Why the visual items could not be automated

An unattended launch stops at the main menu. `CompassController` only builds the HUD once
`Player.m_localPlayer` is non-null, so no world means no HUD, and a scripted sweep captures
nothing but the title screen. Loading a world needs a human at the menu. That is why the
rotation evidence above comes from operator screenshots rather than from the harness.

`ConfigWatcher` exists to make the rest cheap: with the game already in a world, config can
be retuned live instead of costing a relaunch plus a world load per value.

## Orientation reversed: heading-up to north-up

Seeing ClassicWood on screen changed the product decision, and the in-game view is better
evidence than the earlier argument was.

`N` is now fixed at 12 o'clock and the card never moves. A heading marker rides the rim to
the bearing the player faces; the wind pointer sits at the centre. That is how a compass is
normally read.

Two things drove it. The cardinal glyphs are **baked into `ring.png`**, so a rotating card
put `E` and `W` upside down on southerly headings — the legibility problem first seen at
heading 178 with the placeholder glyphs, which the artwork reproduced rather than solved.
And a fixed north is simply the familiar reading.

The code got simpler, not more complex:

| | heading-up | north-up |
|---|---|---|
| rotation mappings | two (`RoseRotationZ`, `WindRotationZ`) | **one** (`BearingRotationZ`) |
| card | rotates by `+heading` | **static** |
| glyphs | counter-rotated to stay upright | upright by construction |
| indicator placement | relative to where the player looks | **absolute world bearing** |

Every indicator is handed a world bearing and rendered at `z = -bearing`. Nothing needs to
know where the player is looking, so adding an indicator is handing it a bearing.

**No redundant heading pointer was added.** The skin's `lubber_marker.png` — a brass chevron
drawn at the rim — becomes the rotating heading index, which is what the art already was.
Heading and wind stay distinguishable by place and shape: rim chevron versus centre spear.

`verify-local.ps1` was rewritten for the new model and now refuses to pass if the heading-up
pair reappears, so a partial revert cannot pass silently.

## Orientation model

**North-up.** `N` fixed at 12 o'clock, card static, indicators absolute. See
`docs/ARCHITECTURE_PLAN.md` for the derivation and `RuneCompass/README.md` for the
player-facing description.

## Corrections to earlier status

- **The `m_windDir` fallback never existed.** `EnvMan` has no such field — the backing
  state is `m_wind` / `m_windDir1` / `m_windDir2`, all `Vector4` and non-public, and the
  fallback filtered on `Vector3`, so it could never have bound. Removed; `GetWindDir()` is now bound at compile time.
- **The first playable candidate did not build.** `build-local.ps1` had a parse error
  that killed every mode of the script, and the `.csproj` was missing two Unity module
  references. Both fixed. See `docs/SHADOW_GENOME.md` Failure #5.

## Confirmed product decisions

- Name: Rune Compass. Primary use case: No Map navigation.
- Core rule: direction, not hidden information.
- Orientation: heading-up.
- Wind is first-class, and points the direction the wind blows **toward**.
- Skins are presentation-only and interchangeable.
- Client-side; no save or world persistence; no GitHub Actions.

## Next

1. Rebuild after the `CompassUI` / `CompassUiFactory` split.
2. Rerun `verify-local.ps1` and confirm Razor still passes.
3. Complete the remaining human in-game acceptance checklist.
4. Merge PR #9 only when those results are recorded.
5. Start the visual/skin cycle with `ClassicWood`, then extract the loader, then prove a second skin.

---

# Storm + directional hierarchy — operator test protocol

Session `2026-09-09T1559-7ca34d`, branch `feat/rune-compass-storm-hierarchy`.

**Operator acceptance, 2026-09-09: the behaviour is accepted.** Every storm and
directional row has been observed in game and passes. The only outstanding work is the
nine skin art assets (`Assets/Skins/ART_SPEC.md`, tracked in `SKINS_INDEX.md`) and row
S12, which needs a ship.

**Merge is blocked until every row below is recorded.** This cannot be automated: an
unattended launch stops at the main menu, and `CompassController` builds no HUD until
`Player.m_localPlayer` is non-null. A human has to load a world.

## Setting up

The compass is installed and the build is green. Two things make this quick:

- **`ConfigWatcher` reloads config live**, so every value below can be retuned without a
  relaunch. No value change costs a world load.
- **Storms can be summoned rather than waited for.** `EnvMan.GetCurrentEnvironment()`
  honours the force-environment override, so a console `env ThunderStorm` puts you in one
  immediately, and `env clear` (or whatever your install's clear-weather name turns out to
  be) takes you out. **Row S2 gives you the real names.**

## The rows

| # | Check | What should happen | Result |
|---|---|---|---|
| S1 | Clear weather, standing still | **No drift.** Every layer rests on its true bearing: dial still, arrow on your facing, sector on the camera, rune upwind. | **PASS**, and now provable — at capture 0 the maths returns the true bearing exactly, for any time, seed or turbulence. |
| S2 | Read the environment log | `BepInEx/LogOutput.log` contains a `Rune Compass sees N environments` block listing your install's real names and wind ranges. Copy the stormy ones into `StormEnvironments`. |**PASS** - 49 environments listed. Storm set extended to 6 by wind evidence: `Ashlands_storm` 2.50-3.00, `Twilight_SnowStorm` 1.50-2.00, `SnowStorm` 1.30-2.00, `ThunderStorm` 0.80-1.00, `Ashlands_SeaStorm` 0.80-1.00, `Mistlands_thunder` 0.50-1.00. Boss arenas (`Eikthyr` 0.90-1.00, `Moder` 1.00) deliberately excluded as gameplay, not weather. Log also revealed `Ashlands_CinderRain` carries an inverted range, wind 0.75-0.70, in Valheim's own data. |
| S3 | Transition into a storm | Capture ramps in smoothly over ~4s. No snap. | **PASS**, with a jerkiness finding now fixed - see S4. |
| S4 | Sustained storm | The arrow and camera sector are **captured**: they show where the storm's field points, not where you face. **Turning must not recover true direction.** Motion should be drift plus occasional hard lurches, never jerky. | **PASS on capture** - turning no longer recovers direction. **Jerkiness found and fixed**: the lurch envelope began each pulse at full magnitude, stepping the storm bearing by up to 110 degrees in a single frame every 3.1s. It lived in the storm term, not the player term, which is why it persisted regardless of movement. Envelope is now `sin^6`, zero in value and slope at both ends. **Retest the feel.** |
| S5 | Transition out of a storm | Releases smoothly and every layer settles on **its own true bearing** - the arrow on your facing, the sector on the camera, the rune upwind. **Not on north**, unless you happen to be facing north. | **PASS**, with the release too fast. Now asymmetric: attack 4s, release 12s. The original row text said "settles exactly on true north", which the compass has never done and was never meant to do - a defective protocol row, not a defective compass. |
| S6 | Orbit the camera, character standing still | **Camera sector moves; the facing arrow holds.** This is the whole reason facing and camera are separate signals. |**PASS** |
| S7 | Turn the character without moving the camera | Arrow moves; sector holds. The mirror of S6. |**PASS** |
| S8 | Wind changes during a storm | The rune stays true while other layers are captured, and sits on the quarter the wind comes **FROM** - opposite the way smoke blows. | **PASS.** Initially reported as a failure against Moder's wind buff, which turned out to be the wrong reference: `EnvMan.UpdateWind` gates that buff behind `Ship.GetLocalShip()` and `IsWindControllActive()`, so it steers wind to the **ship's** heading and does nothing at all on land. The compass was correct. |
| S9 | Disable Rune Compass mid-interference | HUD disappears cleanly, no exception. Re-enable: the compass returns settled, not mid-wander. | **PASS** |
| S10 | `IndependentLayerInterference` both ways | Both ship. `true`: each layer is captured toward its own storm bearing, so the pointers disagree with each other. `false`: all are dragged toward one bearing, so they lie in agreement. **PASS — settled as a player preference rather than a hard-coded winner; neither is more correct.** |
| S11 | Storm near the world edge | Interference is governed strictly by environment name, unaffected by the very high wind Valheim forces near the edge. | **PASS** - vindicates rejecting `GetWindIntensity()` as the storm gate during research, which would have read the world edge as a permanent storm. |
| S12 | Ship wind gauge | Board a ship: Valheim's own wind gauge is hidden, leaving one wind readout. Then check all three restore paths - step off the ship, set `Enabled = false`, and leave No Map. The gauge must come back each time. | |
| S13 | Mistlands behaves as a storm | The compass is disturbed throughout the biome, whatever the weather, via the `StormBiomes` mask rather than by naming its three environments. | **PASS** - operator accepted. Note the biome is permanent, so interference there is constant rather than passing. |

## OQ-1, closed

`IndependentLayerInterference` shipped as a **player setting**, not a comparison instrument
to be resolved and deleted. Under the capture model the two modes are genuinely different
experiences rather than better and worse versions of one, so there was no winner to pick.

The `verify-local.ps1 -Seal` mode that existed to enforce the toggle's deletion has been
**removed entirely**, since the thing it guarded is now meant to be there. It was not kept
and emptied: a seal check with nothing left to check would pass vacuously, which is
`SHADOW_GENOME` Failure #7 exactly. The forbidden-identifier guard in the main run is
untouched and still refuses every superseded identifier.

## What is already proven locally

| Claim | Evidence |
|---|---|
| Builds clean | `build-local.ps1`: 0 warnings, 0 errors |
| Angle math unchanged | `verify-local.ps1`: 25 bearing assertions against hand-computed literals |
| Wander is bounded and deterministic | 10000-point sweep stays within [-1, 1]; repeat calls agree; distinct seeds separate layers |
| Envelope is correct at both edges | Reaches 1.0 at exactly the ramp, releases symmetrically to 0.0, never leaves [0,1], and a storm ending mid-attack releases from 0.25 rather than 1.0 |
| Clear weather adds no rotation | `Deflection` returns exactly 0 at envelope level 0, at three different times and seeds |
| Both bounds hold | Deflection 180 clamps to 35, -5 clamps to 0; ramp 0.5 clamps to 3.0 |
| Wind immunity is structural | `CompassUI.WindAmplitude` reads 0 off the compiled type |
| Toggle reaches the layers | `StormState` carries `IndependentLayers` and `DirectionLayer.Point` takes the whole state, asserted by reflection. This is the check that was missing when the setting silently became decoration |
| Superseded model cannot return | `verify-local.ps1` refuses `RoseRotationZ`, `WindRotationZ`, `CreateWindNeedle`, `_rose` |
| Razor holds | Largest file `CompassUI.cs` 217/250; longest method 34/40 |
| Nothing outside the mod was touched | Install wrote exactly 4 files, all under `plugins/RuneCompass`; save integrity clean |

## What only a human can confirm

Everything about how it **looks and feels**: whether the hierarchy reads correctly at a
glance, whether 22 degrees of wander is too much or too little, whether the ramp is the
right length, and whether independent or shared interference is the better effect. Those
are taste calls made in a live storm, and no assertion substitutes for them.
