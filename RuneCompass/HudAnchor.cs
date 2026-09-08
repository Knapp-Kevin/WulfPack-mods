using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Which screen corner the compass is pinned to.
/// </summary>
/// <remarks>
/// Anchoring to a corner rather than positioning by absolute pixels keeps the compass in
/// place across resolutions and aspect ratios. <see cref="TopRight"/> is the default
/// because that is where Valheim's minimap sits, and Rune Compass is what replaces it in
/// No Map play.
/// </remarks>
internal enum HudAnchor
{
    TopRight,
    TopCenter,
    TopLeft,
    BottomRight,
    BottomLeft,
}

/// <summary>
/// Applies a <see cref="HudAnchor"/> to the compass panel.
/// </summary>
internal static class HudAnchorLayout
{
    /// <summary>Inset of the panel centre from its corner, in reference-resolution pixels.</summary>
    private const float Inset = 125f;

    /// <summary>Inset from the top edge when anchored to the top centre.</summary>
    private const float TopCentreDrop = 110f;

    /// <summary>
    /// Pins <paramref name="panel"/> to <paramref name="anchor"/> and places it at the
    /// anchor's resting position plus <paramref name="nudge"/>.
    /// </summary>
    public static void Apply(RectTransform panel, HudAnchor anchor, Vector2 nudge)
    {
        Vector2 point = AnchorPoint(anchor);
        panel.anchorMin = point;
        panel.anchorMax = point;
        panel.anchoredPosition = RestingPosition(anchor) + nudge;
    }

    private static Vector2 AnchorPoint(HudAnchor anchor)
    {
        return anchor switch
        {
            HudAnchor.TopCenter => new Vector2(0.5f, 1f),
            HudAnchor.TopLeft => new Vector2(0f, 1f),
            HudAnchor.BottomRight => new Vector2(1f, 0f),
            HudAnchor.BottomLeft => new Vector2(0f, 0f),
            _ => new Vector2(1f, 1f),
        };
    }

    private static Vector2 RestingPosition(HudAnchor anchor)
    {
        return anchor switch
        {
            HudAnchor.TopCenter => new Vector2(0f, -TopCentreDrop),
            HudAnchor.TopLeft => new Vector2(Inset, -Inset),
            HudAnchor.BottomRight => new Vector2(-Inset, Inset),
            HudAnchor.BottomLeft => new Vector2(Inset, Inset),
            _ => new Vector2(-Inset, -Inset),
        };
    }
}
