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
