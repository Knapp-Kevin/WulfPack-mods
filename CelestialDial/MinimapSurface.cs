using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.CelestialDial;

internal sealed class MinimapSurface
{
    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly ManualLogSource _log;
    private Minimap? _instance;
    private FieldInfo? _smallRootField;
    private GameObject? _smallRoot;
    private bool _ownsSuppression;
    private bool _restoreActive;
    private bool _warned;

    public MinimapSurface(ManualLogSource log)
    {
        _log = log;
    }

    public bool TrySuppress()
    {
        if (Game.m_noMap)
        {
            Restore();
            return true;
        }

        Minimap? minimap = Minimap.instance;
        if (minimap == null || !TryResolve(minimap))
        {
            WarnOnce("Celestial Dial could not safely resolve the small minimap surface.");
            return false;
        }

        if (!_ownsSuppression)
        {
            _restoreActive = _smallRoot!.activeSelf;
            _ownsSuppression = true;
        }

        if (_smallRoot!.activeSelf)
        {
            _smallRoot.SetActive(false);
        }
        return true;
    }

    public void Restore()
    {
        if (_ownsSuppression && _smallRoot != null)
        {
            _smallRoot.SetActive(_restoreActive);
        }
        _ownsSuppression = false;
    }

    private bool TryResolve(Minimap minimap)
    {
        if (_instance == minimap && _smallRoot != null)
        {
            return true;
        }

        if (_instance != null && _instance != minimap)
        {
            Restore();
        }

        _instance = minimap;
        _smallRoot = null;
        Type type = minimap.GetType();
        _smallRootField = type.GetField("m_smallRoot", AnyInstance)
            ?? type.GetField("_smallRoot", AnyInstance);
        if (_smallRootField == null)
        {
            return false;
        }

        try
        {
            object? value = _smallRootField.GetValue(minimap);
            if (value is GameObject gameObject)
            {
                _smallRoot = gameObject;
            }
            else if (value is Component component)
            {
                _smallRoot = component.gameObject;
            }
        }
        catch (Exception ex)
        {
            WarnOnce($"Celestial Dial minimap reflection failed: {ex.Message}");
            return false;
        }

        return _smallRoot != null;
    }

    private void WarnOnce(string message)
    {
        if (_warned)
        {
            return;
        }
        _warned = true;
        _log.LogWarning(message);
    }
}
