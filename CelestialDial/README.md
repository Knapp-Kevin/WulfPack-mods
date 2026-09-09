# Celestial Dial

**Read the sky. Know the day.**

Celestial Dial is a focused Valheim HUD mod concept that turns the minimap footprint into a toggleable celestial timekeeper. It answers exactly two questions:

1. What world day is it?
2. Where are we in the current day/night cycle?

It does not translate Valheim into conventional clock time. The instrument represents the world's own continuous cycle from dawn through day, dusk, night, and back to dawn.

## Product boundary

Celestial Dial should:

- display the current Valheim world day
- display continuous position within the complete day/night cycle
- live in the same visual region as the minimap
- expose a small map/dial toggle near the upper-right of that region
- use a circular instrument surface rather than another rectangular HUD panel
- keep **Sól** visually associated with the upper/day half
- keep **Máni** visually associated with the lower/night half
- support presentation-only skins
- remain client-side and read-only

Celestial Dial should not:

- change world time or day length
- change weather, lighting, seasons, or environment state
- write world, character, or ZDO state
- introduce server authority
- expose hidden map information
- become a calendar, alarm system, stopwatch, or general HUD framework
- display modern HH:MM clock time

## Core interaction

The minimap remains the normal state. A small circular control near its upper-right edge toggles the Celestial Dial surface.

The implementation should prefer a separate UI surface anchored over the minimap region rather than modifying minimap internals. When the dial is visible, the map can remain intact underneath and simply be hidden by presentation state. Toggling back restores the normal map.

A short flip or crossfade is appropriate if it remains responsive and does not obscure gameplay.

## Dial semantics

The dial has stable semantic components regardless of skin:

| Component | Meaning |
| --- | --- |
| Outer frame | presentation-only skin surface |
| Cycle ring | normalized position through the Valheim day/night cycle |
| Sól region | daylight half, visually dominant above the horizon |
| Máni region | night half, visually dominant below the horizon |
| Dawn marker | transition into daylight |
| Dusk marker | transition into night |
| Celestial indicator | current position in the cycle |
| Day plate | current world day number |
| Toggle glyph | switch between minimap and Celestial Dial |

The underlying semantic model must not depend on any particular artwork.

## Concept art

![Celestial Dial Sól and Máni concept](Assets/Concept/celestial-dial-sol-mani-concept.jpg)

The concept establishes the intended hierarchy rather than final shipping pixels:

- Sól belongs to the top/day half
- Máni belongs to the bottom/night half
- day and night read as one continuous circular mechanism
- the day number remains immediately legible
- the current cycle position is shown by a moving indicator
- dawn and dusk form explicit transition points
- the toggle is visually connected to the instrument without becoming part of the dial itself

## Skin system

Skins are presentation-only. They may change material, ornament, texture, typography treatment, pointer art, and celestial decoration. They may not change what any indicator means, reveal additional information, or alter game state.

See [Assets/Skins/README.md](Assets/Skins/README.md) for the initial skin contract.

## Initial skin families

The first useful families are:

- **Classic Wood**: carved aged wood and forged iron, closest to the current concept
- **Rune Stone**: weathered carved stone with restrained luminous rune accents
- **Bronze Astrolabe**: worn bronze and brass celestial instrument, still grounded in Norse fantasy
- **Frostborn**: pale wood/stone, silver, ice, and restrained aurora accents

These are not four separate implementations. They are four presentations of the same dial state.

## Technical posture

Celestial Dial is intended to remain in the repository's **read-only** risk tier. Implementation must establish authoritative Valheim 1.0 sources for:

- current world day
- normalized position in the current day/night cycle
- HUD/minimap anchoring and lifecycle

Historical mod source may inform discovery, but installed Valheim 1.0 assemblies are the contract.

No gameplay code has been claimed complete in this framework slice.
