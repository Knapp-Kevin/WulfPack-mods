using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

internal sealed class WindProvider
{
    private readonly ManualLogSource _log;
    private MethodInfo? _getWindDir;
    private FieldInfo? _windDirField;
    private bool _resolved;
    private bool _warnedUnavailable;

    public WindProvider(ManualLogSource log)
    {
        _log = log;
    }

    public bool TryGetWindTowardDegrees(out float degrees)
    {
        EnvMan? env = EnvMan.instance;
        if (env == null)
        {
            degrees = 0f;
            return false;
        }

        Resolve(env.GetType());

        if (TryReadMethod(env, out Vector3 direction) || TryReadField(env, out direction))
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                degrees = 0f;
                return false;
            }

            direction.Normalize();
            degrees = Normalize(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            return true;
        }

        if (!_warnedUnavailable)
        {
            _warnedUnavailable = true;
            _log.LogWarning(
                "Rune Compass could not resolve Valheim's live wind direction API. "
                + "Heading will continue to work; inspect the installed EnvMan API before changing this provider.");
        }

        degrees = 0f;
        return false;
    }

    private void Resolve(Type envType)
    {
        if (_resolved)
        {
            return;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        _getWindDir = envType.GetMethod("GetWindDir", flags, null, Type.EmptyTypes, null);
        if (_getWindDir != null && _getWindDir.ReturnType != typeof(Vector3))
        {
            _getWindDir = null;
        }

        _windDirField = envType.GetField("m_windDir", flags);
        if (_windDirField != null && _windDirField.FieldType != typeof(Vector3))
        {
            _windDirField = null;
        }

        _resolved = true;
        _log.LogInfo(
            $"Rune Compass wind API: method={_getWindDir?.Name ?? "none"}, field={_windDirField?.Name ?? "none"}.");
    }

    private bool TryReadMethod(EnvMan env, out Vector3 direction)
    {
        if (_getWindDir == null)
        {
            direction = default;
            return false;
        }

        try
        {
            object? value = _getWindDir.Invoke(env, null);
            if (value is Vector3 vector)
            {
                direction = vector;
                return true;
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug($"GetWindDir invocation failed: {ex.GetType().Name}: {ex.Message}");
        }

        direction = default;
        return false;
    }

    private bool TryReadField(EnvMan env, out Vector3 direction)
    {
        if (_windDirField == null)
        {
            direction = default;
            return false;
        }

        try
        {
            object? value = _windDirField.GetValue(env);
            if (value is Vector3 vector)
            {
                direction = vector;
                return true;
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug($"m_windDir read failed: {ex.GetType().Name}: {ex.Message}");
        }

        direction = default;
        return false;
    }

    private static float Normalize(float degrees)
    {
        degrees %= 360f;
        return degrees < 0f ? degrees + 360f : degrees;
    }
}
