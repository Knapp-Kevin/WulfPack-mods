# Rested Whispers concept

## Product intent

Rested Whispers is a deliberately small quality-of-life mod that helps the player notice the existing Rested effect before it expires. It does not create a new survival system. It observes Valheim's existing state and gives a small amount of timely feedback.

The product promise is simple: warn gently, then get out of the way.

## Player experience

The player should forget the mod is installed until a warning is useful.

Default behavior:

- one native Valheim notification at about two minutes of Rested time remaining;
- one final native notification at about thirty seconds remaining;
- no duplicate expiry message because Valheim already announces the end of Rested.

There is no permanent HUD element, meter, timer, widget, or extra status icon.

## Visual language

Rested Whispers should look like Valheim, not like a separate overlay package.

- use native Valheim message presentation;
- keep language brief and atmospheric;
- avoid custom frames, meters, progress bars, or persistent UI;
- preserve readability without adding visual weight.

The visual identity comes from restraint rather than custom ornament.

## Concept art

No dedicated concept art is required for the current product because the intended presentation is Valheim's own native notification surface. If future work introduces custom visual treatment, concept art belongs under `Assets/Concept/` and must not imply a permanent HUD unless that product decision is explicitly made.

## Screenshots

No runtime screenshots are currently committed to this repository.

When screenshots are added, they should show the actual warning as rendered in a live Valheim build and be stored under `Assets/Screenshots/`. Generated mockups are not screenshots.

## Constraints and non-goals

Rested Whispers does not:

- change Rested duration or comfort calculations;
- add a tired status effect;
- change player stats;
- write world or character state;
- duplicate Valheim's own Rested-expired message;
- become a general status-effect notification framework;
- add a permanent HUD timer.

See [README.md](README.md) for current behavior and configuration.