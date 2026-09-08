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

    private void Awake()
    {
        BindConfig();
        _configWatcher = new ConfigWatcher(Config, Logger);

        _controller = new CompassController(
            Logger,
            () => _enabled.Value,
            () => _onlyInNoMap.Value,
            () => Mathf.Max(0.25f, _scale.Value),
            () => Mathf.Clamp01(_opacity.Value),
            () => new Vector2(_offsetX.Value, _offsetY.Value),
            () => _anchor.Value,
            () => _headingOffset.Value);

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void BindConfig()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable Rune Compass.");
        _onlyInNoMap = Config.Bind(
            "General",
            "OnlyInNoMap",
            true,
            "Show Rune Compass only when Valheim No Map mode is active.");
        _anchor = Config.Bind(
            "Display",
            "Anchor",
            HudAnchor.TopRight,
            "Screen corner the compass is pinned to. TopRight puts it where the minimap "
            + "would be, which is what Rune Compass replaces in No Map play.");
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
        _headingOffset = Config.Bind(
            "Calibration",
            "HeadingOffsetDegrees",
            0f,
            "Optional clockwise heading calibration offset. Leave at 0 unless in-game validation proves Valheim's world orientation requires adjustment.");
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
