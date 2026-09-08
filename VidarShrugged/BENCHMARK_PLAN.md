# Vidar Shrugged benchmark plan

## Purpose

Vidar Shrugged is only useful if it improves dense settlements measurably. This document defines the benchmark ladder used to compare vanilla behavior, Vidar Shrugged releases, and other optimization approaches.

## Benchmark ladder

| Scenario | Construction pieces | Lights | Fires/effect sources | Creatures | Players |
| --- | ---: | ---: | ---: | ---: | ---: |
| Homestead | ~1,000 | 10 | 4 | 5 | 1 |
| Village | ~5,000 | 30 | 10 | 10 | 3 |
| Castle | ~10,000 | 50 | 20 | 20 | 5 |
| City | ~20,000 | 100 | 40 | 30 | 5 |
| **Asgard** | **~40,000** | **200** | **75** | **50** | **10** |

These are target profiles, not claims that every benchmark world already exists. The exact composition should be versioned once canonical test worlds are captured.

## Why density matters

The benchmark must keep a large fraction of each settlement within the active/rendered region. Forty thousand pieces spread over a continent do not represent the same workload as forty thousand pieces concentrated into a city-scale view.

## Required passes

For each scenario, capture at least:

1. **Cold entry** — start outside the settlement and enter it normally.
2. **Warm stationary** — stand at the canonical observation point after the scene stabilizes.
3. **Traversal** — move through the settlement on a fixed route.
4. **Change event** — place and destroy representative building pieces.
5. **Effects stress** — observe with normal configured lights/fires active.
6. **Multiplayer convergence** — where practical, have target players enter the dense area.

## Gate 0 metrics

The initial instrumentation records CPU-facing frame metrics:

- sample count
- average frame time
- p50 frame time
- p95 frame time
- p99 frame time
- p99.9 frame time
- maximum sampled frame time
- average FPS derived from average frame time

Optional active-scene snapshots record counts for:

- renderers
- lights
- particle systems
- audio sources
- colliders
- rigidbodies
- MonoBehaviours

These counts are diagnostic context, not optimization success metrics by themselves.

## Future metrics

When authoritative instrumentation is implemented, add:

- draw calls / batches
- triangles / vertices
- shadow-caster counts
- active Valheim `Piece` count
- active `WearNTear` count and update rate
- active `ZNetView` / ZDO-related population
- zone-entry stabilization time
- garbage collections and managed allocations
- network receive/send rates during convergence
- time spent in specific patched or observed subsystems

## Comparison matrix

At minimum, performance investigations should support:

| Configuration | Purpose |
| --- | --- |
| Vanilla / BepInEx only | canonical baseline |
| Vidar Shrugged diagnostics only | instrumentation overhead check |
| Vidar Shrugged candidate | optimization measurement |
| Relevant third-party optimizer | prior-art comparison where licensing and compatibility permit |
| Third-party + Vidar | collision / duplicated-bottleneck investigation only |

Do not assume two optimizers stack beneficially. If both intercept the same lifecycle or rendering path, the combination may be meaningless or unsafe.

## Run metadata

Every benchmark record should capture:

- date/time
- Valheim version
- BepInEx version
- Vidar Shrugged version/commit
- scenario name/version
- enabled mod list
- CPU
- GPU
- RAM
- resolution
- graphics preset and relevant overrides
- player count
- notes on weather/time-of-day if visually consequential

## Measurement discipline

- Use the same observation point and traversal route between comparable runs.
- Do not compare a cold first shader/material load with a warmed subsequent run without labeling the distinction.
- Prefer multiple runs and report spread, not one heroic screenshot.
- Treat average FPS as insufficient. Large-build pain often appears in tail frame times and entry stalls.
- Record correctness regressions alongside performance. Missing walls are technically excellent for FPS and therefore not an acceptable optimization.

## Aspirational outcome

A 30,000–40,000-piece settlement that is effectively unusable in vanilla should become comfortably playable on the same hardware, without sacrificing canonical construction behavior or requiring a custom world format.

The achievable numerical target will be set only after Valheim 1.0 baseline evidence exists.