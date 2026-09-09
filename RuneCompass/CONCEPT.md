# Rune Compass concept

## Product intent

Rune Compass is an immersive navigation instrument for Valheim No Map play. Its rule is intentionally narrow:

> Direction, not hidden information.

It should help the player orient themselves without becoming a minimap, GPS, route planner, hidden-location detector, or general navigation HUD.

## Player experience

Rune Compass occupies the top-right HUD region where the minimap normally lives. The card is north-up: north remains fixed at 12 o'clock while directional signals move against it.

The information hierarchy is:

1. **Character facing** — the primary needle, center-mounted and visually dominant.
2. **North/cardinal reference** — fixed on the dial face.
3. **Camera view** — a quiet semi-transparent sector behind the primary needle.
4. **Wind source** — a small rune outside the rim, deliberately subordinate to heading.

In storms, the character-facing and camera indicators can be captured by interference while the fixed card remains stable. The wind signal stays truthful because wind is directly observable in the world.

A persistent instrument toggle near the minimap region is part of the intended interaction contract. It must remain available even in No Map play where Valheim's minimap surface is absent. The control is its own HUD affordance, not a child of the minimap that disappears with it.

## Visual language

Rune Compass should feel like a compact physical Norse navigation instrument rather than a modern HUD widget.

- fixed circular card with clear cardinal hierarchy;
- one bold character-facing needle;
- camera view shown as a **semi-transparent HUD sector**, not a physical wedge cut from the instrument;
- small external wind rune or gust glyph;
- restrained wood, forged metal, stone, rune, or minimal Nordic materials depending on skin;
- no color-only distinctions between signals;
- readable at the mod's supported HUD scale.

Skins are presentation-only. They cannot change what a signal means, add information, or alter interference behavior.

The currently wired skin slots are documented in [Assets/Skins/ASSET_INDEX.md](Assets/Skins/ASSET_INDEX.md). The missing shipping art is three assets per skin: camera wedge, character arrow, and wind gust.

## Concept art

No single canonical Rune Compass concept sheet is currently committed. Existing skin assets under `Assets/Skins/` are implementation assets, not proof that the final visual hierarchy is complete.

Future concept work should first establish the hierarchy of the complete instrument, then derive skin variants. It must not invent replacement WulfPack logos or treat decorative mockups as runtime evidence.

## Screenshots

Operator screenshots were used during development to validate orientation and visual behavior, but no curated runtime screenshots are currently committed under `Assets/Screenshots/`.

When curated screenshots are added, they should include at minimum:

- clear-weather north-up hierarchy;
- character-facing versus camera-sector separation;
- wind rune placement;
- storm interference behavior;
- No Map placement and persistent toggle behavior once that control is implemented.

Generated or composited mockups are not screenshots.

## Constraints and non-goals

Rune Compass does not provide:

- minimap replacement or map tiles;
- pins, route guidance, home bearing, trader/boss/player tracking, or Vegvisir discovery;
- hidden-information advantages;
- server authority or save/world persistence;
- skin-specific behavior;
- a camera wedge rendered as a solid physical slice of the compass.

The persistent toggle requirement is a product contract, not evidence that the current runtime already implements it. Implementation truth remains in [STATUS.md](STATUS.md).