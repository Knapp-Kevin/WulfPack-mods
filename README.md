![WulfPack Mods banner](banner.png)

# WulfPack Mods

A maintained monorepo for focused Valheim mods developed under the WulfPack name.

WulfPack Mods is the in-game modding side of the broader WulfPack Valheim project family. It is intentionally separate from [WulfPack Forge](https://github.com/Knapp-Kevin/WulfPackForge), the external character-editor companion application.

## Current mods

| Mod | Purpose | Current state | Documentation |
| --- | --- | --- | --- |
| **Rested Whispers** | Native Valheim warnings as the Rested effect fades. | ✅ Implemented, tested, validated | [README](RestedWhispers/README.md) · [Concept](RestedWhispers/CONCEPT.md) |
| **Rune Compass** | Immersive No Map navigation with north-up heading, camera view, wind, and storm interference. | 🧪 Runtime accepted; optional Celestial Dial interop candidate added; nine skin art assets remain | [README](RuneCompass/README.md) · [Concept](RuneCompass/CONCEPT.md) · [Asset index](RuneCompass/Assets/Skins/ASSET_INDEX.md) · [Status](RuneCompass/STATUS.md) |
| **Celestial Dial** | Toggleable Sól and Máni day-cycle instrument showing world day and position in Valheim's day/night cycle. | 🧪 Primitive runtime and persistent-toggle candidates implemented; local 1.0 validation pending | [README](CelestialDial/README.md) · [Concept](CelestialDial/CONCEPT.md) · [Asset index](CelestialDial/Assets/Skins/ASSET_INDEX.md) · [Status](CelestialDial/STATUS.md) |
| **Pied Piper** | Native Follow / Stay interaction for eligible tamed creatures. | ✅ Functionally complete and operator-validated; release icon/packaging remains | [README](PiedPiper/README.md) · [Concept](PiedPiper/CONCEPT.md) · [Status](PiedPiper/STATUS.md) |
| **Vidar Shrugged** | Large-settlement performance instrumentation and optimization. | 🧪 Gate 0 foundation merged; later optimization gates remain | [README](VidarShrugged/README.md) · [Concept](VidarShrugged/CONCEPT.md) · [Implementation plan](VidarShrugged/IMPLEMENTATION_PLAN.md) · [Benchmark plan](VidarShrugged/BENCHMARK_PLAN.md) · [Status](VidarShrugged/STATUS.md) |

## Documentation model

Every mod has two different documentation surfaces because concept and evidence are not the same thing:

- `README.md` describes current behavior, use, configuration, compatibility, and readiness.
- `CONCEPT.md` describes product intent, player experience, visual language, concept art, runtime screenshots, and explicit non-goals.

Concept art is never runtime evidence. Generated mockups are never labeled as screenshots. Actual screenshots belong under `<Mod>/Assets/Screenshots/` and concept work belongs under `<Mod>/Assets/Concept/`.

The repository-wide contract is in [MOD_DOCUMENTATION_STANDARD.md](MOD_DOCUMENTATION_STANDARD.md).

Run the documentation gate locally:

```powershell
.\verify-mod-docs.ps1
```

## Mod risk tiers

What a mod is allowed to touch decides how hard it has to be validated.

| Mod | Tier | Touches | Gate required |
| --- | --- | --- | --- |
| **Rested Whispers** | read-only | reads status effects, shows native messages | build, load, lifecycle containment |
| **Rune Compass** | read-only | reads heading, facing, wind, weather/biome state; draws HUD | build, load, lifecycle containment |
| **Celestial Dial** | read-only | reads world day/day-night position; changes owned/vanilla HUD visibility while selected | build, load, lifecycle containment, UI restoration |
| **Vidar Shrugged** | read-only | Gate 0 reads frame timings and scene population | build, load, lifecycle containment |
| **Pied Piper** | state-touching | patches `Tameable`; unlocks native commands that can write patrol state | build, load, lifecycle containment, **plus a save-integrity diff across a play session with the patches live** |

### Tier definitions

- **read-only** — observes game state and may draw or temporarily change presentation visibility. No gameplay mutation, save writes, world writes, map-data writes, or persistent state owned by the mod.
- **state-touching** — patches or mutates live game objects during a session. Validation must account for what can happen while the mod runs.
- **persistent** — directly owns writes to save, world, or ZDO state that survive the session. No current mod is declared in this tier.

Run the repository gate locally:

```powershell
.\verify-repo.ps1
```

A state-touching validation pass can use:

```powershell
.\verify-save-integrity.ps1 -Baseline
.\verify-save-integrity.ps1 -Compare
```

## Rested Whispers

A small client-side mod that observes the existing Rested effect and gives timely native Valheim warnings before it expires. It does not change Rested mechanics, stats, saves, or world state.

Current state: complete and accepted.

→ [README](RestedWhispers/README.md)  
→ [Concept](RestedWhispers/CONCEPT.md)

## Rune Compass

Rune Compass is a north-up No Map navigation instrument built around one rule: **direction, not hidden information**.

Its accepted runtime hierarchy is:

- fixed north/cardinal card;
- bold character-facing arrow;
- quiet semi-transparent camera-view sector;
- small wind-source rune outside the rim;
- storm interference that can capture the facing/camera indicators while the card stays fixed and wind remains truthful.

All 13 current operator protocol rows for the existing compass behavior pass. The remaining visual backlog is nine assets: `camera_wedge`, `character_arrow`, and `wind_gust` for Classic Wood, Rune Ring, and Minimal Nordic. The current loader falls back to primitive geometry when those optional textures are absent.

Rune Compass now also exposes a tiny optional presentation-only bridge for Celestial Dial. The bridge can temporarily suppress the compass HUD without changing its configuration or gameplay behavior, then release it back to Rune Compass's normal visibility rules. This new interop path is implemented but not yet included in the historical operator acceptance.

→ [README](RuneCompass/README.md)  
→ [Concept](RuneCompass/CONCEPT.md)  
→ [Skin asset index](RuneCompass/Assets/Skins/ASSET_INDEX.md)  
→ [Skin-art tracker #19](https://github.com/Knapp-Kevin/WulfPack-mods/issues/19)

## Celestial Dial

**Read the sky. Know the day.**

Celestial Dial is a Sól and Máni themed instrument that answers exactly two questions: the current Valheim world day and the player's position in the full day/night cycle.

The concept and runtime evidence are deliberately separated. Corrected concept art lives only on the mod's concept page. The README does not embed it as if it were a screenshot.

The repository now contains a primitive read-only runtime candidate plus a persistent instrument-toggle candidate. The toggle is its own top-right HUD object, so it can remain present when a No Map server has no minimap surface. In normal-map play the candidate conservatively hides and restores the small minimap root; with Rune Compass loaded it uses optional reflection interop to suppress and restore only the compass presentation.

The candidate is deliberately not called playable yet. The repository-owned discovery probe and local build must be run against the installed Valheim 1.0 assemblies, the day/cycle mapping must be observed in-game, and the full toggle protocol must pass in normal-map and No Map play.

→ [README](CelestialDial/README.md)  
→ [Concept](CelestialDial/CONCEPT.md)  
→ [Skin asset index](CelestialDial/Assets/Skins/ASSET_INDEX.md)  
→ [Status](CelestialDial/STATUS.md)  
→ [Toggle acceptance protocol](CelestialDial/TOGGLE_TEST.md)  
→ [Runtime tracker #20](https://github.com/Knapp-Kevin/WulfPack-mods/issues/20)

## Pied Piper

Pied Piper makes eligible tameables use Valheim's native Follow / Stay path through the normal interact action.

The current gameplay scope is functionally complete and operator-validated on Valheim 1.0. Body interaction on saddled creatures still commands them, saddle interaction still mounts, and wild creatures remain unaffected.

Important removal rule: command every Pied Piper-commanded creature back to **Follow** before uninstalling. Valheim's native Stay command writes a patrol point that can outlive the mod; returning to Follow clears it.

Remaining work is release polish, notably a Thunderstore-ready `icon.png`, not API discovery.

→ [README](PiedPiper/README.md)  
→ [Concept](PiedPiper/CONCEPT.md)  
→ [Status](PiedPiper/STATUS.md)

## Vidar Shrugged

**Build Asgard. Keep your frames.**

Vidar Shrugged targets dense 30,000–40,000-piece settlements. Gate 0 is intentionally read-only and establishes measurement/instrumentation before invasive optimization work.

Future gates investigate safe classification, scheduling, rendering scalability, streaming, caches, and HLOD-style representations while preserving logical gameplay pieces and failing open to vanilla when safety is uncertain.

→ [README](VidarShrugged/README.md)  
→ [Concept](VidarShrugged/CONCEPT.md)  
→ [Implementation plan](VidarShrugged/IMPLEMENTATION_PLAN.md)  
→ [Benchmark plan](VidarShrugged/BENCHMARK_PLAN.md)  
→ [Status](VidarShrugged/STATUS.md)

## Repository structure

Each mod is independently buildable, packageable, testable, and removable.

```text
WulfPack-mods/
├── banner.png
├── README.md
├── MOD_DOCUMENTATION_STANDARD.md
├── verify-repo.ps1
├── verify-mod-docs.ps1
├── verify-save-integrity.ps1
├── RestedWhispers/
│   ├── README.md
│   └── CONCEPT.md
├── RuneCompass/
│   ├── Plugin.cs
│   ├── CompassController.cs
│   ├── RuneCompassInterop.cs
│   ├── README.md
│   ├── CONCEPT.md
│   ├── STATUS.md
│   └── Assets/Skins/
├── CelestialDial/
│   ├── Plugin.cs
│   ├── DialController.cs
│   ├── DialUI.cs
│   ├── InstrumentToggleUI.cs
│   ├── MinimapSurface.cs
│   ├── RuneCompassBridge.cs
│   ├── ValheimTimeSource.cs
│   ├── CelestialDial.csproj
│   ├── build-local.ps1
│   ├── discover-time-api.ps1
│   ├── TOGGLE_TEST.md
│   ├── README.md
│   ├── CONCEPT.md
│   ├── STATUS.md
│   └── Assets/
│       ├── Concept/
│       └── Skins/
├── PiedPiper/
│   ├── README.md
│   ├── CONCEPT.md
│   └── STATUS.md
└── VidarShrugged/
    ├── README.md
    ├── CONCEPT.md
    ├── IMPLEMENTATION_PLAN.md
    ├── BENCHMARK_PLAN.md
    └── STATUS.md
```

Asset directories are created when they contain real assets. Do not add fake screenshots or placeholder art just to make every tree visually identical.

## Design principles

- Prefer small, well-bounded mods over sprawling feature bundles.
- Solve a real gameplay problem instead of cloning another mod without a reason.
- Avoid save/world mutation unless the feature genuinely requires it.
- Treat multiplayer synchronization, server authority, and persistence as explicit complexity boundaries.
- Prefer native Valheim UI and behavior where practical.
- Keep presentation systems separate from gameplay logic.
- Treat installed Valheim assemblies as authoritative; historical source is reference material.
- Document verified behavior separately from assumptions and concept intent.
- For performance work, optimize measured bottlenecks rather than proxy counts or intuition.
- Do not invent replacement WulfPack branding. Repository-level branding uses the existing `banner.png`.

## Local build and validation policy

**GitHub Actions are prohibited in this repository. The GitHub Actions budget is zero.**

Builds, tests, packaging, install-state checks, documentation gates, and in-game validation are performed locally.

Common lifecycle pattern:

```powershell
.\<ModName>\build-local.ps1
.\<ModName>\build-local.ps1 -Install
.\<ModName>\build-local.ps1 -Status
.\<ModName>\build-local.ps1 -Disable
.\<ModName>\build-local.ps1 -Enable
.\<ModName>\build-local.ps1 -Uninstall
```

## Adding another mod

A new mod starts as its own top-level folder with a `manifest.json`, README, concept page, explicit scope/non-goals, risk tier, and local validation path.

Before treating its documentation as complete:

```powershell
.\verify-mod-docs.ps1
.\verify-repo.ps1
```

See [MOD_DOCUMENTATION_STANDARD.md](MOD_DOCUMENTATION_STANDARD.md) for the required concept/screenshot structure.

## Status

- **Rested Whispers:** ✅ implemented, tested, validated, accepted.
- **Rune Compass:** 🧪 established compass behavior accepted; optional Celestial Dial interop candidate awaits local acceptance; nine skin assets remain.
- **Celestial Dial:** 🧪 primitive runtime and persistent-toggle candidates implemented; installed Valheim 1.0 compile/runtime acceptance pending.
- **Pied Piper:** ✅ gameplay scope complete and operator-validated; release icon/packaging remains.
- **Vidar Shrugged:** 🧪 Gate 0 foundation merged; later optimization gates remain.
- **GitHub Actions:** prohibited. Zero runs expected.
