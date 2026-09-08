using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Shared bearing math for Rune Compass.
/// </summary>
/// <remarks>
/// Rune Compass uses Valheim's own world-yaw convention: <c>Atan2(dir.x, dir.z)</c> in degrees,
/// normalized to [0, 360). This matches <c>Utils.YawFromDirection</c> in the installed
/// <c>assembly_utils.dll</c>, so a bearing shown here is the same number the game computes
/// internally. 0 = north (+Z), 90 = east (+X), 180 = south, 270 = west.
/// </remarks>
internal static class Bearing
{
    private static readonly string[] CardinalNames =
    {
        "N", "NE", "E", "SE", "S", "SW", "W", "NW",
    };

    /// <summary>Wraps any angle in degrees into [0, 360).</summary>
    public static float Normalize(float degrees)
    {
        degrees %= 360f;
        return degrees < 0f ? degrees + 360f : degrees;
    }

    /// <summary>Converts a world direction to a compass bearing in degrees.</summary>
    public static bool TryBearingFromDirection(Vector3 direction, out float degrees)
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

    /// <summary>Nearest 8-point cardinal name for a bearing.</summary>
    public static string Cardinal(float degrees)
    {
        int index = Mathf.RoundToInt(Normalize(degrees) / 45f) % 8;
        return CardinalNames[index];
    }

    /// <summary>
    /// Unity UI z-rotation that puts a world bearing at its true place on a north-up dial.
    /// </summary>
    /// <remarks>
    /// North-up: the card is fixed with N at 12 o'clock, so a world bearing is an absolute
    /// screen position rather than one relative to where the player is looking.
    /// <c>localEulerAngles.z</c> is counter-clockwise positive and a bearing is clockwise
    /// from north, so bearing <c>b</c> renders at <c>z = -b</c>.
    ///
    /// Every rotating indicator shares this one mapping — the heading marker and the wind
    /// pointer differ only in which bearing they are handed. Nothing needs to know where
    /// the player is looking, which is why the ring itself never moves.
    /// </remarks>
    public static float BearingRotationZ(float worldBearing) => -worldBearing;
}
