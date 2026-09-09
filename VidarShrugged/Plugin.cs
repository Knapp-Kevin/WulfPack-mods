using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WulfPack.VidarShrugged;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.vidarshrugged";
    public const string PluginName = "Vidar Shrugged";
    public const string PluginVersion = "0.1.0";

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<float> _reportIntervalSeconds = null!;
    private ConfigEntry<int> _sampleCapacity = null!;
    private ConfigEntry<bool> _sceneSnapshots = null!;
    private ConfigEntry<float> _sceneSnapshotIntervalSeconds = null!;

    private FrameMetrics _frameMetrics = null!;
    private ConfigWatcher? _configWatcher;
    private float _nextReportAt;
    private float _nextSceneSnapshotAt;
    private bool _wasEnabled;
    private FramePacingContext _pacing;
    private bool _pacingLogged;
    private bool _snapshotCostWarned;

    private void Awake()
    {
        _enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Enable Vidar Shrugged diagnostics. Gate 0 is read-only and does not modify gameplay or world state.");

        _reportIntervalSeconds = Config.Bind(
            "Diagnostics",
            "ReportIntervalSeconds",
            10f,
            "Seconds between frame-metric summaries. Minimum effective value is 1 second.");

        _sampleCapacity = Config.Bind(
            "Diagnostics",
            "SampleCapacity",
            3600,
            "Maximum rolling frame samples used for each report. Values are clamped between 120 and 36000.");

        _sceneSnapshots = Config.Bind(
            "Diagnostics",
            "SceneSnapshots",
            false,
            "Periodically count active Unity scene components. This is intentionally off by default because enumeration itself has a measurable cost.");

        _sceneSnapshotIntervalSeconds = Config.Bind(
            "Diagnostics",
            "SceneSnapshotIntervalSeconds",
            30f,
            "Seconds between optional active-scene population snapshots. Minimum effective value is 5 seconds.");

        _frameMetrics = new FrameMetrics(Mathf.Clamp(_sampleCapacity.Value, 120, 36000));
        _configWatcher = new ConfigWatcher(Config, Logger);
        _wasEnabled = _enabled.Value;
        ResetTimers();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded in read-only Gate 0 mode.");
        Logger.LogInfo($"Unity {Application.unityVersion}; game version {Application.version}; GPU: {SystemInfo.graphicsDeviceName}.");

        // Frame pacing is deliberately NOT read here. At Awake the engine has not applied
        // display settings yet, so Screen reports a placeholder surface; it is captured on
        // the first Update and re-checked each report instead.
    }

    private void Update()
    {
        _configWatcher?.Tick();

        if (!_enabled.Value)
        {
            if (_wasEnabled)
            {
                _frameMetrics.Reset();
                _wasEnabled = false;
            }
            return;
        }

        if (!_wasEnabled)
        {
            _frameMetrics.Reset();
            ResetTimers();
            _wasEnabled = true;
        }

        float deltaSeconds = Time.unscaledDeltaTime;
        if (deltaSeconds > 0f)
        {
            _frameMetrics.Add(deltaSeconds * 1000f);
        }

        float now = Time.unscaledTime;
        if (now >= _nextReportAt)
        {
            ReportPacingIfChanged();
            ReportFrameMetrics();
            _nextReportAt = now + Mathf.Max(1f, _reportIntervalSeconds.Value);
        }

        if (_sceneSnapshots.Value && now >= _nextSceneSnapshotAt)
        {
            WarnAboutSnapshotCostOnce();
            ReportSceneSnapshot();
            _nextSceneSnapshotAt = now + Mathf.Max(5f, _sceneSnapshotIntervalSeconds.Value);
        }
    }

    /// <summary>
    /// Logs the pacing context the first time it is seen and whenever it changes.
    /// </summary>
    /// <remarks>
    /// A frame-duration comparison is only meaningful if VSync, frame cap and resolution
    /// held constant across it, so a change is recorded next to the numbers it invalidates
    /// rather than left for the reader to discover.
    /// </remarks>
    private void ReportPacingIfChanged()
    {
        FramePacingContext current = FramePacingContext.Capture();
        if (_pacingLogged && current.Matches(_pacing))
        {
            return;
        }

        Logger.LogInfo(
            (_pacingLogged ? "Frame pacing CHANGED: " : "Frame pacing context: ") + current);
        _pacing = current;
        _pacingLogged = true;
    }

    private void ReportFrameMetrics()
    {
        FrameMetricsSnapshot snapshot = _frameMetrics.SnapshotAndReset();
        if (snapshot.SampleCount == 0)
        {
            return;
        }

        Logger.LogInfo(
            $"Frame duration ({snapshot.SampleCount} samples): " +
            $"avg {snapshot.AverageMs:F2} ms (~{snapshot.AverageFps:F1} FPS equivalent), " +
            $"p50 {snapshot.P50Ms:F2} ms, p95 {snapshot.P95Ms:F2} ms, " +
            $"p99 {snapshot.P99Ms:F2} ms, p99.9 {snapshot.P999Ms:F2} ms, max {snapshot.MaxMs:F2} ms.");
    }

    /// <summary>
    /// Warns, once, that scene snapshots invalidate the frame metrics printed beside them.
    /// </summary>
    /// <remarks>
    /// The scan runs on the main thread and measured 81-156 ms per capture on the
    /// reference machine — five to nine whole frames at 60 fps. That lands squarely in the
    /// p99.9 and max columns of the report it sits next to, so a benchmark taken with
    /// snapshots enabled is measuring the instrument, not the game.
    /// </remarks>
    private void WarnAboutSnapshotCostOnce()
    {
        if (_snapshotCostWarned)
        {
            return;
        }

        _snapshotCostWarned = true;
        Logger.LogWarning(
            "SceneSnapshots is ON. The scan is a main-thread stall of roughly 80-160 ms and " +
            "will appear in the p99.9 and max frame-duration columns below it. Turn it off " +
            "before recording any frame-duration baseline.");
    }

    private void ReportSceneSnapshot()
    {
        ActiveSceneSnapshot snapshot = ActiveSceneCounter.Capture();
        Logger.LogInfo(
            "Active scene snapshot: " +
            $"renderers {snapshot.Renderers}, lights {snapshot.Lights}, particles {snapshot.ParticleSystems}, " +
            $"audio {snapshot.AudioSources}, colliders {snapshot.Colliders}, rigidbodies {snapshot.Rigidbodies}, " +
            $"MonoBehaviours {snapshot.MonoBehaviours}; scan cost {snapshot.ElapsedMilliseconds:F2} ms.");
    }

    private void ResetTimers()
    {
        float now = Time.unscaledTime;
        _nextReportAt = now + Mathf.Max(1f, _reportIntervalSeconds?.Value ?? 10f);
        _nextSceneSnapshotAt = now + Mathf.Max(5f, _sceneSnapshotIntervalSeconds?.Value ?? 30f);
    }
}
