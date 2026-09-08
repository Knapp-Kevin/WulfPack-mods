using System;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

internal sealed class CompassController : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly Func<bool> _enabled;
    private readonly Func<bool> _onlyInNoMap;
    private readonly Func<float> _scale;
    private readonly Func<float> _opacity;
    private readonly Func<Vector2> _offset;
    private readonly Func<HudAnchor> _anchor;
    private readonly Func<float> _headingOffset;
    private readonly HeadingProvider _headingProvider = new();
    private readonly WindProvider _windProvider = new();

    private CompassUI? _ui;

    public CompassController(
        ManualLogSource log,
        Func<bool> enabled,
        Func<bool> onlyInNoMap,
        Func<float> scale,
        Func<float> opacity,
        Func<Vector2> offset,
        Func<HudAnchor> anchor,
        Func<float> headingOffset)
    {
        _log = log;
        _enabled = enabled;
        _onlyInNoMap = onlyInNoMap;
        _scale = scale;
        _opacity = opacity;
        _offset = offset;
        _anchor = anchor;
        _headingOffset = headingOffset;
    }

    public void Tick()
    {
        if (!ShouldShow())
        {
            Hide();
            return;
        }

        if (!_headingProvider.TryGetHeadingDegrees(_headingOffset(), out float heading))
        {
            Hide();
            return;
        }

        EnsureUi();
        _ui!.SetVisible(true);
        _ui.ApplyLayout(_scale(), _opacity(), _offset(), _anchor());
        _ui.SetHeading(heading);
        _ui.SetWind(_windProvider.TryGetWindTowardDegrees(out float wind) ? wind : null);
    }

    /// <summary>
    /// Whether the compass should be on screen at all, independent of whether a heading
    /// can currently be resolved.
    /// </summary>
    private bool ShouldShow()
    {
        return _enabled()
            && (!_onlyInNoMap() || Game.m_noMap)
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
