using BepInEx;
using HarmonyLib;

namespace WulfPack.PiedPiper;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.wulfpack.piedpiper";
    public const string PluginName = "Pied Piper";
    public const string PluginVersion = "0.1.0";

    private Harmony? _harmony;

    private void Awake()
    {
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo("Pied Piper loaded. Native Follow / Stay commands are enabled for tameables.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
    }
}
