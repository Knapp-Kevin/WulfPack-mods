using System.Reflection;
using HarmonyLib;

namespace WulfPack.PiedPiper;

internal static class TameablePatches
{
    private static readonly MethodInfo? HaveSaddleMethod = AccessTools.Method(typeof(Tameable), "HaveSaddle");

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

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact), new[] { typeof(Humanoid), typeof(bool), typeof(bool) })]
    private static class TameableInteractPatch
    {
        private static void Prefix(Tameable __instance, ref CommandGuardState __state)
        {
            __state = default;
            if (__instance == null || !__instance.IsTamed() || __instance.m_saddle == null || !__instance.m_commandable)
            {
                return;
            }

            if (HaveSaddleMethod == null)
            {
                __state.RestoreCommandable = true;
                __instance.m_commandable = false;
                return;
            }

            try
            {
                if (HaveSaddleMethod.Invoke(__instance, null) is true)
                {
                    __state.RestoreCommandable = true;
                    __instance.m_commandable = false;
                }
            }
            catch
            {
                // Fail closed on rideables if saddle state cannot be confirmed.
                __state.RestoreCommandable = true;
                __instance.m_commandable = false;
            }
        }

        private static void Postfix(Tameable __instance, CommandGuardState __state)
        {
            if (__instance != null && __state.RestoreCommandable)
            {
                __instance.m_commandable = true;
            }
        }
    }

    private struct CommandGuardState
    {
        public bool RestoreCommandable;
    }
}
