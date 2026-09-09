using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.CelestialDial;

internal readonly struct DayCycleState
{
    public DayCycleState(int day, float fraction)
    {
        Day = day;
        Fraction = fraction;
    }

    public int Day { get; }
    public float Fraction { get; }
}

internal sealed class ValheimTimeSource
{
    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly ManualLogSource _log;
    private MethodInfo? _dayMethod;
    private FieldInfo? _fractionField;
    private bool _resolved;
    private bool _warned;

    public ValheimTimeSource(ManualLogSource log)
    {
        _log = log;
    }

    public bool TryRead(out DayCycleState state)
    {
        state = default;
        EnvMan? env = EnvMan.instance;
        if (env == null)
        {
            return false;
        }

        Resolve(env.GetType());
        if (_dayMethod == null || _fractionField == null)
        {
            WarnOnce("Celestial Dial time seam is unavailable; HUD remains hidden.");
            return false;
        }

        try
        {
            object? dayValue = _dayMethod.Invoke(env, null);
            object? fractionValue = _fractionField.GetValue(env);
            if (dayValue is not int day || fractionValue is not float fraction)
            {
                WarnOnce("Celestial Dial time seam returned unexpected value types.");
                return false;
            }

            if (day < 0 || float.IsNaN(fraction) || float.IsInfinity(fraction))
            {
                WarnOnce("Celestial Dial time seam returned an invalid day/cycle value.");
                return false;
            }

            if (fraction < -0.001f || fraction > 1.001f)
            {
                WarnOnce("Celestial Dial cycle fraction is outside the expected 0..1 range.");
                return false;
            }

            state = new DayCycleState(day, Mathf.Repeat(fraction, 1f));
            return true;
        }
        catch (TargetInvocationException ex)
        {
            WarnOnce($"Celestial Dial time read failed: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            WarnOnce($"Celestial Dial time read failed: {ex.Message}");
            return false;
        }
    }

    private void Resolve(Type envType)
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        _dayMethod = envType.GetMethod("GetCurrentDay", AnyInstance, null, Type.EmptyTypes, null);
        _fractionField = envType.GetField("m_smoothDayFraction", AnyInstance);

        if (_dayMethod != null && _fractionField != null)
        {
            _log.LogInfo(
                "Celestial Dial resolved candidate time seam: "
                + "GetCurrentDay() + m_smoothDayFraction. Runtime semantics still require acceptance.");
        }
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
