# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

Its job is intentionally narrow: show orientation and wind direction without becoming a minimap, GPS, route planner, or hidden-information overlay.

> Rune Compass gives you direction, not information.

## Orientation model

**North-up.** `N` is fixed at 12 o'clock and the card never moves. The indicators travel
instead:

- a prominent **heading needle** points to the bearing you are facing;
- a slimmer **wind pointer** points where the wind is blowing.

That is how a compass is normally read: north is a fixed reference and you read your
direction against it.

Because the card is pinned to world north, every rotation is **absolute**. Each indicator
is handed a world bearing and rendered at `z = -bearing` through a single mapping — nothing
needs to know where the player is looking. Adding an indicator means handing it a bearing.

Heading and wind are told apart by mass, shape, and layer order, not colour alone. The
solid warm-metal heading needle is broader and renders above the slimmer feather-spear
wind pointer.

> An earlier revision used a heading-up card that rotated under a fixed marker. It was
> replaced after seeing it on real artwork: the cardinal glyphs are baked into the ring, so
> a rotating card put `E` and `W` upside down on southerly headings, and a fixed north
> reads more like a compass. North-up is also simpler — one mapping instead of two, and no
> counter-rotation.

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

## Current implementation

Compiled against the installed Valheim build, installed, loaded, and observed rendering
correctly in a live No Map world. The remaining acceptance steps need a human at the
keyboard and are listed in [STATUS.md](STATUS.md); issue #4 stays open until then.

Implemented now:

- north-up orientation: fixed card, prominent heading needle, secondary wind pointer
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

ClassicWood is bound, and all six prepared skins now include a dedicated heading texture.
The new heading art still needs the local rebuild and in-game visual acceptance recorded
in [STATUS.md](STATUS.md).

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

**Quickest check if you ever doubt it:** open the vanilla minimap and compare its wind
marker against the compass pointer. They are driven by the same value through the same
formula, so they must agree. Smoke is harder to read than it sounds.

`WindPointsToward = false` flips to the meteorological "coming from" convention if you
prefer it.

Heading and wind calculations remain separate in code even though both drive directional UI elements.

## Skin system

Skins control presentation only. They must not change gameplay behavior.

Under north-up, the card and base are static. The heading and wind pointers each rotate to
an absolute world bearing. A skin chooses artwork for those roles; it never chooses their
motion semantics.

A skin may eventually define:

- `base.png` — static back plate
- `ring.png` — static north-up card carrying navigation marks
- `heading_pointer.png` — primary indicator, rotates to player heading
- `wind_pointer.png` — secondary indicator, rotates to wind-toward bearing
- optional `lubber_marker.png` — static accent at the north index
- pivot metadata and visual offsets
- default visual scale or opacity where needed for alignment

Both pointer textures share an exact centre pivot. Heading renders after wind and uses a
broader, warmer, more opaque silhouette so player direction remains primary.

Prepared skin families:

1. **Classic Wood**: dark oak, aged brass, a broad brass heading needle and feather wind.
2. **Rune Ring**: black basalt, ember-cut runes, a bronze rune-blade and icy wind wisp.
3. **Minimal Nordic**: negative space, a broken iron line, an ivory heading lozenge and hairline teal wind.
4. **Knotwork Wood**: pale ash, braided iron and leather, an antler heading spear and blue-green feather wind.
5. **Black Iron**: soot-dark hide, riveted forge iron, a bone heading spear and rust-copper wind vane.
6. **Gilded Sigil**: an indigo mystic face, open-work gold, a ceremonial heading lance and cyan wind crescent.

Existing compass concept art should be curated into these roles rather than copied wholesale into every skin.

## Structure

```text
RuneCompass/
├── Plugin.cs               BepInEx lifecycle + configuration
├── CompassController.cs    visibility rules, per-frame update
├── CompassUI.cs            runtime UI state, rotation, readouts, disposal
├── CompassUiFactory.cs     primitive Unity UI construction
├── HudAnchor.cs            screen-corner anchoring
├── Bearing.cs              angle math + the absolute bearing mapping
├── CompassSkin.cs          skin manifest, texture loading, safe fallback
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

The loader treats heading art as optional and falls back to the primitive heading needle
if a third-party skin has not adopted `headingPointerTexture` yet.

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
