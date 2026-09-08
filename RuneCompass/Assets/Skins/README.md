# Rune Compass skins

Rune Compass skins are presentation-only asset bundles. They must not alter navigation logic, wind semantics, configuration authority, or gameplay state.

## Which layers move

Rune Compass is **heading-up**: the compass card rotates so each cardinal sits at its
true bearing relative to the player's view, and screen-up is always the player's facing.

That fixes the motion of every layer, and a skin does not get to choose it:

| Layer | Moves? |
|---|---|
| `base.png` — back plate behind the card | static |
| `ring.png` — the card carrying the cardinal marks | **rotates with the rose**, by `+heading` |
| `wind_pointer.png` | mounted on the card; carries a pure world bearing |
| `north_marker.png` (optional) | mounted on the card at bearing 0 |
| lubber marker — "you are looking this way" | static, at the top of the dial |

A skin supplies artwork for these roles. It never decides which of them rotate, and it
cannot introduce a layer with its own heading-dependent rotation — heading is applied in
exactly one transform, and that is a behavioural invariant, not a presentation choice.

Art authored for the ring should therefore read correctly at **any** rotation: the
letters turn with the card, so `S` will be upside down when the player faces south.

Each skin should eventually contain:

```text
<SkinName>/
├── base.png
├── ring.png
├── ring_marks.png        # optional; cardinal glyphs if not baked into ring.png
├── wind_pointer.png
├── north_marker.png      # optional
└── skin.json
```

Not every texture is mandatory. A skin definition should declare only the layers it actually uses.

Initial families:

- `ClassicWood`
- `RuneRing`
- `MinimalNordic`

The existing concept art should be curated into these roles during implementation. Do not duplicate every source image into every skin just because storage is cheap and restraint apparently is not.
