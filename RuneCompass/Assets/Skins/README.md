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

The heading pointer is intentionally the dominant indicator. Wind uses a slimmer feather,
wisp, vane, or hairline-arrow silhouette and renders beneath heading. Shape, mass, layer
order, and material distinguish the signals before colour does. Across the prepared skins,
heading carries 2.75x to 11.02x the opaque mass of wind.

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

Not every texture is mandatory. A skin definition declares only the layers it uses. Every
prepared ring includes baked cardinal art, so a skin can be bound without a second glyph
system or a font dependency.

## Prepared families

| Family | State | Visual language |
|---|---|---|
| `ClassicWood` | prepared, not yet bound | warm dark wood, aged brass, restrained cardinal ring |
| `RuneRing` | prepared, not yet bound | black basalt, ember runes, bronze rune-blade heading, icy wind wisp |
| `MinimalNordic` | prepared, not yet bound | negative space, broken iron line, ivory lozenge heading, hairline teal wind |
| `KnotworkWood` | prepared, not yet bound | pale ash, braided iron and leather, antler heading, blue-green feather wind |
| `BlackIron` | prepared, not yet bound | soot-dark hide, riveted forge iron, bone spear heading, rust-copper wind vane |
| `GildedSigil` | prepared, not yet bound | indigo mystic face, open-work gold, ceremonial heading lance, cyan wind crescent |

Every prepared PNG is a centered `512 x 512` RGBA canvas. That shared canvas is the
alignment contract: both pointers rotate around `(0.5, 0.5)`, while the base, ring, and
optional lubber marker remain static. Both pointer tips and the ring's north index point
to 12 o'clock in their source textures. `skin.json` records the same convention beside
each bundle.

The existing concept art should be curated into these roles during implementation. Do not duplicate every source image into every skin just because storage is cheap and restraint apparently is not.
