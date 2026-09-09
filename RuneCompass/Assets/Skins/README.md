# Rune Compass skins

Rune Compass skins are presentation-only asset bundles. They must not alter navigation logic, wind semantics, configuration authority, or gameplay state.

## Which layers move

Rune Compass is **north-up**: `N` stays at 12 o'clock and the card never rotates. The
indicators move instead, each placed at an absolute world bearing.

That fixes the motion of every layer, and a skin does not get to choose it:

| Layer | Moves? |
|---|---|
| `base.png` — back plate | static |
| `ring.png` — the card carrying the cardinal marks | **static** — north stays up |
| `wind_pointer.png` | rotates to the wind's world bearing, centre-mounted |
| `lubber_marker.png` | rotates to the player's heading, riding the rim |

A skin supplies artwork for these roles. It never decides which of them rotate, and it
cannot introduce a layer with its own heading-dependent rotation — heading is applied in
exactly one transform, and that is a behavioural invariant, not a presentation choice.

Because the ring is static, its baked `N`/`E`/`S`/`W` are always upright — art for it does
not need to survive rotation. The two rotating layers are silhouettes, so they read at any
angle by construction.

Prepared skins contain:

```text
<SkinName>/
├── base.png
├── ring.png
├── wind_pointer.png
├── lubber_marker.png
└── skin.json
```

Not every texture is mandatory. A skin definition declares only the layers it uses.
The first prepared bundles keep the cardinal glyphs and tick marks in `ring.png`, so a
separate `ring_marks.png` or `north_marker.png` would add layers without adding clarity.

## Prepared families

| Family | State | Visual language |
|---|---|---|
| `ClassicWood` | prepared, not yet bound | warm dark wood, aged brass, restrained cardinal ring |
| `RuneRing` | prepared, not yet bound | darker forged metal, amber rune engraving |
| `MinimalNordic` | prepared, not yet bound | compact fallback with minimal ornament and a teal wind spear |

Every prepared PNG is a centered `512 x 512` RGBA canvas. That shared canvas is the
alignment contract: the ring and wind pointer rotate around `(0.5, 0.5)`, while the base
and lubber marker remain static. The wind spear tip and `N` both point to 12 o'clock in
their source textures. `skin.json` records the same convention beside each bundle.

The existing concept art should be curated into these roles during implementation. Do not duplicate every source image into every skin just because storage is cheap and restraint apparently is not.
