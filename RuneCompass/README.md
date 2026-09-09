# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

Its job is intentionally narrow: show orientation and wind direction without becoming a minimap, GPS, route planner, or hidden-information overlay.

> Rune Compass gives you direction, not information.

## Orientation model

**North-up.** `N` is fixed at 12 o'clock and the card rests there. The indicators travel
instead, and they are ranked so the important one is obvious at a glance:

| Signal | Where it is | How loud |
|---|---|---|
| **Which way you are facing** | a bold arrow from the centre | the compass's needle — the boldest thing on the dial |
| **North and south** | printed on the card, which never moves | quiet background reference |
| **Where the camera looks** | a faint sector behind the card | subtle; there when you want it, ignorable when you don't |
| **Where the wind comes from** | a small rune orbiting *outside* the rim | about a third the arrow's size |

It works like a normal compass: **one** moving pointer, read against a fixed graduated card.
The difference from a real one is what the pointer shows — your facing, not north. North is
printed on the card and stays put, which is why the card never rotates.

The wind rune sits on the side the wind is **coming from**, the way a nor'easter is named for
where it blows from rather than where it blows to. (Valheim reports wind as a
direction-of-travel vector; the rune marks the other end of it. Set `WindShowsSource = false`
if you would rather it sat downwind.)

Your **body's facing** and your **camera's view** are separate signals, because in Valheim
they genuinely differ: stand still and swing the camera around and the arrow holds while
the sector moves. That is the point — the arrow tells you where you will walk, the sector
tells you where you are looking.

Because the card is pinned to world north, every rotation is **absolute**. Each indicator
is handed a world bearing and rendered at `z = -bearing` through a single mapping — nothing
needs to know where the player is looking. Adding an indicator means handing it a bearing.

The four are told apart by place and size, never by colour alone: arrow at the centre,
card behind, camera sector fainter still, wind outside the ring and small.

> An earlier revision used a heading-up card that rotated under a fixed marker. It was
> replaced after seeing it on real artwork: the cardinal glyphs are baked into the ring, so
> a rotating card put `E` and `W` upside down on southerly headings, and a fixed north
> reads more like a compass. North-up is also simpler — one mapping instead of two, and no
> counter-rotation.

## Storms

A Valheim storm is bad weather for a compass, and Rune Compass behaves like an instrument
that is suffering rather than one that has been switched off.

During a storm the **camera sector** and the **facing arrow** drift off true,
wandering slowly rather than spinning or twitching. The card itself never moves, so the
drift is legible against it — and the printed N/E/S/W stay upright and readable throughout.
Interference fades in as the storm builds and fades out as it passes; it never snaps.

**The wind rune does not malfunction.** Wind is something you can see — driven rain, bent
grass, a sail pulling — so it keeps telling the truth while the instrument struggles. The
compass fails; the weather does not lie.

Rune Compass tells you nothing new here. Interference only ever *removes* accuracy from a
reading you already had, and it happens when you can already see the storm.

### Which weather counts as a storm

Valheim has no "is it storming" flag anywhere in its code — that was checked field by field,
not assumed — so Rune Compass matches on the environment's name instead, which is what the
game does internally. It ships knowing only about `ThunderStorm`, the one name that could be
proven to exist.

The rest are yours to add, and the mod hands you the list: **on your first world load it
logs every environment your install actually has**, with each one's wind range, to the
BepInEx log. Copy the stormy-looking ones into `StormEnvironments`.

That is deliberate. A longer default list would have been guesswork dressed up as a feature.

## Configuration

Written to `BepInEx/config/com.wulfpack.runecompass.cfg` on first run. BepInEx does not
watch the file, so changes take effect on the next launch.

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Master switch. |
| `OnlyInNoMap` | `true` | Show only in No Map worlds. Set `false` to keep it in normal worlds too. |
| `Anchor` | `TopRight` | Screen corner to pin to: `TopRight`, `TopCenter`, `TopLeft`, `BottomRight`, `BottomLeft`. `TopRight` is where the minimap would be. |
| `ShowReadouts` | `false` | Numeric heading/wind text under the dial. Off by default — direction, not instrumentation. Turn on for calibration. |
| `Scale` | `1` | HUD scale multiplier, floored at `0.25`. |
| `Opacity` | `0.9` | HUD opacity, clamped to 0–1. |
| `OffsetX` | `0` | Nudge from the anchored resting position. Positive is right. |
| `OffsetY` | `0` | Nudge from the anchored resting position. Positive is up. |
| `HeadingOffsetDegrees` | `0` | Clockwise calibration offset. Leave at `0` — the bearing convention already matches the game's own. |

Anchoring to a corner rather than to absolute pixels keeps the compass in place across
resolutions and aspect ratios.

### `[Storm]`

| Key | Default | What it does |
|---|---|---|
| `InterferenceEnabled` | `true` | Master switch. `false` keeps the compass accurate in every weather. |
| `StormEnvironments` | `ThunderStorm` | Comma-separated environment names treated as storms. Extend from the environment list logged on your first world load. |
| `MaxDeflectionDegrees` | `22` | How far a storm can push the compass off true. Clamped to 35 — past that the N/E/S/W marks baked into the card turn upside down. |
| `InterferenceRampSeconds` | `4.0` | How long interference takes to arrive and to fade. Clamped to a minimum of 3; see below. |
| `IndependentLayerInterference` | `true` | `true`: each disturbed layer wanders on its own. `false`: all three swing together. |

> **Why the ramp has a floor.** Valheim switches the environment's name at the *start* of a
> weather change, about two seconds before the sky visibly turns. A slow ramp keeps that
> lead invisible. A fast one would make the compass react before the weather does, turning
> it into a storm early-warning device — information you could not otherwise have. Raise
> this value if interference ever arrives ahead of the sky; do not lower it.

## Current implementation

Compiled against the installed Valheim build, installed, loaded, and observed rendering
correctly in a live No Map world. The remaining acceptance steps need a human at the
keyboard and are listed in [STATUS.md](STATUS.md); issue #4 stays open until then.

Implemented now:

- north-up orientation: fixed card, rim heading marker, centre wind pointer
- camera-based heading, using Valheim's own `Atan2(x, z)` bearing convention
- No Map-aware visibility through `Game.m_noMap`
- live wind direction from `EnvMan.GetWindDir()`, bound at compile time
- wind semantics defined as direction **toward**
- pinned to a screen corner, defaulting to top-right where the minimap would be
- configurable anchor, scale, opacity, position nudge, enable state, and heading calibration
- local build / install / disable / enable / status / uninstall workflow
- local verification of the angle conventions and the Section 4 line limits
- no save or world-state mutation
- zero GitHub Actions

The HUD is still deliberately plain. It exists to prove mechanics before skin assets are
bound, and the mechanics are now proven.

## Local build and install

From the repository root:

```powershell
.\RuneCompass\build-local.ps1
.\RuneCompass\verify-local.ps1
.\RuneCompass\build-local.ps1 -Install
```

`verify-local.ps1` loads the compiled DLL and checks every angle mapping against a
hand-computed literal, then measures the Section 4 line limits. It exits non-zero on any
failure. It cannot construct Unity objects, so it verifies the maths, not the
rendering — the rendering checks live in [STATUS.md](STATUS.md).

Other lifecycle commands:

```powershell
.\RuneCompass\build-local.ps1 -Status
.\RuneCompass\build-local.ps1 -Disable
.\RuneCompass\build-local.ps1 -Enable
.\RuneCompass\build-local.ps1 -Uninstall
```

These commands are local only. This repository does not use GitHub Actions.

## Wind behavior

Wind is a first-class Rune Compass signal, not decorative polish.

The compass distinguishes:

- **heading / cardinal orientation**: where the player is facing relative to north; and
- **wind direction**: where the current world wind is blowing.

Rune Compass shows the direction the wind is blowing **toward**, never the
meteorological "coming from" convention.

This needs no conversion, because it is already Valheim's own convention. Three independent
confirmations from the game's own code:

1. **Sailing** — `Ship.GetWindAngleFactor()` drives sail power to zero as
   `Dot(GetWindDir(), -forward)` approaches `+1`, the cannot-sail-into-the-wind case. Only
   true if the vector points downwind.
2. **Physics** — `Cinder.FixedUpdate` accelerates embers along `GetWindForce()`, which is
   `GetWindDir()` scaled by strength. Debris drifts *with* the vector.
3. **Valheim's own indicator** — `Minimap.UpdateWindMarker` rotates its marker by
   `-LookRotation(GetWindDir()).eulerAngles.y`, identical to Rune Compass's mapping.

**These proofs are about the game's API, not about where the rune is drawn.** The wind rune
deliberately sits on the quarter the wind comes **from**, which is the *reciprocal* of the
vector above.

That is a choice about visual grammar rather than a disagreement with the game. An arrow is a
vector and must point downwind or it contradicts itself; a glyph parked on a compass rim is a
*position*, and a position on a rim reads as a quarter — the direction weather arrives from.

**So expect the rune to sit opposite the vanilla minimap's wind marker.** They are driven by
the same value; the minimap draws the vector's head and Rune Compass marks its tail. If you
would rather it sat downwind, set `WindShowsSource = false`.

**Quickest check if you ever doubt the underlying value:** stand by a fire. Smoke drifts
*downwind*, so the rune should sit on the opposite side of the dial from the way the smoke
blows.

Heading and wind calculations remain separate in code even though both drive directional UI elements.

## Skin system

Skins control presentation only. They must not change gameplay behavior.

Under heading-up, the **card-bearing ring is the rotating layer** — it is mounted on the
rose and turns with it. The base plate, the lubber marker and the readouts are static. A
skin chooses artwork for those layers; it never chooses which of them rotate.

A skin may eventually define:

- `base.png` — static back plate
- `ring.png` — the rotating card carrying the cardinal marks
- optional `ring_marks.png` — cardinal glyphs, if not baked into the ring
- `wind_pointer.png` — mounted on the card, carries a pure world bearing
- optional `north_marker.png` — mounted on the card at bearing 0
- a static lubber marker for "you are looking this way"
- pivot metadata and visual offsets
- default visual scale or opacity where needed for alignment

There is deliberately **no rotating heading pointer**: under heading-up your facing is
always screen-up, so the lubber marker is static and the card moves instead.

Planned initial skin families:

1. **Classic Wood**: carved wooden face, restrained metal framing, simple pointer.
2. **Rune Ring**: darker runic ring treatment with stronger Norse ornament.
3. **Minimal Nordic**: compact, highly readable treatment for players who want less HUD weight.

Existing compass concept art should be curated into these roles rather than copied wholesale into every skin.

## Structure

```text
RuneCompass/
├── Plugin.cs               BepInEx lifecycle + configuration
├── CompassController.cs    visibility rules, per-frame update
├── CompassUI.cs            runtime UI state, rotation, readouts, disposal
├── CompassUiFactory.cs     primitive Unity UI construction
├── HudAnchor.cs            screen-corner anchoring
├── Bearing.cs              angle math + the two UI rotation mappings
├── HeadingProvider.cs      camera forward -> world bearing
├── WindProvider.cs         EnvMan.GetWindDir() -> world bearing
├── RuneCompass.csproj
├── build-local.ps1
├── verify-local.ps1
├── manifest.json
├── icon.png
├── README.md
├── IMPLEMENTATION_PLAN.md
├── STATUS.md
└── Assets/
    └── Skins/
```

`SkinDefinition` and `SkinLoader` are intentionally not implemented yet. The working compass behavior should earn the abstraction before it is introduced.

## Validation

Mechanical validation is complete and recorded in [STATUS.md](STATUS.md): clean build,
clean plugin load, angle assertions, Section 4 limits, and a SHA-256 lifecycle diff
proving install/disable/enable/uninstall leave every other plugin, every other config,
and all 100 character/world files byte-identical.

The in-game acceptance pass — a full turn, wind cross-check, visibility modes, and the
display config — is a numbered checklist in the same file. Issue #4 stays open until an
operator completes it.

## Explicitly out of scope for the current slice

- minimap replacement
- map pins or markers
- boss / trader / player tracking
- route guidance
- home bearing
- Vegvisir bearing
- progression gating
- physical equippable compass item
- multiplayer config synchronization
- server-side state
- world/save persistence
- final multi-skin implementation

Those may be considered later. They are not prerequisites for proving Rune Compass.
