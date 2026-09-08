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
    /// Unity UI z-rotation for the compass rose. Heading enters the UI here and nowhere else.
    /// </summary>
    /// <remarks>
    /// <c>localEulerAngles.z</c> is counter-clockwise positive; a bearing is clockwise from
    /// north. A bearing <c>b</c> must appear at clockwise <c>b - heading</c> from screen-up,
    /// i.e. at <c>z = -(b - heading)</c>. North sits at the rose's local up (<c>b = 0</c>),
    /// so the rose itself takes <c>z = +heading</c>: face east and the rose's +90 rotation
    /// carries N from up to the left, which is where north actually is.
    /// </remarks>
    public static float RoseRotationZ(float heading) => heading;

    /// <summary>
    /// Unity UI z-rotation for an indicator mounted on the rose, from its world bearing.
    /// </summary>
    /// <remarks>
    /// Pure-z rotations compose additively, so a child of the rose renders at
    /// <c>heading + child</c>. Setting <c>child = -bearing</c> yields <c>heading - bearing</c>,
    /// which is the required <c>-(bearing - heading)</c>. The heading term cancels exactly,
    /// so an indicator mounted here carries a pure world bearing and never needs to know
    /// where the player is looking.
    /// </remarks>
    public static float WindRotationZ(float worldBearing) => -worldBearing;
}
