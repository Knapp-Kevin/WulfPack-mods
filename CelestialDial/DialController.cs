using System;
using BepInEx.Logging;

namespace WulfPack.CelestialDial;

internal sealed class DialController : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly ValheimTimeSource _timeSource;
    private DialUI? _ui;
    private bool _loggedFirstRender;

    public DialController(ManualLogSource log)
    {
        _log = log;
        _timeSource = new ValheimTimeSource(log);
    }

    public void Tick()
    {
        if (Player.m_localPlayer == null || !_timeSource.TryRead(out DayCycleState state))
        {
            _ui?.SetVisible(false);
            return;
        }

        _ui ??= new DialUI();
        _ui.SetState(state);
        _ui.SetVisible(true);

        if (!_loggedFirstRender)
        {
            _loggedFirstRender = true;
            _log.LogInfo(
                $"Celestial Dial primitive HUD rendered: day {state.Day}, cycle {state.Fraction:F4}.");
        }
    }

    public void Dispose()
    {
        _ui?.Dispose();
        _ui = null;
    }
}
