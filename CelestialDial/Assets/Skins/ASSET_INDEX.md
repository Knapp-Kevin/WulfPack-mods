# Celestial Dial asset index

This index defines the interchangeable presentation slots for Celestial Dial. Every skin must preserve the same instrument semantics.

## Stable semantic slots

| Slot | Meaning | Rule |
| --- | --- | --- |
| `outer_frame` | decorative bezel/instrument frame | presentation-only; must not crowd the day plate |
| `cycle_ring` | circular scale for normalized day/night-cycle position | same cycle semantics in every skin |
| `sol_region` | upper/day treatment | Sól always belongs on the upper half |
| `mani_region` | lower/night treatment | Máni always belongs on the lower half |
| `dawn_marker` | transition into daylight | distinct from dusk, restrained size |
| `dusk_marker` | transition into night | distinct from dawn, restrained size |
| `celestial_indicator` | moving marker for current cycle position | primary motion element |
| `day_plate` | center plate containing current world day | immediate readability required |
| `toggle_glyph` | persistent map/dial control | remains available even when the minimap is absent in No Map play |

## File naming

For a skin named `<SkinName>`:

```text
<SkinName>/
├── outer-frame.png
├── cycle-ring.png
├── sol-region.png
├── mani-region.png
├── dawn-marker.png
├── dusk-marker.png
├── celestial-indicator.png
├── day-plate.png
└── toggle-glyph.png
```

Transparent PNG is preferred for component assets.

## Invariants

Every skin must preserve:

- Sól as the upper/day identity;
- Máni as the lower/night identity;
- the same normalized cycle position;
- the same dawn and dusk semantics;
- the same world-day value and readable placement;
- the same toggle behavior;
- comparable readability at supported UI scale.

Skins may change material, ornament, color treatment, typography treatment, pointer style, and celestial decoration. They may not reveal extra information or alter game behavior.

## Initial families

- **Classic Wood** — carved aged wood, forged iron, restrained Norse ornament.
- **Rune Stone** — weathered stone with carved runes and restrained luminous accents.
- **Bronze Astrolabe** — worn bronze/brass celestial instrument language without drifting into generic steampunk.
- **Frostborn** — pale wood or stone, silver inlay, ice, and restrained aurora accents.

## Generation strategy

Do not generate every skin at once. Finish one complete component set, review it as an assembled dial, then propagate the approved semantic shapes into the next material family.

Recommended first pass:

1. `ClassicWood/toggle-glyph.png`
2. `ClassicWood/day-plate.png`
3. `ClassicWood/celestial-indicator.png`
4. `ClassicWood/cycle-ring.png`
5. `ClassicWood/dawn-marker.png`
6. `ClassicWood/dusk-marker.png`
7. `ClassicWood/sol-region.png`
8. `ClassicWood/mani-region.png`
9. `ClassicWood/outer-frame.png`

The concept image is a visual target, not a flattened shipping texture. Production assets should remain independently swappable and animatable.

See [../../CONCEPT.md](../../CONCEPT.md) for the product/visual contract.