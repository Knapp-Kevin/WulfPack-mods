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

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<bool> _onlyInNoMap = null!;
    private ConfigEntry<float> _scale = null!;
    private ConfigEntry<float> _opacity = null!;
    private ConfigEntry<float> _offsetX = null!;
    private ConfigEntry<float> _offsetY = null!;
    private ConfigEntry<float> _headingOffset = null!;

    private void Awake()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable Rune Compass.");
        _onlyInNoMap = Config.Bind(
            "General",
            "OnlyInNoMap",
            true,
            "Show Rune Compass only when Valheim No Map mode is active.");
        _scale = Config.Bind("Display", "Scale", 1f, "HUD scale multiplier.");
        _opacity = Config.Bind("Display", "Opacity", 0.9f, "HUD opacity from 0 to 1.");
        _offsetX = Config.Bind("Display", "OffsetX", 0f, "Horizontal offset from top center in pixels.");
        _offsetY = Config.Bind("Display", "OffsetY", -90f, "Vertical offset from top center in pixels.");
        _headingOffset = Config.Bind(
            "Calibration",
            "HeadingOffsetDegrees",
            0f,
            "Optional clockwise heading calibration offset. Leave at 0 unless in-game validation proves Valheim's world orientation requires adjustment.");

        _controller = new CompassController(
            Logger,
            () => _enabled.Value,
            () => _onlyInNoMap.Value,
            () => Mathf.Max(0.25f, _scale.Value),
            () => Mathf.Clamp01(_opacity.Value),
            () => new Vector2(_offsetX.Value, _offsetY.Value),
            () => _headingOffset.Value);

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        _controller?.Tick();
    }

    private void OnDestroy()
    {
        _controller?.Dispose();
        _controller = null;
    }
}
