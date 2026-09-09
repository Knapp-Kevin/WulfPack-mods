using System.IO;
using System.Reflection;
using BepInEx;

namespace WulfPack.RuneCompass;

/// <summary>
/// BepInEx lifecycle: bind config, build the controller, tick it, dispose it.
/// </summary>
/// <remarks>
/// Config binding lives in <see cref="CompassConfig"/>. This file previously carried it and
/// had grown to 219 of the 250-line Section 4 limit, with the interference amplitude keys
/// still to come; the split was made before those keys landed rather than after the gate
/// caught them.
/// </remarks>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.runecompass";
    public const string PluginName = "Rune Compass";
    public const string PluginVersion = "0.1.0";

    private CompassController? _controller;
    private ConfigWatcher? _configWatcher;

    private void Awake()
    {
        string skins = SkinsRoot;
        CompassConfig config = new(Config);
        _configWatcher = new ConfigWatcher(Config, Logger);
        _controller = new CompassController(Logger, config.ToSettings(skins));

        Logger.LogInfo(
            $"{PluginName} {PluginVersion} loaded. Skins root: {skins} "
            + $"(exists: {Directory.Exists(skins)}).");
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
