using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WulfPack.PiedPiper;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.piedpiper";
    public const string PluginName = "Pied Piper";
    public const string PluginVersion = "0.1.0";

    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<KeyboardShortcut> _commandKey = null!;
    private ConfigEntry<float> _commandDistance = null!;

    private void Awake()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable Pied Piper.");
        _commandKey = Config.Bind(
            "Input",
            "CommandKey",
            new KeyboardShortcut(KeyCode.G),
            "Command the tamed creature you are looking at to Follow / Stay.");
        _commandDistance = Config.Bind(
            "Input",
            "CommandDistance",
            5f,
            new ConfigDescription(
                "Maximum distance in metres for the Pied Piper command.",
                new AcceptableValueRange<float>(1f, 15f)));

        Logger.LogInfo("Pied Piper loaded. Default Follow / Stay key: G.");
    }

    private void Update()
    {
        if (!_enabled.Value || !_commandKey.Value.IsDown())
        {
            return;
        }

        Player? player = Player.m_localPlayer;
        Camera? camera = Camera.main;
        if (player == null || camera == null)
        {
            return;
        }

        if (!Physics.Raycast(
                camera.transform.position,
                camera.transform.forward,
                out RaycastHit hit,
                _commandDistance.Value,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Tameable? tameable = hit.collider.GetComponentInParent<Tameable>();
        if (tameable == null)
        {
            return;
        }

        NativeFollowCommand.TryToggle(tameable, player, Logger);
    }
}
