# Vidar Shrugged status

## Current state

🧱 **Gate 0: foundation implementation in progress**

The product name, objective, safety boundary, benchmark contract, and phased architecture are established.

## Implemented in this branch

- dedicated `VidarShrugged/` project area
- product README and explicit non-goals
- benchmark ladder culminating in the ~40,000-piece **Asgard** workload
- implementation gates from diagnostics through optional aggressive optimizations
- initial read-only runtime diagnostics
- cooperative frame-budget scheduler primitive
- local build/install/remove workflow
- Thunderstore-style manifest metadata

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

## Next validation gate

1. Build against the installed Valheim/BepInEx assemblies.
2. Load in-game and confirm diagnostics overhead is negligible with scene snapshots disabled.
3. Enable scene snapshots and measure their own cost.
4. Re-run against Valheim 1.0 assemblies when available.
5. Capture the first canonical vanilla large-build baseline before selecting the first invasive optimization.

## Current risk

Valheim 1.0 is imminent, so implementation-sensitive patches are intentionally deferred until the shipped 1.0 assemblies can be treated as authoritative.

## Completion definition for Gate 0

Gate 0 is complete when the plugin compiles, loads, reports stable metrics, can be disabled/removed cleanly, and has captured reproducible baseline evidence without altering gameplay or world state.
