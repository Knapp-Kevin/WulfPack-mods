using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// The optional numeric bearing lines under the dial.
/// </summary>
/// <remarks>
/// Diagnostic, not the normal presentation — Rune Compass gives direction, not
/// instrumentation, and these are off by default. Camera heading and wind only: the
/// character-facing bearing drives the arrow and is deliberately not given a third numeric
/// line, because a new numeric channel should arrive by decision rather than by symmetry.
///
/// <para>Split out of <see cref="CompassUI"/>, which crossed the 250-line Section 4 limit
/// when the north/south needle became its own layer. Text formatting and change-tracking is
/// a separate concern from the geometry of four rotating indicators, so this is where the
/// file wanted to divide anyway.</para>
/// </remarks>
internal sealed class CompassReadouts
{
    private readonly Text _heading;
    private readonly Text _wind;

    private int? _shownHeading;
    private int? _shownWind;
    private bool _visible = true;

    public CompassReadouts(RectTransform panel, Font font)
    {
        // Sit just below the dial, so they follow its size rather than a fixed offset.
        const float below = CompassUiFactory.DialSize * 0.5f;
        _heading = CompassUiFactory.CreateReadout(
            "HeadingText", panel, new Vector2(0f, -(below + 14f)), font, 14);
        _wind = CompassUiFactory.CreateReadout(
            "WindText", panel, new Vector2(0f, -(below + 32f)), font, 12);
    }

    public void SetVisible(bool visible)
    {
        if (_visible == visible)
        {
            return;
        }

        _visible = visible;
        _heading.gameObject.SetActive(visible);
        _wind.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Updates the heading line, skipping the string build when the rounded value has not
    /// moved — this runs every frame.
    /// </summary>
    public void SetHeading(float degrees)
    {
        int shown = Mathf.RoundToInt(degrees);
        if (_shownHeading == shown)
        {
            return;
        }

        _shownHeading = shown;
        _heading.text = $"{shown:000}° {Bearing.Cardinal(shown)}";
    }

    /// <summary>Updates the wind line, or reports that no wind bearing is available.</summary>
    public void SetWind(float? degrees)
    {
        if (!degrees.HasValue)
        {
            if (_shownWind.HasValue)
            {
                _shownWind = null;
                _wind.text = "Wind unavailable";
            }

            return;
        }

        int shown = Mathf.RoundToInt(degrees.Value);
        if (_shownWind == shown)
        {
            return;
        }

        _shownWind = shown;
        _wind.text = $"Wind → {shown:000}° {Bearing.Cardinal(shown)}";
    }
}
