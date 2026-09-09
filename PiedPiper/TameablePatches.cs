using HarmonyLib;

namespace WulfPack.PiedPiper;

/// <summary>
/// Makes every tamed creature commandable.
/// </summary>
/// <remarks>
/// <c>Tameable.m_commandable</c> is authored per-prefab, so only some tames accept a
/// Follow / Stay command in vanilla. Setting it on <c>Awake</c> makes the interaction
/// consistent across every tame.
///
/// The field is not persisted: it is a plain public field, read only by
/// <c>Tameable.Interact</c>, written by no game code, and never paired with a ZDO. It is
/// re-applied from the prefab on every wake, so removing this mod restores vanilla
/// behaviour the next time the component awakes.
///
/// <para>
/// <b>No saddle guard here, deliberately.</b> An earlier revision suppressed commanding
/// whenever the creature carried a saddle, to avoid interfering with riding. That guarded
/// a path which cannot mount: <c>Sadle</c> is a separate <c>Interactable</c> with its own
/// <c>Interact(Humanoid, bool, bool)</c>, and <c>Tameable.Interact</c> contains no saddle
/// or mount reference anywhere in its call graph. The guard could not protect riding, and
/// it did break petting — on a saddled creature the body interaction silently did nothing
/// instead of toggling Follow. Riding is reached by interacting with the saddle, which this
/// mod does not patch.
/// </para>
/// </remarks>
internal static class TameablePatches
{
    [HarmonyPatch(typeof(Tameable), "Awake")]
    private static class TameableAwakePatch
    {
        private static void Postfix(Tameable __instance)
        {
            if (__instance != null)
            {
                __instance.m_commandable = true;
            }
        }
    }
}
