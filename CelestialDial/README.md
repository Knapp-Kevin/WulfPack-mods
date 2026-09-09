# Celestial Dial

**Read the sky. Know the day.**

Celestial Dial is a focused Valheim HUD mod concept that turns the minimap region into a toggleable Sól and Máni timekeeper. It answers exactly two questions:

1. What world day is it?
2. Where are we in the current day/night cycle?

It does not translate Valheim into conventional clock time. The instrument represents the world's own continuous cycle from dawn through day, dusk, night, and back to dawn.

See [CONCEPT.md](CONCEPT.md) for the visual/product contract and corrected concept art. Concept art is intentionally kept out of this implementation-facing README so it cannot be mistaken for runtime evidence.

## Product boundary

Celestial Dial should:

- display the current Valheim world day;
- display continuous position within the complete day/night cycle;
- live in the same visual region as the minimap;
- expose a small persistent map/dial toggle near the upper-right of that region;
- keep that toggle available in No Map play even when the minimap itself is absent;
- use a circular instrument surface rather than another rectangular HUD panel;
- keep **Sól** visually associated with the upper/day half;
- keep **Máni** visually associated with the lower/night half;
- support presentation-only skins;
- remain client-side and read-only.

Celestial Dial should not:

- change world time or day length;
- change weather, lighting, seasons, or environment state;
- write world, character, or ZDO state;
- introduce server authority;
- expose hidden map information;
- become a calendar, alarm system, stopwatch, or general HUD framework;
- display modern `HH:MM` clock time.

## Core interaction

Where maps are available, the minimap remains the normal state. A small circular control near its upper-right edge toggles the Celestial Dial surface.

The implementation should own a separate UI surface anchored over the minimap region rather than modifying minimap internals. The map can remain intact underneath and simply be hidden by presentation state.

In No Map play the map surface may be absent, but the toggle remains its own HUD control so the player can still open the Celestial Dial.

A short flip or crossfade is appropriate if it remains responsive and does not obscure gameplay.

## Dial semantics

The semantic model is stable regardless of skin:

| Component | Meaning |
| --- | --- |
| Outer frame | presentation-only instrument frame |
| Cycle ring | normalized position through the Valheim day/night cycle |
| Sól region | daylight half, above the horizon |
| Máni region | night half, below the horizon |
| Dawn marker | transition into daylight |
| Dusk marker | transition into night |
| Celestial indicator | current position in the cycle |
| Day plate | current world day number |
| Toggle glyph | switch between minimap and Celestial Dial |

See [Assets/Skins/ASSET_INDEX.md](Assets/Skins/ASSET_INDEX.md) for the component index and generation order.

## Skin system

Skins are presentation-only. They may change material, ornament, texture, typography treatment, pointer art, and celestial decoration. They may not change what any indicator means, reveal additional information, or alter game state.

Initial families:

- **Classic Wood**
- **Rune Stone**
- **Bronze Astrolabe**
- **Frostborn**

These are four presentations of one semantic dial, not four implementations.

## Technical posture

Celestial Dial is intended to remain in the repository's **read-only** risk tier. Implementation must establish authoritative Valheim 1.0 sources for:

- current world day;
- normalized position in the current day/night cycle;
- HUD/minimap anchoring and lifecycle;
- persistent toggle behavior in both map and No Map play.

Historical mod source may inform discovery, but installed Valheim 1.0 assemblies are the contract.

## Current state

The product boundary, concept hierarchy, and skin component contract are established. Gameplay implementation has not started, and no runtime screenshot is claimed.

See [STATUS.md](STATUS.md) for the working state and [CONCEPT.md](CONCEPT.md) for intended visuals.