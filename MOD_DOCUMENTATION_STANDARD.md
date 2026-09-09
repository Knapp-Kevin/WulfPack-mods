# WulfPack mod documentation standard

Every top-level mod in this repository has three different documentation jobs. They must stay separate so concept art, implementation truth, and runtime evidence do not get confused with one another.

## Required files

Every mod folder containing `manifest.json` must contain:

- `README.md` — user-facing purpose, current behavior, configuration, installation, compatibility, and current release/readiness state.
- `CONCEPT.md` — product intent, visual language, concept art, screenshot evidence, and explicit non-goals.
- `STATUS.md` when the mod has implementation work whose verification state is still moving. A completed small mod may keep its completion evidence in the README instead, but `CONCEPT.md` is still required.

## Required `CONCEPT.md` sections

Every `CONCEPT.md` must contain these second-level headings, in this order:

1. `## Product intent`
2. `## Player experience`
3. `## Visual language`
4. `## Concept art`
5. `## Screenshots`
6. `## Constraints and non-goals`

Additional sections are allowed when they add information rather than repeating the README.

## Asset locations

Use these locations when the corresponding assets exist:

```text
<ModName>/
├── CONCEPT.md
└── Assets/
    ├── Concept/       concept art, visual studies, mockups
    └── Screenshots/   actual captures from a running Valheim build
```

Do not create fake screenshots to fill the structure. Empty asset directories do not need placeholder files. The `CONCEPT.md` screenshot section should simply state that no committed runtime screenshots exist yet.

## Evidence boundary

Concept art and screenshots are different evidence classes.

- **Concept art** shows intended visual direction. It may contain features that are not implemented yet and must be described as such.
- **Screenshots** are captures of actual runtime behavior. A generated mockup, composite, render, or annotated concept image must never be labeled as a screenshot.
- **README/STATUS claims** must be based on implementation and validation evidence, not on what concept art depicts.

When screenshots are added, include a short caption saying what build/state they demonstrate. Do not use screenshots as proof of behavior they do not visibly establish.

## Visual consistency

A mod may have its own visual language, but concept work must preserve the repository-wide principles:

- readable at Valheim HUD scale
- no invented WulfPack branding or replacement logos
- use the repository's existing `banner.png` when repository-level branding is needed
- presentation assets must not silently change gameplay semantics
- reusable skins must preserve the same semantic slots and behavior
- concept pages should show the intended hierarchy before decorative variants

## Updating a mod

When a new user-visible component, HUD element, skin slot, interaction, or visual state is introduced:

1. update the implementation and status documentation that describes what is actually true;
2. update `CONCEPT.md` if the product or visual contract changed;
3. add or update concept assets only when they clarify the intended design;
4. add runtime screenshots only after the feature has been observed in-game;
5. update any asset index/skin contract affected by the change.

A component is not documentation-complete if its implementation exists but the README and concept contract still describe the old behavior.

## New-mod gate

A new mod is not documentation-complete until:

- its `README.md` exists;
- its `CONCEPT.md` exists with all required sections;
- the root README lists both pages;
- its risk tier is declared in the root README;
- concept art and screenshots, if present, are stored in the correct asset class;
- the repository documentation verifier passes.

Run:

```powershell
.\verify-mod-docs.ps1
.\verify-repo.ps1
```

The first command enforces this documentation contract. The second enforces repository/risk-tier consistency.