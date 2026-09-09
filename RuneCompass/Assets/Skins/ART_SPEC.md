# Rune Compass — skin art specification

What to draw, how to orient it, and what the mod does with it. Every rule here exists
because getting it wrong produces a specific, known failure, and each one names that failure.

## The universal rules

**Canvas: 512 × 512 RGBA, transparent background, every time.** No exceptions, including for
the small glyphs. The mod mounts every layer centred on the same square and rotates it about
the canvas centre, so a glyph's *position on its canvas* is what places it on the dial. This
is what lets the wind rune orbit outside the ring without its art knowing the orbit radius.

**Up on the canvas is bearing 0.** Every rotating layer is handed a world bearing and turned
to it. Draw the thing pointing at 12 o'clock and the mod does the rest. Draw it pointing any
other way and every reading in the game is wrong by that angle.

**Draw about the centre.** The pivot is the canvas centre, always. A glyph drawn off-centre
orbits rather than spins — which is deliberate for the wind rune and a defect for everything
else.

**No baked drop shadows on rotating layers.** A shadow drawn into the texture rotates with
it, so a light source that starts top-left ends up bottom-right. The ring and base may carry
lighting; the pointers may not.

---

## The three assets needed

Nine files: three assets across three skins. Sizes are the fraction of the 512 px canvas the
artwork should occupy, and they are **prominence rules, not suggestions** — the hierarchy is
the whole design, and art that ignores it defeats the layering the mod does correctly.

### 1. `character_arrow.png` → `characterArrowTexture`

**This is the compass's needle and its primary read.** It shows which way the *character* is
facing, not north. It should be the boldest, most confident thing on the dial.

- **Occupies:** roughly the central 45% of the canvas — about 230 px tall, centred.
- **Points:** tip at 12 o'clock, tail at or near the centre.
- **Reads as:** a single decisive pointer. Warm metal, a rune-etched blade, a bronze arrow —
  something that looks like it means it.
- **Must not:** be symmetric end-to-end. The tip has to be unmistakable from the tail at a
  glance, because during a storm it swings and the player needs to know which end is
  speaking.
- **Currently:** a plain orange rectangle drawn by the mod. Functional, ugly.

### 2. `camera_wedge.png` → `cameraWedgeTexture`

The direction the **camera** is looking. Deliberately quiet — background information the
player consults rather than reads.

- **Occupies:** roughly 86% of the canvas, a sector centred on 12 o'clock spanning about
  **60° total** (30° either side).
- **Reads as:** a soft cone or a pair of bracketing marks fanning out from the centre. Think
  a lantern beam, or the shaded field-of-view wedge on a map.
- **Must be visually subordinate to the arrow.** Low contrast, low opacity — roughly 25-35%
  alpha at its strongest. If the eye lands on this before the arrow, it is wrong.
- **Must not:** have a hard bright outline, which turns a background hint into a foreground
  element.
- **Currently:** two faint ticks at the sector edges. Adequate, not lovely.

### 3. `wind_gust.png` → `windGustTexture`

A small rune that **orbits outside the ring**, marking the quarter the wind comes **from**.

- **Occupies:** a small glyph roughly **13% of the canvas** (about 65 px), and — importantly
  — **drawn at the canvas centre, not at the rim.** The mod offsets it outward by 56% of the
  dial. Draw it at the rim and it will orbit at double the radius, off the edge of the HUD.
- **Points:** if the glyph has a direction, its "away" edge faces 12 o'clock.
- **Reads as:** weather, not instrumentation — a gust curl, a spiral, a small storm rune. It
  sits *outside* the ring, so it is a mark on the horizon rather than part of the dial.
- **Must stay small.** Roughly a third of the arrow's visual weight. Wind must never compete
  with facing for attention; that ranking is the design.
- **Must not:** be an arrow or anything that reads as pointing *along* the wind. It marks a
  quarter — where the weather is coming from — the way a nor'easter is named. An arrow would
  contradict its own position.

---

## Per-skin palettes

Sampled from each skin's existing `ring.png` and `base.png`, so new pieces sit with the art
already there.

### ClassicWood — warm weathered wood, aged brass

| role | swatch |
|---|---|
| base timber | `#483018`, `#603018` |
| deep shadow | `#301800` |
| **brass, for the arrow** | `#a87818` |

Existing style: painted, dimensional, aged metal with visible wear. The wind pointer carries
a small teal inlay (`#3d8a8a`-ish) as its one cool accent — worth echoing on the gust rune.

### MinimalNordic — pale, clean, cool-accented

| role | swatch |
|---|---|
| pale wood | `#a87848`, `#c0a878` |
| warm brass | `#c09048` |
| **steel blue accent** | `#6090a8` |

The only skin with a genuine cool accent already in its indicator art. The camera wedge
almost certainly wants to be that steel blue here.

### RuneRing — near-black, carved stone, ember

| role | swatch |
|---|---|
| dark stone | `#181818`, `#303018` |
| oxblood | `#301818` |
| **brass/ember** | `#a87818` |

Highest contrast of the three, so it can carry the most ornamented glyphs.

---

## RuneRing is not currently its own skin

Worth fixing while art is being made. Verified by hash:

```text
wind_pointer.png    ClassicWood d4e8de18  ==  RuneRing d4e8de18
lubber_marker.png   ClassicWood 0dac3199  ==  RuneRing 0dac3199
```

Only `ring.png` and `base.png` differ. RuneRing is presently ClassicWood's indicators on a
different ring, so "three interchangeable skins" overstates what ships. If the three new
assets are authored per-skin, that resolves itself for everything that still renders — and
`wind_pointer.png` / `lubber_marker.png` become dead files, since nothing mounts them any
more.

---

## Superseded assets

Both are still read by the loader and **neither is rendered**. They can be deleted once the
new art lands.

| file | why it is dead |
|---|---|
| `wind_pointer.png` | The centre-mounted wind spear. Wind moved to a small rune orbiting outside the rim; a centre-mounted pointer no longer has a slot. |
| `lubber_marker.png` | A fixed rim chevron from the abandoned heading-up model. Under north-up the character arrow carries facing, and it is drawn at the rim rather than the centre, so it cannot stand in. |

---

## Dropping the files in

No code change is needed. Add the filename to each skin's `skin.json`:

```json
"characterArrowTexture": "character_arrow.png",
"cameraWedgeTexture":    "camera_wedge.png",
"windGustTexture":       "wind_gust.png"
```

An empty string or a missing file falls back to the mod's primitive geometry with **identical
motion**, so a skin can be finished one asset at a time and stays playable throughout. A
missing file logs a warning and costs nothing else.

Then `build-local.ps1 -Install` and check the log for
`Rune Compass skin loaded: <name>` with no texture warnings.
