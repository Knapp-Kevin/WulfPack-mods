# Vidar Shrugged status

## Current state

🧪 **Gate 0 foundation merged; runtime validation remains open**

The product name, objective, safety boundary, benchmark contract, phased architecture, and read-only instrumentation foundation are now on `main` via PR #11.

## Landed on main

- dedicated `VidarShrugged/` project area
- product README and explicit non-goals
- benchmark ladder culminating in the ~40,000-piece **Asgard** workload
- implementation gates from diagnostics through optional aggressive optimizations
- initial read-only runtime diagnostics
- wall-clock frame-duration percentiles and frame-pacing context logging
- optional active-scene pressure snapshots with self-reported scan cost
- cooperative frame-budget scheduler primitive
- local build/install/remove workflow
- Thunderstore-style manifest metadata
- no compile-time dependency on `assembly_valheim.dll` in Gate 0

## Deliberately not implemented yet

- Harmony patches
- mesh combining
- GPU instancing
- HLOD
- ZDO/network interception
- `WearNTear` suppression
- `ZSyncTransform` suppression
- prefab prewarming
- zone retention
- persistent render cache
- save/world mutation

## Remaining Gate 0 validation

1. Build against the installed Valheim/BepInEx assemblies.
2. Load in-game and confirm diagnostics overhead is negligible with scene snapshots disabled.
3. Enable scene snapshots and measure their own cost.
4. Re-run against the shipped Valheim 1.0 assemblies when available.
5. Capture the first canonical vanilla large-build baseline before selecting the first invasive optimization.

## Current risk

The foundation has been statically reviewed and merged, but it has not yet been compiled or exercised in the actual Valheim runtime from this execution environment. Implementation-sensitive patches remain intentionally deferred until shipped 1.0 assemblies and baseline evidence are available.

## Completion definition for Gate 0

Gate 0 is complete when the plugin compiles, loads, reports stable metrics, can be disabled/removed cleanly, and has captured reproducible baseline evidence without altering gameplay or world state.
