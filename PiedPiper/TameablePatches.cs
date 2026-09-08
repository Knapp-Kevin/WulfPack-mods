using System;
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
            if (__instance == null)
            {
                return;
            }

            // Valheim already owns the Command RPC and MonsterAI follow target.
            // Pied Piper only exposes that existing path to every Tameable.
            __instance.m_commandable = true;
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    private static class TameableInteractPatch
    {
        private static void Prefix(Tameable __instance, ref CommandGuardState __state)
        {
            __state = default;
            if (__instance == null || !__instance.IsTamed() || __instance.m_saddle == null)
            {
                return;
            }

            if (!HasSaddle(__instance) || !__instance.m_commandable)
            {
                return;
            }

            // A saddled rideable remains pettable/renameable, but the normal Use
            // interaction must not switch its movement authority into Follow.
            __state.RestoreCommandable = true;
            __instance.m_commandable = false;
        }

        private static void Postfix(Tameable __instance, CommandGuardState __state)
        {
            if (__instance != null && __state.RestoreCommandable)
            {
                __instance.m_commandable = true;
            }
        }
    }

    private static bool HasSaddle(Tameable tameable)
    {
        if (HaveSaddleMethod == null)
        {
            return false;
        }

        try
        {
            return HaveSaddleMethod.Invoke(tameable, null) is true;
        }
        catch (TargetInvocationException)
        {
            return false;
        }
    }

    private struct CommandGuardState
    {
        public bool RestoreCommandable;
    }
}
