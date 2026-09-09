using System;

namespace WulfPack.RuneCompass;

/// <summary>
/// Optional runtime bridge for sibling WulfPack HUD instruments.
/// </summary>
/// <remarks>
/// Consumers discover this type through reflection so Rune Compass keeps no compile-time
/// dependency on Celestial Dial. The bridge controls presentation only.
/// </remarks>
public static class RuneCompassInterop
{
    private static Action<bool>? _setSuppressed;

    public static bool IsAvailable => _setSuppressed != null;

    public static void SetExternalSuppressed(bool suppressed)
    {
        _setSuppressed?.Invoke(suppressed);
    }

    internal static void Bind(Action<bool> setter)
    {
        _setSuppressed = setter;
    }

    internal static void Unbind()
    {
        _setSuppressed = null;
    }
}
