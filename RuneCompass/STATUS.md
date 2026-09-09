# Rune Compass status

## Current status

**Compiled, installed, loaded, and observed rendering correctly in a live No Map world.**
Mechanical validation is complete for PR #9's heading/wind correction. A follow-up UI split has now been added on the same branch and requires one local rebuild before PR #9 can be considered stable again.

Six presentation asset families are prepared, but deliberately remain unbound:

- `ClassicWood`: complete four-layer 512 x 512 RGBA bundle plus `skin.json`;
- `RuneRing`: complete four-layer 512 x 512 RGBA bundle plus `skin.json`;
- `MinimalNordic`: complete compact four-layer fallback plus `skin.json`;
- `KnotworkWood`: complete carved-wood and aged-brass four-layer bundle plus `skin.json`;
- `BlackIron`: complete dark timber, iron, and copper four-layer bundle plus `skin.json`;
- `GildedSigil`: complete ornate runic four-layer bundle plus `skin.json`;
- all six bundles use the same centered pivot and north-up source convention;
- all six now include a dedicated `heading_pointer.png` whose warm solid silhouette is
  intentionally more prominent than the wind pointer;
- runtime binding for `headingPointerTexture` remains for the validated integration branch;
- the three follow-up rings omit baked cardinal letters for compatibility with the
  runtime's upright, counter-rotated glyphs;
- no runtime behavior or gameplay code changed during asset preparation.

Issue #4 stays **open** until the operator acceptance pass is done.

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

## UI split before skins

`CompassUI.cs` had reached 246/250 lines, so the next implementation step was completed before skin work:

- `CompassUI.cs` now owns state, layout application, bearing rotation, readouts, and disposal.
- new `CompassUiFactory.cs` owns primitive Unity UI construction.
- behavior and orientation math were intentionally left unchanged.
- this creates room for a later skin renderer without forcing skin loading into the behavioral UI class.

This refactor is **not yet locally compiled**. The next local gate must rerun both build and `verify-local.ps1` before additional visual changes are layered on top.

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

The image files may be reviewed independently of that build gate. Binding them into
Unity remains after the split verification, so their presence is not evidence that a
skin has rendered in game.

## What is not verified

Honest boundary. The local harness invokes pure functions by reflection over the
compiled DLL; it cannot construct Unity `GameObject`s or `RectTransform`s, so it never
observes parenting or rendering. Everything below needs eyes on the screen.

- A **full 360° turn** — orientation is confirmed at one heading (344°), not swept.
- Whether the wind needle agrees with **observable** world wind (smoke, sail, grass),
  as opposed to being drawn correctly for the number `GetWindDir()` reports.
- The No Map **hide** case and the `OnlyInNoMap = false` override.
- `Scale`, `Opacity`, `OffsetX/Y` visual effect.
- Whether **camera-sourced heading reads well in third person**, where the camera
  orbits independently of the body (open question 1).
- Whether **rotating cardinal glyphs** read acceptably — `S` is upside down when facing
  south, which is authentic to a physical compass card but is a taste call
  (open question 2).

## In-game acceptance

Run these and record the result on issue #4. Install first:

```powershell
.\RuneCompass\build-local.ps1 -Install
```

| # | Step | Expect |
|---|---|---|
| 1 | Launch Valheim, check `BepInEx/LogOutput.log` | `Rune Compass 0.1.0 loaded.`, no Rune Compass error |
| 2 | Enter a **No Map** world | `Rune Compass heading-up HUD created.` |
| 3 | Look at the HUD | Dial at top centre; `N`/`E`/`S`/`W` on the card; amber marker fixed at top |
| 4 | Enter a **normal** (map-enabled) world | Compass hidden |
| 5 | Set `OnlyInNoMap = false`, restart, normal world | Compass visible |
| 6 | **Turn slowly through a full circle** | Card rotates smoothly; when `N` is at the top marker the readout reads ≈`000° N`; letters stay on their true bearings the whole way round |
| 7 | Face a known direction and check the readout | Degrees and cardinal agree with where you are actually looking |
| 8 | Compare the wind needle against smoke from a fire, or a sail | Needle points where the wind **blows toward**, not where it comes from |
| 9 | Third person, orbit the camera without moving | Decide whether camera-sourced heading reads correctly (open question 1) |
| 10 | Set `Scale`, then `Opacity`, then `OffsetX`/`OffsetY`, restarting between | Each changes the HUD as documented |
| 11 | `.\RuneCompass\build-local.ps1 -Disable`, relaunch | No compass, no Rune Compass line in the log |
| 12 | `-Enable`, relaunch | Compass returns |
| 13 | `-Uninstall`, relaunch | No Rune Compass plugin load; Rested Whispers and Jotunheim still load; character and world unchanged |

Config lives at `BepInEx/config/com.wulfpack.runecompass.cfg` and is written on first
run.

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
5. Bind `ClassicWood` without changing the heading-up transform model and test it in game.
6. Extract the smallest useful loader, then prove `RuneRing` changes presentation only.
7. Bind `MinimalNordic` only after both primary families render cleanly at reduced scale.
