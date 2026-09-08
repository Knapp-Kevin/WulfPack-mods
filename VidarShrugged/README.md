# Vidar Shrugged

### Large Build Optimizer for Valheim

**Build Asgard. Keep your frames.**

Vidar Shrugged is a WulfPack performance mod focused on one problem: making extraordinarily dense Valheim settlements practical without changing how players build or how the world behaves.

The north-star workload is a 30,000–40,000-piece settlement where rendering, simulation, effects, streaming, and multiplayer activity all overlap in one dense area.

## Product promise

> Make extremely large Valheim settlements scale gracefully while preserving vanilla gameplay semantics and canonical world state.

Vidar Shrugged is not intended to become a generic graphics-tweaking bundle. Every optimization must be measurable against large-build workloads and independently disableable when it crosses a compatibility boundary.

## Design rules

1. **Measure first.** No optimization is accepted because FPS merely feels better.
2. **Logical pieces remain logical pieces.** Rendering may be clustered or instanced, but gameplay objects remain independently addressable.
3. **Prefer representation optimization over world mutation.** Client-side rendering and scheduling come before authority or persistence changes.
4. **Event-driven invalidation beats permanent polling.** Static or dormant work should wake only when a relevant change occurs.
5. **Fail open to vanilla.** If an optimization cannot prove that it is safe for a prefab or runtime state, vanilla behavior wins.
6. **Bound frame work.** Expensive analysis, rebuilds, and future cache work must use cooperative per-frame budgets.
7. **Experimental features stay experimental.** ZSync suppression, WearNTear shortcuts, zone retention, and prefab prewarming do not belong in the stable first slice.
8. **No GitHub Actions.** This repository's zero-Actions policy applies here too.

## Performance domains

Vidar Shrugged treats large-settlement cost as five related but separable domains:

- **Rendering:** renderer count, draw submission, repeated meshes/materials, shadows, and future HLOD.
- **Simulation:** redundant component updates, structure/environment checks, and other polling work.
- **Effects:** lights, shadows, particles, smoke, and audio that can be prioritized by relevance.
- **Streaming:** the burst cost of entering or loading a dense settlement.
- **Multiplayer:** synchronization and scene materialization when multiple players converge on the same build.

Instance count is a useful signal, not the entire problem.

## Current implementation slice

The initial implementation is deliberately read-only:

- frame-time sampling and percentile reporting
- optional active-scene population snapshots
- a cooperative frame-budget work queue for future expensive tasks
- configuration for diagnostics cadence
- local build/install/disable/enable/status/uninstall workflow

It does **not** currently patch Valheim methods, batch meshes, alter networking, suppress components, mutate saves, or change world state.

That boundary lets us capture a clean vanilla baseline and then measure each optimization independently.

## Roadmap

### Phase 0 — Baseline and instrumentation

Establish repeatable benchmark scenes and capture CPU-facing frame behavior before optimization.

### Phase 1 — Low-risk runtime primitives

- spatial/sector instance registry
- distance-aware cosmetic throttling
- cooperative work scheduling
- explicit static/semi-static/dynamic classification

### Phase 2 — Rendering scalability

- GPU instancing for safe repeated geometry
- render clustering for safe static pieces
- dirty-region invalidation and bounded rebuilds

### Phase 3 — Persistence and streaming

- deterministic cache fingerprints
- zone-level cache invalidation
- progressive scene materialization

### Phase 4 — Hierarchical rendering

Investigate HLOD-style settlement representations so distant dense builds can render as a small number of visual clusters while their canonical gameplay objects remain intact.

### Phase 5 — Experimental aggressive optimizations

Only after evidence justifies the compatibility cost:

- WearNTear update suppression
- static ZSyncTransform suppression
- zone keepalive/retention
- prefab or shader prewarming
- optional server-side replication assistance

## Benchmark target

The benchmark ladder culminates in **Asgard**, a deliberately pathological settlement workload around 40,000 construction pieces with substantial lighting, effects, creatures, and multiplayer activity.

See [BENCHMARK_PLAN.md](BENCHMARK_PLAN.md) for the measurement contract and [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for development gates.

## Compatibility posture

The first product boundary is client-side and read-only wherever practical. Vidar Shrugged should remain independently removable without damaging a vanilla character or world.

Modded prefabs are never assumed safe merely because they resemble vanilla pieces. Future classifiers must explicitly fail open to unoptimized vanilla behavior when safety cannot be established.

## Status

🧱 **Foundation implementation in progress.** Instrumentation and architecture are being established before any invasive optimization is introduced.

See [STATUS.md](STATUS.md) for the current working state.