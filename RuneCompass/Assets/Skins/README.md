# Rune Compass skins

Rune Compass skins are presentation-only asset bundles. They must not alter navigation logic, wind semantics, configuration authority, or gameplay state.

## Which layers move

Rune Compass is **north-up**: the compass card is fixed with north at 12 o'clock. The
heading and wind pointers move independently to their absolute world bearings.

That fixes the motion of every layer, and a skin does not get to choose it:

| Layer | Moves? |
|---|---|
| `base.png` — back plate behind the card | static |
| `ring.png` — fixed card geometry and navigation marks | static, north at 12 o'clock |
| `wind_pointer.png` | rotates to the wind-toward world bearing; secondary signal |
| `heading_pointer.png` | rotates to the player's world bearing; primary signal |
| `lubber_marker.png` | optional static accent at the north index |

A skin supplies artwork for these roles. It never decides which of them rotate. Heading
and wind each receive one absolute-bearing transform; that is a behavioural invariant,
not a presentation choice.

The heading pointer is intentionally the dominant indicator: warm, solid, 379 pixels tall
on the shared authoring canvas, with a broad tip and central boss. Wind uses a slimmer
feather, spear, or arrow silhouette and renders beneath heading. Shape, mass, layer order,
and material distinguish the signals before colour does.

Prepared skins contain:

```text
<SkinName>/
├── base.png
├── ring.png
├── heading_pointer.png
├── wind_pointer.png
├── lubber_marker.png
└── skin.json
```

Not every texture is mandatory. A skin definition declares only the layers it uses.
The original three bundles retain baked cardinal art. The three follow-up bundles use
letterless rings and therefore require runtime cardinal glyphs when they are bound.

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
alignment contract: both pointers rotate around `(0.5, 0.5)`, while the base, ring, and
optional lubber marker remain static. Both pointer tips and the ring's north index point
to 12 o'clock in their source textures. `skin.json` records the same convention beside
each bundle.

The existing concept art should be curated into these roles during implementation. Do not duplicate every source image into every skin just because storage is cheap and restraint apparently is not.
