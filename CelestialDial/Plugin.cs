using BepInEx;

namespace WulfPack.CelestialDial;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.celestialdial";
    public const string PluginName = "Celestial Dial";
    public const string PluginVersion = "0.1.0";

    private DialController? _controller;

    private void Awake()
    {
        _controller = new DialController(Logger);
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
