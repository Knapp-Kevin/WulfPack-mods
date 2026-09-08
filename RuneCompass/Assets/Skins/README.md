# Rune Compass skins

Rune Compass skins are presentation-only asset bundles. They must not alter navigation logic, wind semantics, configuration authority, or gameplay state.

Each skin should eventually contain:

```text
<SkinName>/
├── base.png
├── ring.png
├── heading_pointer.png
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
