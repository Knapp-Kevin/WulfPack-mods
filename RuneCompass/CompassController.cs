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
    private CompassSkin? _skin;
    private bool _skinResolved;
    private string _loadedSkinName = string.Empty;

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
        _ui.SetWind(
            _windProvider.TryGetWindTowardDegrees(out float wind)
                ? Bearing.Normalize(_settings.WindPointsToward() ? wind : wind + 180f)
                : null);
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
        // A skin change has to rebuild the HUD: sprites are bound to Image components at
        // construction, so swapping the config value alone would change nothing visible.
        // With the config watcher applying edits live, this makes comparing skins a
        // one-second edit instead of a relaunch each.
        string requested = _settings.SelectedSkin();
        if (_ui != null && !string.Equals(requested, _loadedSkinName, StringComparison.Ordinal))
        {
            _log.LogInfo($"Rune Compass skin changed to '{requested}'; rebuilding HUD.");
            _ui.Dispose();
            _ui = null;
            _skinResolved = false;
        }

        if (_ui != null)
        {
            return;
        }

        if (!_skinResolved)
        {
            _skinResolved = true;
            _loadedSkinName = requested;
            _skin = SkinLoader.Load(_settings.SkinsRoot(), requested, _log);
            if (_skin == null)
            {
                _log.LogInfo("Rune Compass falling back to the primitive HUD.");
            }
        }

        _ui = new CompassUI(_skin);
        _log.LogInfo(
            _skin == null
                ? "Rune Compass heading-up HUD created (primitive)."
                : $"Rune Compass HUD created (skin: {_skin.Name}).");
    }

    private void Hide()
    {
        _ui?.SetVisible(false);
    }
}
