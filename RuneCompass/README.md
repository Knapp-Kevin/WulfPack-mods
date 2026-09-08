# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

Its job is intentionally narrow: show orientation and wind direction without becoming a minimap, GPS, route planner, or hidden-information overlay.

> Rune Compass gives you direction, not information.

## Orientation model

**Heading-up.** Screen-up is always where you are looking. The compass rose carries the
cardinal letters and rotates so each sits at its true bearing relative to your view,
while a fixed amber **lubber marker** at the top of the dial marks your facing.

That is how a hand-held compass behaves, and it is how Valheim itself draws direction:
the game's ship wind indicator is rotated by a bearing taken in a reference frame, never
by pinning world-absolute letters to the screen.

The letters rotate with the card, so `S` is upside down when you face south. That is
authentic to a physical compass card rather than an oversight.

Internally, heading enters the UI in exactly one place — the rose's rotation. Anything
mounted on the rose is positioned by pure world bearing, and the heading term cancels in
its transform. The derivation is written out in `docs/ARCHITECTURE_PLAN.md`.

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

- heading-up orientation with a rotating rose and fixed lubber marker
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

This needs no conversion, because it is already Valheim's own convention. Confirmed from
`Ship.GetWindAngleFactor()`, which computes `Dot(GetWindDir(), -transform.forward)` and
drives sail power to zero as that approaches `+1` — the "cannot sail into the wind" case,
which only holds if the vector points downwind. Adding a negation would be the bug.

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
