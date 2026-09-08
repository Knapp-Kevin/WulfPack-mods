# Rune Compass skins

Rune Compass skins are presentation-only asset bundles. They must not alter navigation logic, wind semantics, configuration authority, or gameplay state.

## Which layers move

Rune Compass is **heading-up**: the compass card rotates so each cardinal sits at its
true bearing relative to the player's view, and screen-up is always the player's facing.

That fixes the motion of every layer, and a skin does not get to choose it:

| Layer | Moves? |
|---|---|
| `base.png` — back plate behind the card | static |
| `ring.png` — rotating card geometry and navigation marks | **rotates with the rose**, by `+heading` |
| `wind_pointer.png` | mounted on the card; carries a pure world bearing |
| `north_marker.png` (optional) | mounted on the card at bearing 0 |
| `lubber_marker.png` — "you are looking this way" | static, at the top of the dial |

A skin supplies artwork for these roles. It never decides which of them rotate, and it
cannot introduce a layer with its own heading-dependent rotation — heading is applied in
exactly one transform, and that is a behavioural invariant, not a presentation choice.

Art authored for the ring should therefore read correctly at **any** rotation. Current
runtime cardinal glyphs travel with the ring but counter-rotate to remain upright, so
new rings omit baked `N`/`E`/`S`/`W` letters. Decorative runes may rotate with the card
because they are ornament, not primary direction labels.

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
The original three bundles predate the upright-glyph decision and retain baked cardinal
art. The three follow-up bundles use letterless rings and are the cleaner integration
reference. A separate `ring_marks.png` is not needed.

## Prepared families

| Family | State | Visual language |
|---|---|---|
| `ClassicWood` | prepared, not yet bound | warm dark wood, aged brass, restrained cardinal ring |
| `RuneRing` | prepared, not yet bound | darker forged metal, amber rune engraving |
| `MinimalNordic` | prepared, not yet bound | compact fallback with minimal ornament and a teal wind spear |
| `KnotworkWood` | prepared, not yet bound | deeply carved timber, bright aged brass, blue-steel feather spear |
| `BlackIron` | prepared, not yet bound | dark timber, riveted iron, restrained copper wind arrow |
| `GildedSigil` | prepared, not yet bound | ornate amber runes and a subdued open-work mystic sigil |

Every prepared PNG is a centered `512 x 512` RGBA canvas. That shared canvas is the
alignment contract: the ring and wind pointer rotate around `(0.5, 0.5)`, while the base
and lubber marker remain static. Wind tips and the ring's north-up index point to 12
o'clock in their source textures. `skin.json` records the same convention beside each
bundle.

The existing concept art should be curated into these roles during implementation. Do not duplicate every source image into every skin just because storage is cheap and restraint apparently is not.
