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

## Gate 1 step 1: the instrument, rebuilt

Gate 0 ended with a number that could not be acted on and an instrument that corrupted its
own measurement. Both are fixed before any optimization is attempted.

### Scan cost

`Resources.FindObjectsOfTypeAll<Component>()` materialised an array of every Component held
in memory — assets and inactive objects included, not just the loaded scene — then ran seven
type tests per element in managed code. Each category is now requested directly via
`Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)`, which
searches loaded scenes only and filters by type and active state natively.

Counts mean what they meant before: `Exclude` reproduces the old `activeInHierarchy` filter,
and scene-only search reproduces the old `scene.IsValid()` filter.

| Scene | Gate 0 | Gate 1 | |
|---|---|---|---|
| Main menu (~226 renderers) | 85 - 155 ms | **1.71 - 4.07 ms** | same scene, ~40x faster |
| Loaded world | 178 - 225 ms @ 12,274 renderers | **53.8 - 82.0 ms @ 18,355 renderers** | ~5.4x cheaper per renderer |

Still not free. 54 ms is three frames at 60 fps, so snapshots remain off by default and the
measured cost is printed with every snapshot so it can be checked against the frame budget.
The one-time warning no longer quotes a fixed figure, because the figure now varies with
scene size and is reported directly.

### Renderer population by prefab

A bare count says where to look but not what to do. Renderers are now grouped by owning
prefab, `(Clone)` folded, heaviest first.

## Gate 1 finding: the renderer population is vegetation, not structures

Measured in a loaded world, 18,355 active renderers:

```text
Beech1          9152  (49.9%)
Beech_small1    2374  (12.9%)
Beech_small2    2342  (12.8%)
Bush01           758   (4.1%)
Rock_4           584   (3.2%)
Birch1           522   (2.8%)
RaspberryBush    470   (2.6%)
Pickable_Stone   428   (2.3%)
```

**Half of every active renderer is one prefab: `Beech1`. Roughly 85% of the top eight is
vegetation.** Not one player-built piece appears in the list.

That contradicts the recommendation Gate 0 closed with, which was to pursue renderer count
on the assumption it tracked build size. It does not, at least not here. Chasing mesh
batching or instancing for build pieces would have been optimizing a population that is not
the one on screen.

### The honest caveat

This measurement was taken where the session happened to be, which is forest. It shows what
dominates *this* location; it does not yet show what dominates a large settlement, which is
the problem the mod exists to solve. Vegetation may still dominate there, or build pieces may
overtake it — that is exactly the question, and it is now cheap to answer.

**The next measurement is the same snapshot taken standing in the large build.** If
vegetation still leads, the target is vegetation rendering, not structures. If build pieces
take over, the comparison between the two locations sizes the problem precisely.

Nothing should be optimized until that comparison exists. The instrument is now cheap enough
to take it without disturbing the thing being measured.

## Not implemented, deliberately

Harmony patches, mesh batching, GPU instancing, HLOD, WearNTear suppression,
ZSyncTransform suppression, ZDO/network interception, zone retention, prefab prewarming,
persistent rendering cache. Gate 1 so far is instrumentation only; no gameplay or world
state is touched.
