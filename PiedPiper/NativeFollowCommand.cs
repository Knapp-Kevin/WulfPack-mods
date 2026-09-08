using System;
using System.Reflection;
using BepInEx.Logging;

namespace WulfPack.PiedPiper;

internal static class NativeFollowCommand
{
    private static readonly MethodInfo? CommandMethod = FindCommandMethod();
    private static readonly MethodInfo? HaveSaddleMethod = FindNoArgBoolMethod("HaveSaddle");

    public static bool TryToggle(Tameable tameable, Player player, ManualLogSource log)
    {
        if (tameable == null || player == null || !tameable.IsTamed())
        {
            return false;
        }

        if (IsRideableCommandBlocked(tameable, log))
        {
            player.Message(MessageHud.MessageType.Center, "Pied Piper: remove the saddle before commanding this tame.");
            return false;
        }

        if (CommandMethod == null)
        {
            log.LogError("Pied Piper could not resolve Tameable.Command in the current Valheim build.");
            return false;
        }

        try
        {
            object?[] args = BuildCommandArguments(CommandMethod, player);
            CommandMethod.Invoke(tameable, args);
            return true;
        }
        catch (TargetInvocationException ex)
        {
            log.LogError($"Pied Piper command failed: {ex.InnerException?.Message ?? ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            log.LogError($"Pied Piper command failed: {ex.Message}");
            return false;
        }
    }

    private static MethodInfo? FindCommandMethod()
    {
        foreach (MethodInfo method in typeof(Tameable).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.Equals(method.Name, "Command", StringComparison.Ordinal))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length > 0 && parameters[0].ParameterType.IsAssignableFrom(typeof(Player)))
            {
                return method;
            }
        }

        return null;
    }

    private static MethodInfo? FindNoArgBoolMethod(string name)
    {
        MethodInfo? method = typeof(Tameable).GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        return method?.ReturnType == typeof(bool) ? method : null;
    }

    private static object?[] BuildCommandArguments(MethodInfo method, Player player)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        args[0] = player;

        for (int i = 1; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            if (parameter.HasDefaultValue)
            {
                args[i] = parameter.DefaultValue;
            }
            else if (parameter.ParameterType == typeof(bool))
            {
                args[i] = false;
            }
            else
            {
                args[i] = parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null;
            }
        }

        return args;
    }

    private static bool IsRideableCommandBlocked(Tameable tameable, ManualLogSource log)
    {
        if (tameable.m_saddle == null)
        {
            return false;
        }

        if (HaveSaddleMethod == null)
        {
            log.LogError("Pied Piper could not resolve Tameable.HaveSaddle; rideable command blocked for safety.");
            return true;
        }

        try
        {
            return HaveSaddleMethod.Invoke(tameable, null) is true;
        }
        catch (Exception ex)
        {
            log.LogError($"Pied Piper could not read saddle state; rideable command blocked: {ex.Message}");
            return true;
        }
    }
}
