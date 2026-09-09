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
    private ConfigEntry<bool> _interferenceEnabled = null!;
    private ConfigEntry<string> _stormEnvironments = null!;
    private ConfigEntry<float> _maxDeflection = null!;
    private ConfigEntry<float> _interferenceRamp = null!;
    private ConfigEntry<bool> _independentLayers = null!;

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
                InterferenceEnabled = () => _interferenceEnabled.Value,
                StormEnvironments = () => CompassSettings.ParseNames(_stormEnvironments.Value),
                MaxDeflectionDegrees =
                    () => CompassSettings.ClampDeflection(_maxDeflection.Value),
                InterferenceRampSeconds =
                    () => CompassSettings.ClampRamp(_interferenceRamp.Value),
                IndependentLayerInterference = () => _independentLayers.Value,
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
        BindStorm();
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
    /// Storm interference. Valheim exposes no storm flag of any kind, so storm state is
    /// composed from the environment name - which is what the game does internally too.
    /// </summary>
    private void BindStorm()
    {
        _interferenceEnabled = Config.Bind(
            "Storm",
            "InterferenceEnabled",
            true,
            "Let storms disturb the compass. Set false to keep it accurate in all weather.");
        _stormEnvironments = Config.Bind(
            "Storm",
            "StormEnvironments",
            "ThunderStorm",
            "Comma-separated environment names treated as storms. Ships with only "
            + "ThunderStorm, the one name confirmed present in the game's own code; every "
            + "other environment name lives in compressed asset data and would be a guess. "
            + "On the first world load Rune Compass logs every environment your install "
            + "actually has, with its wind range - add the stormy ones here from that list.");
        BindStormTuning();
    }

    /// <summary>
    /// The two bounded values. Both bounds are load-bearing rather than stylistic, which is
    /// why each config comment states what breaks outside it. Split from
    /// <see cref="BindStorm"/> at exactly the Section 4 limit.
    /// </summary>
    private void BindStormTuning()
    {
        _maxDeflection = Config.Bind(
            "Storm",
            "MaxDeflectionDegrees",
            22f,
            "How far a storm can push the compass off true, in degrees. Clamped to "
            + "35 - past that the N/E/S/W marks baked into the card turn upside down and "
            + "stop reading, which is the exact problem the north-up dial was adopted to fix.");
        _interferenceRamp = Config.Bind(
            "Storm",
            "InterferenceRampSeconds",
            4f,
            "Seconds for interference to reach full strength, and to fade again. Clamped to "
            + "a minimum of 3: Valheim switches the environment name at the START of a "
            + "weather transition, so a fast ramp would make the compass react before the "
            + "sky does and turn it into a storm early-warning device. Raise this if "
            + "interference arrives ahead of the weather.");
        _independentLayers = Config.Bind(
            "Storm",
            "IndependentLayerInterference",
            true,
            "true: the card, camera wedge and character arrow each wander on their own "
            + "phase. false: all three swing together. Comparison setting - the better of "
            + "the two becomes fixed behaviour once it has been judged in a live storm.");
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
