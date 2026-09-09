# Celestial Dial concept

## Product intent

Celestial Dial is a compact Sól and Máni themed Valheim timekeeper. It answers exactly two questions:

1. What world day is it?
2. Where are we in the current day/night cycle?

It should feel like a celestial instrument built into the HUD, not a modern clock with Norse decoration pasted onto it.

## Player experience

The normal minimap region becomes a toggleable instrument surface.

- the minimap remains the normal state where maps are available;
- a small persistent control near the upper-right of the minimap region switches to the dial;
- the dial occupies essentially the same footprint rather than adding another permanent HUD panel;
- the control remains available in No Map play even when the map itself is absent;
- the dial shows the current Valheim day and continuous position through the world's own cycle;
- no conventional `HH:MM` clock time is shown.

The intended interaction is quick enough to feel like turning over a physical instrument, not opening a menu.

## Visual language

The instrument is a continuous circular day/night mechanism.

- **Sól belongs on the upper/day half.**
- **Máni belongs on the lower/night half.**
- dawn and dusk are explicit transition points on the horizon line;
- a moving celestial indicator shows current cycle position;
- the current day remains immediately legible at the center;
- the toggle sits outside the semantic dial so it does not compete with the time display.

The first skin family should feel carved, weathered, and appropriate to Valheim. Later skins may use Rune Stone, Bronze Astrolabe, and Frostborn treatments, but every skin must preserve the same semantic slots.

See [Assets/Skins/README.md](Assets/Skins/README.md) for the skin contract.

## Concept art

![Celestial Dial Sól and Máni concept](Assets/Concept/celestial-dial-sol-mani-concept.jpg)

This is **concept art**, not a runtime screenshot and not final shipping artwork. It establishes the hierarchy that future implementation and skin assets must preserve:

- Sól on top;
- Máni on bottom;
- day and night as one continuous circular mechanism;
- current day in the center;
- a moving cycle indicator;
- persistent toggle near the upper-right.

Production assets should be generated as individual interchangeable components rather than flattening this whole image into one texture.

## Screenshots

No runtime Celestial Dial screenshots exist yet because gameplay implementation has not started.

When implementation reaches a playable state, actual captures belong under `Assets/Screenshots/` and should show at minimum:

- the normal map state;
- the dial state;
- No Map behavior with the persistent toggle still available;
- dawn, daylight, dusk, and nighttime positions;
- at least one alternate skin after the skin loader exists.

A generated mockup or concept sheet is never a screenshot.

## Constraints and non-goals

Celestial Dial must remain read-only and client-side. It does not:

- change time, day length, weather, lighting, seasons, saves, world state, or ZDOs;
- expose hidden map information;
- become a calendar, alarm, stopwatch, or generic HUD framework;
- display conventional clock time;
- let skins alter the meaning of day, night, dawn, dusk, or the current cycle position.

Implementation truth belongs in [STATUS.md](STATUS.md); this page owns the intended product and visual contract.