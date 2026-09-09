# Vidar Shrugged concept

## Product intent

Vidar Shrugged is a large-build performance mod for Valheim. Its north-star workload is a settlement in the 30,000–40,000-piece range where rendering, simulation, effects, streaming, and multiplayer activity overlap in one dense area.

> Build Asgard. Keep your frames.

The product goal is to make extraordinary settlements scale more gracefully without changing how players build or corrupting canonical world state.

## Player experience

The ideal player experience is mostly invisible: the same settlement, same pieces, same interactions, less frame-time pain.

Optimization work should preserve vanilla semantics and fail open to vanilla behavior whenever a piece, prefab, or runtime state cannot be classified safely.

Diagnostics may expose measurements during development or explicit troubleshooting, but Vidar Shrugged is not intended to become a permanent performance-dashboard HUD during normal play.

## Visual language

The mod's visual identity should emphasize large-scale Norse construction rather than generic "FPS booster" imagery.

- monumental settlements, fortresses, and Asgard-scale builds;
- architectural density and scale as the subject;
- restrained performance/measurement overlays only when they represent actual diagnostics;
- no fake benchmark numbers in concept art;
- no imagery that implies world deletion, aggressive culling, or invisible pieces as the core product promise.

Any future dashboard or benchmark visualization should prioritize measured frame-time evidence over decorative gauges.

## Concept art

No dedicated Vidar Shrugged concept art is currently committed.

Future concept assets should show the intended workload and preservation principle: a huge recognizable settlement still present and playable, not a before/after image that achieves performance by making the world disappear.

Concept assets belong under `Assets/Concept/`.

## Screenshots

No curated runtime screenshots are currently committed under `Assets/Screenshots/`.

Useful evidence captures should eventually include:

- the benchmark settlement at known piece/scene scale;
- diagnostics from a vanilla baseline;
- the same scene after a specific optimization gate;
- captions identifying build, settings, and what the screenshot actually proves.

Performance conclusions require measurements in addition to screenshots.

## Constraints and non-goals

Vidar Shrugged is not:

- a generic graphics-settings bundle;
- permission to remove or merge logical gameplay pieces into irreversible world state;
- justification for unsafe ZDO/save mutation;
- a promise that instance count alone explains performance;
- a reason to optimize unmeasured bottlenecks;
- a license to make modded prefabs behave like vanilla ones without proof.

The current implementation and gate state are documented in [README.md](README.md), [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md), [BENCHMARK_PLAN.md](BENCHMARK_PLAN.md), and [STATUS.md](STATUS.md).