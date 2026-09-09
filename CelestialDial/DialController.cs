using System;
using BepInEx.Logging;

namespace WulfPack.CelestialDial;

internal sealed class DialController : IDisposable
{
    private readonly ManualLogSource _log;
    private readonly ValheimTimeSource _timeSource;
    private readonly MinimapSurface _minimap;
    private readonly RuneCompassBridge _runeCompass;

    private DialUI? _ui;
    private InstrumentToggleUI? _toggle;
    private bool _dialSelected;
    private bool _loggedFirstRender;

    public DialController(ManualLogSource log)
    {
        _log = log;
        _timeSource = new ValheimTimeSource(log);
        _minimap = new MinimapSurface(log);
        _runeCompass = new RuneCompassBridge(log);
    }

    public void Tick()
    {
        if (Player.m_localPlayer == null)
        {
            HideForNoWorld();
            return;
        }

        EnsureToggle();
        _toggle!.SetVisible(true);

        if (!_dialSelected)
        {
            _ui?.SetVisible(false);
            _minimap.Restore();
            _runeCompass.TrySetSuppressed(false);
            return;
        }

        if (!_timeSource.TryRead(out DayCycleState state) || !TryAcquireInstrumentRegion())
        {
            SelectDial(false);
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
        _runeCompass.TrySetSuppressed(false);
        _minimap.Restore();
        _toggle?.Dispose();
        _toggle = null;
        _ui?.Dispose();
        _ui = null;
    }

    private void EnsureToggle()
    {
        if (_toggle != null)
        {
            return;
        }

        _toggle = new InstrumentToggleUI();
        _toggle.Clicked += ToggleSelection;
        _toggle.SetSelected(_dialSelected);
    }

    private void ToggleSelection()
    {
        SelectDial(!_dialSelected);
    }

    private void SelectDial(bool selected)
    {
        _dialSelected = selected;
        _toggle?.SetSelected(selected);
        if (selected)
        {
            return;
        }

        _ui?.SetVisible(false);
        _minimap.Restore();
        _runeCompass.TrySetSuppressed(false);
    }

    private bool TryAcquireInstrumentRegion()
    {
        if (!_minimap.TrySuppress())
        {
            return false;
        }

        if (_runeCompass.TrySetSuppressed(true))
        {
            return true;
        }

        _minimap.Restore();
        return false;
    }

    private void HideForNoWorld()
    {
        _ui?.SetVisible(false);
        _toggle?.SetVisible(false);
        _minimap.Restore();
        _runeCompass.TrySetSuppressed(false);
    }
}
