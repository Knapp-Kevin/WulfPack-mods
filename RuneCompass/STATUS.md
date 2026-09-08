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

## Screenshot-driven visual tuning targets

The first live screenshot proves the primitive HUD is usable as a technical instrument, but not yet release-quality. The next visual cycle should address these in order:

1. reduce the default visual footprint substantially;
2. replace or minimize the opaque rectangular debug panel;
3. move the cardinals onto the final circular/rune geometry;
4. make heading and wind visually distinct through final artwork;
5. retain numeric heading/wind readouts only as an optional diagnostic mode;
6. bind `ClassicWood` first, then extract the reusable skin loader;
7. prove a second skin changes presentation only.

Do not combine these visual changes with the current UI split until the split has rebuilt and passed the existing local verification.

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

## Legibility finding: rotating glyphs

The cardinal letters are children of the rose, so they rotate with the card. At southerly
headings they are upside down (`E` as `Ǝ`, `W` as `M`, `N` inverted).

This is faithful to a physical compass card, and it is the wrong call for a HUD read at a
glance. The fix is to counter-rotate each glyph by `-heading` so the letters stay upright
while their *positions* still travel around the ring. The card still rotates; only the
glyph orientation is pinned. This costs one transform per letter and changes no bearing
math.

Carried into the visual cycle rather than patched here, so the change lands with the
artwork that replaces these placeholder glyphs.

## Orientation model

**Heading-up.** Screen-up is always where you are looking. The rose carries the
cardinal letters and rotates so each sits at its true bearing relative to your view; a
fixed amber lubber marker at the top of the dial marks your facing.

Heading enters the UI in exactly one place, the rose's rotation. The wind needle is
mounted on the rose and carries a pure world bearing; the heading term cancels in its
transform. Full derivation in `docs/ARCHITECTURE_PLAN.md` § Rune Compass.

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
