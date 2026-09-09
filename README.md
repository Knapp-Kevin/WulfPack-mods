![WulfPack Mods banner](banner.png)

# WulfPack Mods

A maintained monorepo for focused Valheim mods developed under the WulfPack name.

WulfPack Mods is the in-game modding side of the broader WulfPack Valheim project family. It is intentionally separate from [WulfPack Forge](https://github.com/Knapp-Kevin/WulfPackForge), the external character-editor companion application, while keeping the two projects easy to discover from one another.

## Table of contents

- [Current mods](#current-mods)
  - [Rested Whispers](#rested-whispers)
  - [Rune Compass](#rune-compass)
  - [Pied Piper](#pied-piper)
  - [Vidar Shrugged](#vidar-shrugged)
- [Repository structure](#repository-structure)
- [Project family](#project-family)
- [Design principles](#design-principles)
- [Local build and validation policy](#local-build-and-validation-policy)
- [Adding another mod](#adding-another-mod)
- [Mod risk tiers](#mod-risk-tiers)
- [Status](#status)

## Current mods

| Mod | Purpose | Status | Documentation |
| --- | --- | --- | --- |
| **Rested Whispers** | Gentle, native Valheim warnings as the Rested effect fades. | ✅ Implemented, tested, validated | [README](RestedWhispers/README.md) |
| **Rune Compass** | Immersive No Map navigation: north-up compass card with heading needle and live wind. | 🧪 Built, installed and rendering in-game with ClassicWood; operator acceptance pass outstanding | [README](RuneCompass/README.md) · [Implementation plan](RuneCompass/IMPLEMENTATION_PLAN.md) · [Status](RuneCompass/STATUS.md) |
| **Pied Piper** | One consistent Follow / Stay command for eligible tamed creatures. | 🧱 Repository mesh established; API discovery next | [README](PiedPiper/README.md) · [Implementation plan](PiedPiper/IMPLEMENTATION_PLAN.md) · [Status](PiedPiper/STATUS.md) |
| **Vidar Shrugged** | Large-settlement performance instrumentation and optimization. | 🧪 Gate 0 foundation merged; runtime validation open | [README](VidarShrugged/README.md) · [Implementation plan](VidarShrugged/IMPLEMENTATION_PLAN.md) · [Benchmark plan](VidarShrugged/BENCHMARK_PLAN.md) · [Status](VidarShrugged/STATUS.md) |

## Mod risk tiers

What a mod is allowed to touch decides how hard it has to be validated. The tier is the
distinction that matters here — not which repository a mod lives in. A mod that patches
live game objects is a different proposition from one that reads state and draws a HUD,
and it earns a stricter gate regardless of where its folder sits.

| Mod | Tier | Touches | Gate required |
| --- | --- | --- | --- |
| **Rested Whispers** | read-only | reads status effects, shows native messages | build, load, lifecycle containment |
| **Rune Compass** | read-only | reads camera heading and wind, draws a HUD | build, load, lifecycle containment |
| **Vidar Shrugged** | read-only | reads frame timings and scene population | build, load, lifecycle containment |
| **Pied Piper** | state-touching | Harmony patches on `Tameable`; issues commands to live creatures | build, load, lifecycle containment, **plus a save-integrity diff across a play session with the patches live** |

### Tier definitions

- **read-only** — observes game state and may draw UI. No Harmony patches, no mutation of
  game objects, no writes to save or world data.
- **state-touching** — patches or mutates live game objects during a session. Cannot be
  assumed harmless to a character or world just because uninstall is clean; the risk is
  what happens *while it runs*, not what it leaves behind.
- **persistent** — writes to save, world, or ZDO state that survives the session. Nothing
  is in this tier, and nothing should enter it without an explicit decision recorded here.

### Why tier and not repository

Splitting a state-touching mod into its own repository does not make its patches safer. The
protection is per-mod containment, which every mod already has and which is measured rather
than assumed: its own plugin GUID, its own install path, its own config, deletes guarded by
leaf name, and a SHA-256 diff proving save data is byte-identical across a full
install/disable/enable/uninstall cycle.

Fragmenting also costs something concrete. `build-local.ps1` is a shared template, and a
parse-time defect in it once propagated from Rune Compass into Pied Piper unnoticed. In one
repository that is a single fix and a single scan. Across four it is four divergent copies
with nowhere to fix them at once.

Vidar Shrugged is the case that settles it: its plan already names `ZSyncTransform`
suppression, so it becomes state-touching the moment Gate 1 starts. If Harmony justified a
separate repository, the monorepo would dissolve by attrition.

### Keeping this table honest

A table nobody checks is a table that drifts. `verify-repo.ps1` enforces it locally:

```powershell
.\verify-repo.ps1
```

It fails when:

- a mod folder exists with no row here, or a row names a folder that does not exist;
- a mod declared **read-only** references Harmony or contains patch attributes — the claim
  is checked against the code, not taken on trust;
- the table parses to zero rows, which would otherwise let a broken parser report success
  forever.

That last one is not hypothetical. A governance check in this repository once parsed zero
rows for its entire life because the values it read were wrapped in bold markup, and every
report it produced was hand-written instead. A gate that reports nothing has not passed —
it has not run.

It also checks that the documentation above is runnable:

- **no control characters in tracked text.** Authoring escape processing turns a lost
  backslash into a control character — a `\v` becomes a vertical tab, a `\b` a backspace, and
  the letter goes with it. Rendered markdown still looks almost right, so this file spent
  several commits telling readers to run a save-integrity command that did not exist, and
  Pied Piper's README named an uninstall script that did not exist. Both looked correct
  until the bytes were examined.
- **every script named in a markdown file resolves to a real script.** A plain typo in a
  documented command is the same failure reached by a different route.

The reference check reads markdown only, deliberately. Source files and this gate itself
mention `.ps1` names incidentally, and counting those would keep the parsed-nothing guard
permanently satisfied — which is the very defect it exists to prevent.

A state-touching mod additionally has to show that a session with its patches live leaves
character and world data sound. `verify-save-integrity.ps1` makes that runnable:

```powershell
.\verify-save-integrity.ps1 -Baseline     # before playing
.\verify-save-integrity.ps1 -Compare      # after playing
```

It reports loss, truncation, implausible shrinkage and orphaned worlds — a `.fwl` with no
`.db`, or the reverse, either of which makes a world stop appearing in the menu. It
deliberately does **not** fail on files simply changing: a session that changes saves is a
session where someone played. Treating that as a finding would make the gate noise, and a
gate that always fires is ignored just as surely as one that never does.

Raising a tier is a deliberate act: change the row, extend the gate, and say in the mod's
`STATUS.md` what new evidence the higher tier now demands.

### Rested Whispers

A lightweight client-side quality-of-life mod that watches the existing Rested effect and gives timely native Valheim notifications before it expires.

Key characteristics:

- configurable first and final warnings
- no duplicate expiry message because Valheim already announces expiry
- native Valheim messaging
- client-side only
- BepInEx 5
- no save or world-state mutation
- local install / disable / enable / status / uninstall workflow
- independently removable before joining servers that prohibit third-party mods

Rested Whispers has been built, tested, validated in-game, and accepted as the first completed WulfPack mod proof of capability.

→ [Rested Whispers documentation](RestedWhispers/README.md)

### Rune Compass

Rune Compass is the second resident mod and the next step up in implementation complexity. It is designed for immersive No Map play without turning navigation into a minimap, GPS overlay, or hidden-information system.

Current implementation includes:

- cardinal orientation and player/camera heading
- live wind direction as a visually distinct secondary signal
- wind represented as the direction it is blowing **toward**
- No Map-aware visibility by default, with config override
- configurable enable state, position, scale, opacity, and heading calibration
- primitive custom HUD used as the technical proof before final skin binding
- local install / disable / enable / status / uninstall tooling
- no save or world-state mutation
- no server authority in the current slice

Rune Compass follows one rule: **direction, not hidden information.** It does not currently include map pins, route guidance, home bearing, trader or boss tracking, player tracking, Vegvisir integration, or hidden-location discovery.

The final visual system is designed around interchangeable presentation-only skins. Planned initial families are **Classic Wood**, **Rune Ring**, and **Minimal Nordic**. Skin assets change appearance only, never navigation semantics or gameplay behavior.

→ [Rune Compass documentation](RuneCompass/README.md)  
→ [Implementation plan](RuneCompass/IMPLEMENTATION_PLAN.md)  
→ [Current status](RuneCompass/STATUS.md)

### Pied Piper

Pied Piper is the third resident mod. Its entire job is to make eligible tamed creatures obey one consistent **Follow / Stay** command without turning into a general pet-management framework.

Initial target creature families:

- wolf
- boar
- hen / chicken
- lox
- asksvin

The implementation will prefer Valheim's native tame/follow machinery and preserve vanilla wolf behavior where practical. Rideable tameables such as lox and asksvin must not be forced into Follow while saddle/riding behavior owns movement.

Pied Piper deliberately excludes teleporting pets, formations, mass-radius commands, breeding automation, pet inventory, stat changes, custom pathfinding, and other tame-overhaul features from v0.1.

The repository mesh is established before gameplay code begins so API discovery, implementation, validation, and future creature compatibility have an explicit home.

→ [Pied Piper documentation](PiedPiper/README.md)  
→ [Implementation plan](PiedPiper/IMPLEMENTATION_PLAN.md)  
→ [Current status](PiedPiper/STATUS.md)  
→ [Implementation tracker #6](https://github.com/Knapp-Kevin/WulfPack-mods/issues/6)

### Vidar Shrugged

**Build Asgard. Keep your frames.**

Vidar Shrugged is the fourth resident mod and a deliberately larger project than the preceding WulfPack utilities. It targets the performance ceiling created by extraordinarily dense settlements, where tens of thousands of construction pieces can overlap with rendering, simulation, lights, particles, streaming, and multiplayer work in the same active area.

Its product boundary is specific: **large-settlement scalability, not generic graphics tweaking.**

The initial Gate 0 implementation is intentionally read-only and includes:

- rolling frame-time diagnostics with tail-percentile reporting
- optional active-scene pressure snapshots
- a cooperative frame-budget work queue for future bounded analysis and rebuild tasks
- a benchmark ladder culminating in a roughly 40,000-piece **Asgard** scenario
- explicit architecture gates that keep aggressive lifecycle changes out of the stable foundation
- local install / disable / enable / status / uninstall tooling
- no Harmony patches, save mutation, networking changes, or world-state mutation in Gate 0

Future gates investigate sector indexing, safe object classification, distance-aware throttling, GPU instancing, render clustering, deterministic caches, progressive streaming, and HLOD-style distant settlement representations. More invasive ideas such as WearNTear suppression, ZSyncTransform suppression, zone retention, and prefab prewarming remain experimental until evidence justifies their compatibility cost.

→ [Vidar Shrugged documentation](VidarShrugged/README.md)  
→ [Implementation plan](VidarShrugged/IMPLEMENTATION_PLAN.md)  
→ [Benchmark plan](VidarShrugged/BENCHMARK_PLAN.md)  
→ [Current status](VidarShrugged/STATUS.md)  
→ [Gate 0 tracker #10](https://github.com/Knapp-Kevin/WulfPack-mods/issues/10)

## Repository structure

Each mod lives in its own top-level folder and remains independently buildable, packageable, testable, and removable.

```text
WulfPack-mods/
├── banner.png
├── README.md
├── RestedWhispers/
│   ├── Plugin.cs
│   ├── RestedWhispers.csproj
│   ├── build-local.ps1
│   ├── manifest.json
│   ├── icon.png
│   └── README.md
├── RuneCompass/
│   ├── Plugin.cs
│   ├── CompassController.cs
│   ├── CompassUI.cs
│   ├── HeadingProvider.cs
│   ├── WindProvider.cs
│   ├── RuneCompass.csproj
│   ├── build-local.ps1
│   ├── manifest.json
│   ├── icon.png
│   ├── README.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── STATUS.md
│   └── Assets/
│       └── Skins/
├── PiedPiper/
│   ├── README.md
│   ├── IMPLEMENTATION_PLAN.md
│   ├── STATUS.md
│   └── manifest.json
└── VidarShrugged/
    ├── Plugin.cs
    ├── FrameMetrics.cs
    ├── ActiveSceneCounter.cs
    ├── CooperativeWorkQueue.cs
    ├── VidarShrugged.csproj
    ├── build-local.ps1
    ├── manifest.json
    ├── README.md
    ├── IMPLEMENTATION_PLAN.md
    ├── BENCHMARK_PLAN.md
    └── STATUS.md
```

Shared tooling should be introduced only when it clearly reduces duplication without coupling otherwise independent mods.

## Project family

WulfPack Mods and WulfPack Forge are related Valheim projects, but they occupy different technical boundaries.

| Project | Type | Role |
| --- | --- | --- |
| **WulfPack Mods** | In-game BepInEx mods | Extend or enhance Valheim while the game is running. |
| **[WulfPack Forge](https://github.com/Knapp-Kevin/WulfPackForge)** | External companion application | Character editing and management outside the running game. |

That boundary is deliberate. Features that belong outside the game should not be forced into a mod merely for branding consistency, and runtime gameplay features should not be shoehorned into Forge. They can still reference one another because they serve the same Valheim audience.

## Design principles

- Prefer small, well-bounded mods over sprawling feature bundles.
- Solve a real gameplay problem instead of cloning an existing popular mod without a reason.
- Avoid save mutation unless the feature genuinely requires it.
- Treat multiplayer synchronization, server authority, and world persistence as explicit complexity boundaries.
- Prefer native Valheim UI and behavior where practical.
- Keep every mod independently removable without damaging a vanilla character or world whenever possible.
- Keep presentation systems separate from gameplay logic. Rune Compass skins are the first explicit example.
- Prefer native game behavior over replacement systems. Pied Piper should adapt Valheim's tame/follow machinery rather than invent custom pathfinding unless proven necessary.
- Treat installed Valheim assemblies as authoritative. Historical mod source is reference material, not a contract.
- Document verified behavior separately from implementation assumptions.
- For performance work, optimize measured bottlenecks rather than proxy counts or intuition alone.

## Local build and validation policy

**GitHub Actions are prohibited in this repository. The GitHub Actions budget is zero.**

Do not add `.github/workflows`, hosted CI jobs, scheduled Actions, release Actions, or Actions-based validation.

Builds, tests, packaging, install-state checks, and in-game validation are performed locally unless an explicitly approved non-Actions mechanism is introduced later.

Each implemented mod owns its local workflow. In general:

```powershell
.\<ModName>\build-local.ps1
.\<ModName>\build-local.ps1 -Install
.\<ModName>\build-local.ps1 -Status
.\<ModName>\build-local.ps1 -Disable
.\<ModName>\build-local.ps1 -Enable
.\<ModName>\build-local.ps1 -Uninstall
```

The scripts are expected to manage only their own files and leave unrelated BepInEx plugins untouched.

## Adding another mod

A new mod should begin as a separate top-level directory with its own:

- source and project file once implementation begins
- README
- implementation/status documentation while the design is still moving
- Thunderstore manifest and icon when appropriate
- local build/install/remove workflow before playable validation
- explicit scope and non-goals
- validation evidence

Add the new mod to the table in [Current mods](#current-mods), then keep its detailed documentation inside the mod directory rather than allowing the root README to become an archaeological dig.

## Status

- **Rested Whispers:** ✅ implemented, tested, validated, and accepted.
- **Rune Compass:** 🧪 first playable implementation is in the repository; local compile and in-game validation remain tracked in issue #4.
- **Pied Piper:** 🧱 repository mesh is established; authoritative Valheim tame/follow API discovery is the next gate in issue #6.
- **Vidar Shrugged:** 🧪 Gate 0 foundation is merged on `main`; compile and in-game baseline validation remain tracked in issue #10.
- **GitHub Actions:** prohibited. Zero runs expected.
