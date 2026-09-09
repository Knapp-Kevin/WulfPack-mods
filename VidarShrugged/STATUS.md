# Vidar Shrugged status

## Gate 0: validated

Compiled, installed, and observed running against the shipped Valheim 1.0 build. Two
defects were found and fixed. No optimization work has begun and no gameplay or world
state is touched.

### Environment

| Fact | Value | Source |
|---|---|---|
| Valheim | **1.0** | `Application.version`, logged at load |
| Unity | 6000.0.61f1 | `Application.unityVersion` |
| BepInEx | 5.4.22.0 (BepInExPack Valheim 5.4.2202) | `LogOutput.log` |
| GPU | NVIDIA GeForce GTX 1080 | `SystemInfo.graphicsDeviceName` |
| Display | 3440x1440, fullscreen | measured from a live frame |

### Gate 0 results

| Check | Result |
|---|---|
| `build-local.ps1` | clean first run, 0 warnings / 0 errors |
| `-Install`, `-Status` | clean, no PowerShell errors |
| BepInEx load | clean, zero Vidar exceptions |
| Startup diagnostics | version, Unity, game version, GPU, VSync, target frame rate, resolution, fullscreen |
| Frame summaries | emitted at the configured cadence with avg / p50 / p95 / p99 / p99.9 / max |
| Disable via config | verified — reports stop |
| Re-enable | verified — 3 reports in 35 s at a 10 s cadence, window reset cleanly |
| Scene snapshots | **OFF by default**, confirmed |
| Lifecycle | disable / enable / uninstall verified with SHA-256 diff: 102 save files, all sibling plugin files and all sibling config files byte-identical |

## Defects found and fixed

### 1. Frame pacing context was captured before the engine applied display settings

`Awake` read `Screen` and `QualitySettings` at plugin-load time and logged
`resolution=304x201, fullscreen=False` against a real 3440x1440 fullscreen window. Every
recorded baseline would have carried a resolution the game never ran at.

Pacing is now captured from a live frame and **re-checked at every report**. That second
part matters more than the first: the game lifts its own frame cap after startup, which
this caught in the act —

```text
Frame pacing context: vSyncCount=0, targetFrameRate=60,  resolution=3440x1440, fullscreen=True (PACED ...)
Frame pacing CHANGED : vSyncCount=0, targetFrameRate=-1, resolution=3440x1440, fullscreen=True (unpaced)
```

A frame-duration comparison spanning that transition is not a comparison. It is now
flagged next to the numbers it invalidates.

### 2. Scene snapshots dominate the metrics they sit beside

The scan is a main-thread stall. Measured in a loaded world:

| | value |
|---|---|
| scan cost | **178 - 225 ms** per capture |
| renderers | 12,274 |
| colliders | 8,888 |
| MonoBehaviours | 25,750 |
| lights / particles / audio / rigidbodies | 57 / 580 / 48 / 517 |

At 60 fps that is eleven to thirteen whole frames. With snapshots enabled, `p99.9` and
`max` were **193 - 233 ms in every interval** and equal to each other — the worst frame in
each window *is* the snapshot. With snapshots off, `p99.9` fell to 37 - 46 ms.

The cause is `Resources.FindObjectsOfTypeAll<Component>()`, which walks every Component in
memory including assets and inactive objects, not just the active scene, then runs seven
type checks per element.

The scan is unchanged — its semantics are documented and it stays off by default — but it
now logs a one-time warning when enabled, so nobody records a baseline through it by
accident. Making it cheap is a Gate 1 candidate, not a Gate 0 edit.

## Diagnostics overhead

Frame duration on this machine is capped (`targetFrameRate=60` at startup), and even
unpaced the frame-to-frame variance is several milliseconds. Vidar's per-frame cost is
roughly three orders of magnitude below that, so an in-game A/B measures noise, not
overhead. `FrameMetrics` is pure managed code with no Unity dependency, so it was measured
directly against the compiled DLL instead.

**These are wall-clock durations, not CPU timings.**

| | cost |
|---|---|
| `Add()` per frame sample | **7.49 ns** (median of 5 x 20,000,000 calls) |
| `SnapshotAndReset()` at 60 fps x 10 s (600 samples) | **0.082 ms** median, 0.127 ms p95 |
| at 144 fps x 10 s (1,440 samples) | 0.193 ms median |
| at the 3,600-sample cap | 0.475 ms median |
| combined steady state | **0.00087% of a 16.67 ms frame** |

The periodic report allocates one `float[n]` (~2.4 KB at 600 samples) and sorts it in
place. The report spike is 0.5% of a single frame, once per 600 frames.

Scene snapshots are excluded from these figures and are the dominant cost by four orders
of magnitude when enabled.

## Known state

- `CooperativeWorkQueue` is present but **referenced by nothing**. It is foundation for a
  later gate. Flagged rather than deleted so the decision to keep or drop it is deliberate.
- `SampleCapacity` is read once at `Awake`; changing it live changes the clamp used for
  reports but not the allocated buffer.

## Not implemented, deliberately

Harmony patches, mesh batching, GPU instancing, HLOD, WearNTear suppression,
ZSyncTransform suppression, ZDO/network interception, zone retention, prefab prewarming,
persistent rendering cache. Gate 0 is read-only instrumentation.

## Recommended first bottleneck for Gate 1

Chosen from the measurement, not from roadmap order.

**Renderer count and draw-call submission.** The loaded world carries **12,274 active
renderers** against 57 lights and 580 particle systems. Renderers outnumber every other
tracked category by more than an order of magnitude except MonoBehaviours, and they are the
category that scales with build size — which is the stated problem. The 25,750
MonoBehaviours are worth a second look, but many are engine and UI components rather than
per-piece build cost, so the count needs breaking down by type before it can be acted on.

The honest next step is not an optimization at all: extend the snapshot to group renderers
by prefab or material so the count becomes actionable, and make it cheap enough to run
without destroying the measurement. Optimizing before that is guessing.
