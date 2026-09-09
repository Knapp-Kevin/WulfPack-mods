using System;
using System.Reflection;
using BepInEx.Logging;

namespace WulfPack.CelestialDial;

internal sealed class RuneCompassBridge
{
    private const string InteropTypeName = "WulfPack.RuneCompass.RuneCompassInterop";
    private const string SetterName = "SetExternalSuppressed";

    private readonly ManualLogSource _log;
    private bool _warned;

    public RuneCompassBridge(ManualLogSource log)
    {
        _log = log;
    }

    public bool TrySetSuppressed(bool suppressed)
    {
        Type? interop = FindInteropType(out bool runeCompassLoaded);
        if (interop == null)
        {
            if (runeCompassLoaded)
            {
                WarnOnce("Rune Compass is loaded but exposes no compatible WulfPack instrument bridge.");
                return false;
            }
            return true;
        }

        MethodInfo? setter = interop.GetMethod(
            SetterName,
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(bool) },
            null);
        if (setter == null)
        {
            WarnOnce("Rune Compass interop type is present but its suppression method is missing.");
            return false;
        }

        try
        {
            setter.Invoke(null, new object[] { suppressed });
            return true;
        }
        catch (Exception ex)
        {
            WarnOnce($"Rune Compass interop failed: {ex.Message}");
            return false;
        }
    }

    private static Type? FindInteropType(out bool runeCompassLoaded)
    {
        runeCompassLoaded = false;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (string.Equals(name, "RuneCompass", StringComparison.OrdinalIgnoreCase))
            {
                runeCompassLoaded = true;
            }

            Type? type = assembly.GetType(InteropTypeName, false);
            if (type != null)
            {
                runeCompassLoaded = true;
                return type;
            }
        }
        return null;
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
