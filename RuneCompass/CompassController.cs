using System;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

internal sealed class CompassController : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly CompassSettings _settings;
    private readonly HeadingProvider _headingProvider = new();
    private readonly WindProvider _windProvider = new();

    private CompassUI? _ui;

    public CompassController(ManualLogSource log, CompassSettings settings)
    {
        _log = log;
        _settings = settings;
    }

    public void Tick()
    {
        if (!ShouldShow())
        {
            Hide();
            return;
        }

        if (!_headingProvider.TryGetHeadingDegrees(_settings.HeadingOffset(), out float heading))
        {
            Hide();
            return;
        }

        EnsureUi();
        _ui!.SetVisible(true);
        _ui.ApplyLayout(
            _settings.Scale(), _settings.Opacity(), _settings.Offset(), _settings.Anchor());
        _ui.SetReadoutsVisible(_settings.ShowReadouts());
        _ui.SetHeading(heading);
        _ui.SetWind(_windProvider.TryGetWindTowardDegrees(out float wind) ? wind : null);
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

        _ui = new CompassUI();
        _log.LogInfo("Rune Compass heading-up HUD created.");
    }

    private void Hide()
    {
        _ui?.SetVisible(false);
    }
}
