# Vidar Shrugged implementation plan

## Objective

Make extraordinarily dense Valheim settlements practical by reducing redundant rendering, simulation, effects, streaming, and multiplayer overhead while preserving vanilla gameplay semantics and canonical world state.

The project is benchmark-driven. A change is not considered an optimization until it produces repeatable improvement on a defined workload without introducing gameplay or compatibility regressions.

## Gate 0 — Baseline instrumentation

### Deliverables

- frame-time sampler with percentile reporting
- optional active-scene population snapshots
- cooperative frame-budget scheduler primitive
- local build/install/remove workflow
- benchmark scenarios and measurement schema
- no Harmony patches or gameplay mutation

### Exit criteria

- compiles against the installed Valheim/BepInEx environment
- loads and unloads without errors
- produces stable frame metrics at configurable intervals
- diagnostics can be disabled completely
- no save/world-state writes
- benchmark instructions are reproducible

## Gate 1 — Spatial and classification primitives

### Deliverables

- sector-indexed registry for relevant scene instances
- piece safety classifier with explicit `Static`, `SemiStatic`, `Dynamic`, and `Unknown` outcomes
- dirty/invalidation event model
- distance-aware throttling limited to cosmetic behavior initially

### Safety requirements

- `Unknown` always receives vanilla behavior
- modded prefabs are not classified safe by name alone
- no permanent suppression without an invalidation path
- every optimization is feature-flagged

### Exit criteria

- registry materially reduces repeated whole-scene lookup cost in dense benchmarks
- classifier has deterministic tests for allow/deny/unknown behavior
- disabling Gate 1 features restores vanilla behavior without restart-sensitive world changes

## Gate 2 — Rendering scalability

### Deliverables

- repeated-mesh GPU instancing where material and renderer state are safe
- static render clustering where pieces can remain logically independent
- dirty-region invalidation and bounded rebuild queue
- transparent/cutout/shader-sensitive exclusions

### Core invariant

**Logical object != rendered object.**

A construction piece may be represented through a shared render cluster while remaining an independent Valheim object for damage, repair, support, interaction, persistence, and networking.

### Exit criteria

- substantial draw/renderer reduction in 10k, 20k, and 40k-piece workloads
- destroyed/changed pieces invalidate only affected render groups
- no missing materials, incorrect transparency, interaction loss, or collider changes
- rebuild work remains within the configured frame budget

## Gate 3 — Persistence and streaming

### Deliverables

- deterministic zone cache format
- cache fingerprint includes at least Valheim version, Vidar Shrugged version, configuration, prefab identity, mesh identity, and material identity
- safe stale-cache rejection
- progressive scene-analysis/materialization path for dense zone entry

### Exit criteria

- warm restart improves settlement entry without stale geometry
- removing/updating a prefab mod cannot silently reuse incompatible cached data
- corrupt cache fails open and can be deleted without world damage

## Gate 4 — Hierarchical rendering

### Investigation target

Assess HLOD-style render representations for distant settlements.

Potential model:

- near: native/instanced high-fidelity representation
- mid: small static render clusters
- far: larger settlement clusters
- very far: aggressively reduced visual representation

Canonical gameplay objects remain untouched.

### Exit criteria

No implementation ships until visual popping, lighting, destruction invalidation, and modded-material behavior have acceptable evidence.

## Gate 5 — Experimental aggressive optimization

Candidate features:

- WearNTear polling reduction
- static ZSyncTransform suppression
- zone retention/keepalive
- prefab/shader prewarming
- optional server-side replication prioritization

Each feature requires its own evidence and compatibility gate. These features must remain opt-in until proven broadly safe.

## Benchmark acceptance model

Every performance PR should record:

1. benchmark scenario
2. Valheim version
3. Vidar Shrugged commit/version
4. relevant mod list
5. hardware summary
6. baseline metrics
7. candidate metrics
8. correctness validation
9. observed regressions

Primary metrics:

- average frame time
- p50 / p95 / p99 / p99.9 frame time
- peak frame stall
- active renderers
- active lights/effects where measurable
- scene-entry stabilization time
- allocation/GC observations where measurable

Later gates add draw calls, batches, ZDO/network metrics, and subsystem timings where authoritative instrumentation is available.

## Non-goals

- changing build limits or placement rules
- changing structural integrity rules
- reducing canonical world detail
- server administration features
- generic graphics presets
- save-file rewriting
- requiring a custom world format
- treating all modded prefabs as safe to optimize

## Initial implementation sequence

1. Land Gate 0 repository mesh and diagnostics.
2. Validate against the installed pre-1.0 environment.
3. Revalidate against Valheim 1.0 assemblies when available.
4. Capture vanilla benchmark baselines.
5. Profile the 10k–40k workload before selecting the first invasive optimization.
6. Implement the smallest optimization that attacks the largest verified bottleneck.
