# Pied Piper concept

## Product intent

Pied Piper gives eligible tamed Valheim creatures one consistent native command: **Follow / Stay**.

The goal is not to build a pet-management suite. It is to remove an arbitrary creature-by-creature limitation while preserving Valheim's own interaction, command RPC, AI, pathfinding, and riding behavior.

> One command. One behavior. Follow or stay.

## Player experience

Pied Piper should feel like Valheim simply learned how to command all of its tameable creatures consistently.

- look at a tamed creature;
- use Valheim's normal interact action, `E` by default;
- the creature toggles Follow / Stay through Valheim's native command path;
- wild creatures remain unaffected;
- interacting with the body of a saddled rideable still commands it;
- interacting with the saddle still mounts it because the saddle owns its own interaction path.

There is no separate command wheel, pet HUD, selection mode, or custom movement system.

## Visual language

Pied Piper intentionally adds almost no visual language of its own.

- preserve Valheim's native interaction prompts and messages;
- avoid persistent HUD elements;
- do not add selection outlines, formation markers, pet status panels, or map clutter;
- any future release artwork or Thunderstore icon should communicate tameable companionship and command without implying features the mod does not provide.

## Concept art

No dedicated concept art is currently committed.

A future Thunderstore/release icon may become the primary concept asset, but it must represent the actual Follow / Stay product rather than a broad "animal army" fantasy. Concept assets belong under `Assets/Concept/`.

## Screenshots

No curated runtime screenshots are currently committed under `Assets/Screenshots/`.

Useful future captures would show:

- a normally non-commandable tame receiving the Follow / Stay prompt;
- a saddled rideable commanded from the body;
- the saddle interaction still mounting normally;
- a wild creature remaining unaffected.

Generated mockups are not screenshots.

## Constraints and non-goals

Pied Piper does not provide:

- global or radius-based army commands;
- formations;
- teleport-to-player behavior;
- pet inventory or stat changes;
- breeding automation;
- protection systems;
- custom pathfinding;
- map markers or portal management;
- a general tame overhaul.

The mod does touch live tameable state and unlocks Valheim's native patrol-point persistence when a creature is told to Stay. Before uninstalling, every Pied Piper-commanded creature must be returned to Follow so its patrol point is cleared. See [README.md](README.md) and [STATUS.md](STATUS.md) for the verified behavior and removal procedure.