# Glossary

Canonical definitions for terms introduced by governed cycles in this
repository. Each entry names the operator-facing document that is its home.

## plugins-disabled

```yaml
term: plugins-disabled
home: RestedWhispers/README.md
introduced_in_plan: plan-rested-whispers-genesis
referenced_by:
  - RestedWhispers/README.md
  - RestedWhispers/build-local.ps1
  - docs/ARCHITECTURE_PLAN.md
definition: >
  The directory <Valheim>/BepInEx/plugins-disabled/, a sibling of
  BepInEx/plugins/ rather than a child of it. Rested Whispers is disabled by
  moving its folder here. BepInEx discovers plugins by scanning for *.dll
  across the plugins tree, so a "disabled" subfolder placed inside
  BepInEx/plugins/ would still be loaded; a sibling directory is never
  scanned, which holds whether or not the scan recurses. The name is a
  WulfPack convention, not a BepInEx feature - BepInEx has no knowledge of it
  and simply never looks there.
```

## owned path

```yaml
term: owned path
home: RestedWhispers/README.md
introduced_in_plan: plan-rested-whispers-genesis
referenced_by:
  - RestedWhispers/README.md
  - RestedWhispers/build-local.ps1
  - docs/ARCHITECTURE_PLAN.md
definition: >
  One of exactly three filesystem paths that build-local.ps1 is permitted to
  create, move, or delete: <Valheim>/BepInEx/plugins/RestedWhispers/,
  <Valheim>/BepInEx/plugins-disabled/RestedWhispers/, and
  <Valheim>/BepInEx/config/com.wulfpack.restedwhispers.cfg. Every deletion
  routes through a single guarded function that refuses a path whose final
  component is not the expected name, and refuses reparse points (junctions
  and symlinks) outright. Anything not on this list - including every other
  BepInEx plugin and every Thunderstore Mod Manager profile - is never
  enumerated, moved, or deleted.
```

## Heading-up orientation

```yaml
term: Heading-up orientation
home: RuneCompass/README.md
introduced_in_plan: plan-rune-compass-heading-up
referenced_by:
  - RuneCompass/README.md
  - RuneCompass/STATUS.md
  - RuneCompass/Assets/Skins/README.md
  - docs/ARCHITECTURE_PLAN.md
definition: >
  The Rune Compass display model in which screen-up is always the direction
  the player is looking. The rose rotates so each cardinal letter sits at its
  true bearing relative to the player's view, and a fixed lubber marker at the
  top of the dial marks the facing. The alternative, north-up, pins the letters
  to the widget and rotates a needle instead; that model was replaced because it
  cannot satisfy "N/E/S/W align with actual world orientation" and because it is
  not how Valheim's own HUD draws direction. Implemented as a single
  heading-dependent transform: the rose takes z = +heading, and anything mounted
  on it carries a pure world bearing.
```

## Rose (compass rose)

```yaml
term: Rose (compass rose)
home: RuneCompass/README.md
introduced_in_plan: plan-rune-compass-heading-up
referenced_by:
  - RuneCompass/README.md
  - RuneCompass/CompassUI.cs
  - docs/ARCHITECTURE_PLAN.md
definition: >
  The rotating compass card in the Rune Compass HUD - the RectTransform that
  carries the cardinal letters and the wind needle. It is the only
  heading-dependent transform in the mod: it is rotated by z = +heading, so a
  child placed at z = -bearing renders at heading - bearing and the heading term
  cancels. Anchored and pivoted at (0.5, 0.5) with zero sizeDelta, so it spins
  about the panel centre rather than orbiting a corner as an unconfigured
  RectTransform would.
```

## Lubber marker

```yaml
term: Lubber marker
home: RuneCompass/README.md
introduced_in_plan: plan-rune-compass-heading-up
referenced_by:
  - RuneCompass/README.md
  - RuneCompass/CompassUI.cs
  - RuneCompass/Assets/Skins/README.md
definition: >
  The static marker at the top of the Rune Compass dial indicating where the
  player is looking, named for the lubber line of a real marine compass. It never
  rotates: under heading-up the facing is always screen-up, so the marker replaces
  the rotating heading needle used by the earlier north-up design. It is a child
  of the panel rather than of the rose, so the card turns beneath it.
```
