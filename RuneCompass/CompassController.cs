using System;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

internal sealed class CompassController : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly CompassSettings _settings;
    private readonly HeadingProvider _headingProvider = new();
    private readonly FacingProvider _facingProvider = new();
    private readonly WindProvider _windProvider = new();
    private readonly StormProvider _stormProvider;
    private readonly InterferenceEnvelope _envelope = new();

    private CompassUI? _ui;
    private CompassSkin? _skin;
    private bool _skinResolved;

    public CompassController(ManualLogSource log, CompassSettings settings)
    {
        _log = log;
        _settings = settings;
        _stormProvider = new StormProvider(log);
    }

    public void Tick()
    {
        if (!ShouldShow()
            || !_headingProvider.TryGetHeadingDegrees(_settings.HeadingOffset(), out float heading))
        {
            Hide();
            return;
        }

        EnsureUi();
        _ui!.SetVisible(true);
        _ui.ApplyLayout(
            _settings.Scale(), _settings.Opacity(), _settings.Offset(), _settings.Anchor());
        _ui.SetReadoutsVisible(_settings.ShowReadouts());

        AdvanceInterference();
        Render(heading);
    }

    /// <summary>
    /// Grows or fades the interference envelope toward the current storm state.
    /// </summary>
    /// <remarks>
    /// The envelope is advanced even while interference is disabled, so re-enabling mid
    /// storm does not snap the compass. <c>unscaledDeltaTime</c> keeps the ramp honest
    /// while the game is paused or time-scaled.
    /// </remarks>
    private void AdvanceInterference()
    {
        bool storm = _settings.InterferenceEnabled()
            && _stormProvider.IsStorm(_settings.StormEnvironments());
        _envelope.Tick(
            storm,
            Time.unscaledDeltaTime,
            CompassSettings.ClampRamp(_settings.InterferenceRampSeconds()));
    }

    /// <summary>
    /// Hands each layer its bearing and its share of the storm displacement.
    /// </summary>
    /// <remarks>
    /// The wind rune is handed a deflection like everything else and ignores it, because
    /// its amplitude scale is zero. Wind is directly observable in the world - driven rain,
    /// bent grass, a sail - so the instrument failing while observation continues is the
    /// coherent reading.
    /// </remarks>
    private void Render(float heading)
    {
        float max = CompassSettings.ClampDeflection(_settings.MaxDeflectionDegrees());
        float now = Time.unscaledTime;
        bool independent = _settings.IndependentLayerInterference();

        _ui!.SetNorth(Deflect(max, now, independent, CompassUI.CardPhaseSeed));
        _ui.SetCameraHeading(heading, Deflect(max, now, independent, CompassUI.WedgePhaseSeed));

        if (_facingProvider.TryGetFacingDegrees(_settings.HeadingOffset(), out float facing))
        {
            _ui.SetFacing(facing, Deflect(max, now, independent, CompassUI.ArrowPhaseSeed));
        }

        _ui.SetWind(
            _windProvider.TryGetWindTowardDegrees(out float wind)
                ? Bearing.Normalize(_settings.WindPointsToward() ? wind : wind + 180f)
                : null,
            0f);
    }

    /// <summary>
    /// The displacement for one layer. In shared mode every layer is given phase zero, so
    /// the three disturbed layers swing together; in independent mode each carries its own.
    /// </summary>
    private float Deflect(float max, float now, bool independent, float phaseSeed)
    {
        return Interference.Deflection(
            _envelope.Level, max, now, independent ? phaseSeed : 0f);
    }

    /// <summary>
    /// Whether the compass should be on screen at all, independent of whether a heading
    /// can currently be resolved.
    /// </summary>
    private bool ShouldShow()
    {
        return _settings.Enabled()
            && (!_settings.OnlyInNoMap() || Game.m_noMap)
            && Player.m_localPlayer != null;
    }

    public void Dispose()
    {
        _ui?.Dispose();
        _ui = null;
    }

    private void EnsureUi()
    {
        if (_ui != null)
        {
            return;
        }

        if (!_skinResolved)
        {
            _skinResolved = true;
            _skin = SkinLoader.Load(_settings.SkinsRoot(), _settings.SelectedSkin(), _log);
            if (_skin == null)
            {
                _log.LogInfo("Rune Compass falling back to the primitive HUD.");
            }
        }

        _ui = new CompassUI(_skin);
        _log.LogInfo(
            _skin == null
                ? "Rune Compass north-up HUD created (primitive)."
                : $"Rune Compass north-up HUD created (skin: {_skin.Name}).");
    }

    /// <summary>
    /// Hides the HUD and drops interference, so a compass re-shown later starts settled
    /// rather than mid-storm.
    /// </summary>
    private void Hide()
    {
        _ui?.SetVisible(false);
        _envelope.Reset();
    }
}
