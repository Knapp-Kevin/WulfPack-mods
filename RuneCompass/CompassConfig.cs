using BepInEx.Configuration;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Every configurable value the compass exposes, bound in one place and handed to the
/// controller as a <see cref="CompassSettings"/>.
/// </summary>
/// <remarks>
/// Split out of <see cref="Plugin"/>, which is a BepInEx lifecycle class and had accumulated
/// roughly 150 lines of <c>Config.Bind</c> calls that have nothing to do with lifecycle. The
/// split is the right shape on its own terms; it was forced by the Section 4 Razor, which
/// this file's growth would otherwise have breached when the amplitude keys landed.
///
/// <para>Clamps live on <see cref="CompassSettings"/> rather than here, so a value is bounded
/// wherever it is read rather than only where it happens to be bound.</para>
/// </remarks>
internal sealed class CompassConfig
{
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<bool> _onlyInNoMap;
    private readonly ConfigEntry<HudAnchor> _anchor;
    private readonly ConfigEntry<bool> _showReadouts;
    private readonly ConfigEntry<string> _selectedSkin;
    private readonly ConfigEntry<float> _scale;
    private readonly ConfigEntry<float> _opacity;
    private readonly ConfigEntry<float> _offsetX;
    private readonly ConfigEntry<float> _offsetY;
    private readonly ConfigEntry<bool> _windShowsSource;
    private readonly ConfigEntry<float> _headingOffset;
    private readonly ConfigEntry<bool> _hideShipWind;
    private readonly StormConfig _storm;

    public CompassConfig(ConfigFile config)
    {
        (_enabled, _onlyInNoMap, _anchor, _showReadouts, _selectedSkin) = BindGeneral(config);
        (_scale, _opacity, _offsetX, _offsetY) = BindPlacement(config);
        (_windShowsSource, _headingOffset) = BindCalibration(config);
        _hideShipWind = config.Bind(
            "Display", "HideShipWindIndicator", true,
            "Hide Valheim's own wind gauge while aboard a ship, since Rune Compass already "
            + "shows wind and the two use opposite conventions. Restored to exactly the "
            + "state it was found in whenever the compass hides or the mod is disabled.");
        _storm = new StormConfig(config);
    }

    /// <summary>Visibility and presentation identity.</summary>
    private static (ConfigEntry<bool>, ConfigEntry<bool>, ConfigEntry<HudAnchor>,
        ConfigEntry<bool>, ConfigEntry<string>) BindGeneral(ConfigFile config)
    {
        ConfigEntry<bool> enabled = config.Bind("General", "Enabled", true, "Enable Rune Compass.");
        ConfigEntry<bool> onlyInNoMap = config.Bind(
            "General", "OnlyInNoMap", true,
            "Show Rune Compass only when Valheim No Map mode is active.");
        ConfigEntry<HudAnchor> anchor = config.Bind(
            "Display", "Anchor", HudAnchor.TopRight,
            "Screen corner the compass is pinned to. TopRight puts it where the minimap "
            + "would be, which is what Rune Compass replaces in No Map play.");
        ConfigEntry<bool> showReadouts = config.Bind(
            "Display", "ShowReadouts", false,
            "Show the numeric heading and wind readouts under the dial. Off by default: "
            + "Rune Compass gives direction, not instrumentation. Useful for calibration.");
        ConfigEntry<string> selectedSkin = config.Bind(
            "Display", "SelectedSkin", "ClassicWood",
            "Skin folder under Assets/Skins. Presentation only - a skin cannot change "
            + "heading, wind semantics, No Map behaviour or config authority. Falls back "
            + "to the primitive HUD if the folder is missing or unreadable.");
        return (enabled, onlyInNoMap, anchor, showReadouts, selectedSkin);
    }

    /// <summary>Where on screen the dial sits, and how loud it is.</summary>
    private static (ConfigEntry<float>, ConfigEntry<float>, ConfigEntry<float>,
        ConfigEntry<float>) BindPlacement(ConfigFile config)
    {
        ConfigEntry<float> scale = config.Bind("Display", "Scale", 1f, "HUD scale multiplier.");
        ConfigEntry<float> opacity = config.Bind(
            "Display", "Opacity", 0.9f, "HUD opacity from 0 to 1.");
        ConfigEntry<float> offsetX = config.Bind(
            "Display", "OffsetX", 0f,
            "Horizontal nudge from the anchored resting position, in pixels. Positive is right.");
        ConfigEntry<float> offsetY = config.Bind(
            "Display", "OffsetY", 0f,
            "Vertical nudge from the anchored resting position, in pixels. Positive is up.");
        return (scale, opacity, offsetX, offsetY);
    }

    /// <summary>The two escape hatches, both expected to stay at their defaults.</summary>
    private static (ConfigEntry<bool>, ConfigEntry<float>) BindCalibration(ConfigFile config)
    {
        ConfigEntry<bool> windShowsSource = config.Bind(
            "Calibration", "WindShowsSource", true,
            "true: the wind rune sits on the side the wind comes FROM, the way a quarter is "
            + "named on a compass rose. false: it sits downwind instead, where an arrow "
            + "would point. The game reports wind as a toward-vector either way; this only "
            + "chooses which end of it the glyph marks.");
        ConfigEntry<float> headingOffset = config.Bind(
            "Calibration", "HeadingOffsetDegrees", 0f,
            "Optional clockwise heading calibration offset. Leave at 0 unless in-game "
            + "validation proves Valheim's world orientation requires adjustment.");
        return (windShowsSource, headingOffset);
    }


    /// <summary>Live accessors over the bound entries, re-read every frame.</summary>
    public CompassSettings ToSettings(string skinsRoot)
    {
        CompassSettings settings = new()
        {
            Enabled = () => _enabled.Value,
            OnlyInNoMap = () => _onlyInNoMap.Value,
            Anchor = () => _anchor.Value,
            Scale = () => Mathf.Max(0.25f, _scale.Value),
            Opacity = () => Mathf.Clamp01(_opacity.Value),
            Offset = () => new Vector2(_offsetX.Value, _offsetY.Value),
            ShowReadouts = () => _showReadouts.Value,
            HeadingOffset = () => _headingOffset.Value,
            SelectedSkin = () => _selectedSkin.Value,
            WindShowsSource = () => _windShowsSource.Value,
            SkinsRoot = () => skinsRoot,
            HideShipWindIndicator = () => _hideShipWind.Value,
        };
        _storm.Apply(settings);
        return settings;
    }
}
