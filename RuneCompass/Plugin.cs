using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WulfPack.RuneCompass;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.runecompass";
    public const string PluginName = "Rune Compass";
    public const string PluginVersion = "0.1.0";

    private CompassController? _controller;
    private ConfigWatcher? _configWatcher;

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<bool> _onlyInNoMap = null!;
    private ConfigEntry<float> _scale = null!;
    private ConfigEntry<float> _opacity = null!;
    private ConfigEntry<float> _offsetX = null!;
    private ConfigEntry<float> _offsetY = null!;
    private ConfigEntry<float> _headingOffset = null!;
    private ConfigEntry<HudAnchor> _anchor = null!;
    private ConfigEntry<bool> _showReadouts = null!;
    private ConfigEntry<string> _selectedSkin = null!;
    private ConfigEntry<bool> _windPointsToward = null!;

    private void Awake()
    {
        BindConfig();
        _configWatcher = new ConfigWatcher(Config, Logger);

        _controller = new CompassController(
            Logger,
            new CompassSettings
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
                WindPointsToward = () => _windPointsToward.Value,
                SkinsRoot = () => SkinsRoot,
            });

        string skins = SkinsRoot;
        Logger.LogInfo(
            $"{PluginName} {PluginVersion} loaded. Skins root: {skins} "
            + $"(exists: {Directory.Exists(skins)}, selected: {_selectedSkin.Value}).");
    }

    private void BindConfig()
    {
        BindGeneral();
        BindDisplay();
        _windPointsToward = Config.Bind(
            "Calibration",
            "WindPointsToward",
            true,
            "true: the wind pointer shows the direction the wind blows TOWARD, matching "
            + "smoke drift and sail push. false: the meteorological convention, showing the "
            + "direction it comes FROM. Flip this and watch smoke from a fire to confirm "
            + "which matches your world.");
        _headingOffset = Config.Bind(
            "Calibration",
            "HeadingOffsetDegrees",
            0f,
            "Optional clockwise heading calibration offset. Leave at 0 unless in-game "
            + "validation proves Valheim's world orientation requires adjustment.");
    }

    private void BindGeneral()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable Rune Compass.");
        _onlyInNoMap = Config.Bind(
            "General",
            "OnlyInNoMap",
            true,
            "Show Rune Compass only when Valheim No Map mode is active.");
    }

    private void BindDisplay()
    {
        _anchor = Config.Bind(
            "Display",
            "Anchor",
            HudAnchor.TopRight,
            "Screen corner the compass is pinned to. TopRight puts it where the minimap "
            + "would be, which is what Rune Compass replaces in No Map play.");
        _showReadouts = Config.Bind(
            "Display",
            "ShowReadouts",
            false,
            "Show the numeric heading and wind readouts under the dial. Off by default: "
            + "Rune Compass gives direction, not instrumentation. Useful for calibration.");
        _selectedSkin = Config.Bind(
            "Display",
            "SelectedSkin",
            "ClassicWood",
            "Skin folder under Assets/Skins. Presentation only - a skin cannot change "
            + "heading, wind semantics, No Map behaviour or config authority. Falls back "
            + "to the primitive HUD if the folder is missing or unreadable.");
        _scale = Config.Bind("Display", "Scale", 1f, "HUD scale multiplier.");
        _opacity = Config.Bind("Display", "Opacity", 0.9f, "HUD opacity from 0 to 1.");
        _offsetX = Config.Bind(
            "Display",
            "OffsetX",
            0f,
            "Horizontal nudge from the anchored resting position, in pixels. Positive is right.");
        _offsetY = Config.Bind(
            "Display",
            "OffsetY",
            0f,
            "Vertical nudge from the anchored resting position, in pixels. Positive is up.");
    }

    /// <summary>
    /// <c>Assets/Skins</c> beside the plugin DLL. BepInEx loads plugins from disk, so the
    /// assembly location is the install directory the build script deployed assets into.
    /// </summary>
    private static string SkinsRoot
    {
        get
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = string.IsNullOrEmpty(location)
                ? Path.Combine(Paths.PluginPath, "RuneCompass")
                : Path.GetDirectoryName(location) ?? string.Empty;
            return Path.Combine(dir, "Assets", "Skins");
        }
    }

    private void Update()
    {
        _configWatcher?.Tick();
        _controller?.Tick();
    }

    private void OnDestroy()
    {
        _controller?.Dispose();
        _controller = null;
    }
}
