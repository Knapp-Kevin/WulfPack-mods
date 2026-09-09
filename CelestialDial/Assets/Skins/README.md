# Celestial Dial skin contract

Celestial Dial skins are presentation-only packages applied to a stable semantic instrument.

## Stable semantic slots

A skin may supply artwork or presentation values for:

1. `outer_frame`
2. `cycle_ring`
3. `sol_region`
4. `mani_region`
5. `dawn_marker`
6. `dusk_marker`
7. `celestial_indicator`
8. `day_plate`
9. `toggle_glyph`

The exact asset format is intentionally not frozen until the Unity UI proof establishes the most reliable loading path.

## Invariants

Every skin must preserve:

- Sól as the upper/day identity
- Máni as the lower/night identity
- the same normalized cycle position
- the same dawn and dusk semantics
- the same current-day value
- the same toggle behavior
- comparable readability at the supported UI scale

A skin must never reveal more gameplay information than another skin.

## Initial families

### Classic Wood

Aged carved wood, forged iron, restrained knotwork, warm Sól treatment and cool Máni treatment. This is the closest family to the first concept art.

### Rune Stone

Weathered stone, iron brackets, carved runes, restrained amber day accents and cool blue night accents.

### Bronze Astrolabe

Worn bronze and brass rings with leather accents and engraved celestial marks. It should feel like a crafted Norse instrument, not generic steampunk.

### Frostborn

Pale wood or stone, silver inlay, ice and restrained aurora detail. Day remains warm enough to read clearly; night becomes colder and more luminous.

## Anti-clutter rule

Skins change character, not information density. Decorative elements must not compete with the day number, cycle indicator, dawn/dusk transitions, or map/dial toggle.
