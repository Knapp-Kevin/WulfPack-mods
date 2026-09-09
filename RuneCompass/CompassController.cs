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
    private readonly ShipWindGauge _shipWindGauge = new();

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
        UpdateShipWindGauge();
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
            && _stormProvider.IsStorm(
                _settings.StormEnvironments(), _settings.StormBiomes());
        _envelope.Tick(
            storm,
            Time.unscaledDeltaTime,
            CompassSettings.ClampRamp(_settings.InterferenceRampSeconds()),
            CompassSettings.ClampRelease(_settings.InterferenceReleaseSeconds()));
    }

    /// <summary>
    /// Hands each layer its bearing and its share of the storm displacement.
    /// </summary>
    /// <remarks>
    /// Each layer scales the shared storm level by its own configured amplitude, so how
    /// completely the storm takes each one over is tunable live. The wind rune is handed the
    /// same storm state as everything else and is unmoved by it, because its supplier is
    /// <see cref="DirectionLayer.Immune"/>. Wind is directly observable in the world - driven
    /// rain, bent grass, a sail - so the instrument failing while observation continues is
    /// the coherent reading.
    /// </remarks>
    private void Render(float heading)
    {
        StormState storm = new(
            _envelope.Level,
            Time.unscaledTime,
            CompassSettings.ClampTurbulence(_settings.StormTurbulence()),
            _settings.IndependentLayerInterference());

        _ui!.SetCameraHeading(heading, storm);

        if (_facingProvider.TryGetFacingDegrees(_settings.HeadingOffset(), out float facing))
        {
            _ui.SetFacing(facing, storm);
        }

        // The rune marks the quarter the wind comes FROM, so it sits at the reciprocal of
        // the toward-bearing the game reports. This is a presentation choice, not a
        // correction: a glyph parked on the rim reads as a quarter, the way a nor'easter is
        // named for where it blows from. An arrow would have to point the other way.
        _ui.SetWind(
            _windProvider.TryGetWindTowardDegrees(out float wind)
                ? Bearing.Normalize(_settings.WindShowsSource() ? wind + 180f : wind)
                : null,
            storm);
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

    /// <summary>
    /// Suppresses Valheim's own ship wind gauge, which duplicates the wind rune while
    /// aboard. Reasserted per frame because <c>Hud.UpdateShipHud</c> re-activates it.
    /// </summary>
    private void UpdateShipWindGauge()
    {
        if (_settings.HideShipWindIndicator())
        {
            _shipWindGauge.Hide();
        }
        else
        {
            _shipWindGauge.Restore();
        }
    }

    public void Dispose()
    {
        // Before the UI, so the gauge is handed back even if disposing our own objects
        // throws. It is the only vanilla state this mod holds.
        _shipWindGauge.Restore();
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

        _ui = new CompassUI(_skin, _settings);
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
        _shipWindGauge.Restore();
    }
}
