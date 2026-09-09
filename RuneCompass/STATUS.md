# Rune Compass status

## Current status

**Revalidated after the `CompassUI` / `CompassUiFactory` split.** Build, verification and
runtime all green on the current branch head, and the split is proven behaviour-preserving
rather than merely assumed.

The compass now anchors to the **top-right corner**, taking the place of the minimap it
replaces in No Map play. Previously it sat top-centre.

Issue #4 stays **open** until the operator acceptance items are recorded.

All six prepared skins now include a dedicated `heading_pointer.png`. ClassicWood remains
the grounded dark-oak and brass baseline. The other five families were rebuilt around
different materials, silhouettes, negative space, and pointer languages instead of
recolouring the same disc. Heading renders above wind and now measures 2.75x to 11.02x the
corresponding wind asset's opaque mass in the rebuilt families. Runtime loading is
implemented on this branch, but the replacement art still requires the normal local
rebuild and in-game review.

## Visual differentiation pass

Recorded 2026-09-09. This pass changes assets and skin metadata only; bearing math and
gameplay behaviour are untouched.

| Family | Distinct construction | Primary heading | Secondary wind |
|---|---|---|---|
| `ClassicWood` | dark oak and aged brass | broad brass needle | dark feather-spear |
| `RuneRing` | basalt and ember-cut runes | bronze rune-blade | icy wisp |
| `MinimalNordic` | open centre and broken iron line | ivory lozenge with ochre north tip | hairline teal arrow |
| `KnotworkWood` | pale ash, braided iron and leather | antler spear | blue-green feather |
| `BlackIron` | soot-dark hide and riveted forge iron | bone spear | rust-copper vane |
| `GildedSigil` | indigo face and open-work gold | violet-inlaid ceremonial lance | cyan crescent |

Every replacement layer is a centred `512 x 512` RGBA PNG with transparent corners.
Cardinal letters are baked into each fixed north-up ring. Visual review has been performed
at the approximate shipped HUD size; local compile and in-game review remain pending.

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

The first north-up pass used a small rim index. The in-game screenshot showed that wind
still carried more authored visual authority, so that decision is superseded. Heading now
uses a dedicated broad centre needle, rendered above the slimmer wind pointer.

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
- Orientation: north-up, with a static card and absolute-bearing indicators.
- Heading is the primary signal and has its own prominent centre needle.
- Wind is first-class, and points the direction the wind blows **toward**.
- Skins are presentation-only and interchangeable.
- Client-side; no save or world persistence; no GitHub Actions.

## Next

1. Rebuild and install the dedicated heading-needle candidate.
2. Rerun `verify-local.ps1` and confirm Razor still passes.
3. Confirm in game that heading remains visually primary at reduced scale and opacity.
4. Complete the remaining human acceptance items and record them on issue #4.
5. Merge only after those results are recorded.
