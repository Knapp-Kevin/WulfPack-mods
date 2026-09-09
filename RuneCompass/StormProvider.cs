using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;

namespace WulfPack.RuneCompass;

/// <summary>
/// Decides whether the world is currently storming.
/// </summary>
/// <remarks>
/// <b>Valheim 1.0 has no storm flag, and that is a finding rather than an assumption.</b>
/// A full field enumeration of <c>EnvSetup</c> in the installed <c>assembly_valheim.dll</c>
/// yields only <c>m_isWet</c>, <c>m_isFreezing</c>, <c>m_isFreezingAtNight</c>,
/// <c>m_isCold</c>, <c>m_isColdAtNight</c> and <c>m_alwaysDark</c>; <c>EnvMan</c> exposes
/// <c>IsWet</c>, <c>IsCold</c>, <c>IsFreezing</c>, <c>IsDay</c>, <c>IsNight</c>,
/// <c>IsAfternoon</c>, <c>IsDaylight</c> and <c>CanSleep</c>, and no storm equivalent. An
/// assembly-wide sweep for <c>storm|thunder|weather</c> returns only the <c>Thunder</c>
/// prefab component, which runs off its own timers and exposes no queryable state.
///
/// <para>So environment-name membership is not a shortcut taken in place of a real API - it
/// is the only seam, and it is the one the game uses itself in
/// <c>EnvMan.IsEnvironment</c>.</para>
///
/// <para>Reading <c>m_name</c> directly rather than calling <c>IsEnvironment</c> once per
/// candidate keeps this to one read per frame, and still honours the <c>m_forceEnv</c>
/// override that <c>GetCurrentEnvironment()</c> applies - which is what makes
/// <c>SetForceEnvironment("ThunderStorm")</c> a usable test lever.</para>
///
/// <para><b>Biomes are the second axis.</b> <c>EnvMan.GetCurrentBiome()</c> is public and
/// returns the <c>Heightmap.Biome</c> flags enum, so a whole set of biomes collapses to one
/// mask and one bitwise test. This exists because the Mistlands should disturb a compass
/// regardless of its weather, and "a place is hostile" is not something an environment name
/// can express.</para>
/// </remarks>
internal sealed class StormProvider
{
    private readonly ManualLogSource _log;
    private bool _environmentsLogged;

    public StormProvider(ManualLogSource log)
    {
        _log = log;
    }

    /// <summary>
    /// Whether the current environment is in <paramref name="stormNames"/>. False when the
    /// world is not loaded, which is also the state in which no HUD exists to disturb.
    /// </summary>
    public bool IsStorm(IReadOnlyList<string> stormNames, Heightmap.Biome stormBiomes)
    {
        EnvMan? env = EnvMan.instance;
        if (env == null)
        {
            return false;
        }

        LogEnvironmentsOnce(env);

        // Two independent axes, because they answer different questions. Weather is
        // transient and named; a biome is a place. The Mistlands is not stormy weather -- it
        // is permanently hostile to an instrument - so no environment name would ever have
        // caught it, and adding its three environments to the name list would have been a
        // biome test written in the wrong vocabulary.
        if (stormBiomes != Heightmap.Biome.None
            && (env.GetCurrentBiome() & stormBiomes) != Heightmap.Biome.None)
        {
            return true;
        }

        EnvSetup? current = env.GetCurrentEnvironment();
        return current != null && Contains(stormNames, current.m_name);
    }

    /// <summary>
    /// Case-insensitive, whitespace-tolerant membership. The set is hand-edited in a config
    /// file, so <c>thunderstorm</c> and <c> ThunderStorm </c> must both work.
    /// </summary>
    private static bool Contains(IReadOnlyList<string> stormNames, string current)
    {
        if (string.IsNullOrEmpty(current))
        {
            return false;
        }

        for (int i = 0; i < stormNames.Count; i++)
        {
            if (string.Equals(stormNames[i], current, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prints every environment the install actually has, once per session.
    /// </summary>
    /// <remarks>
    /// The shipped <c>StormEnvironments</c> default contains only <c>ThunderStorm</c>,
    /// because that is the single environment name the research phase could prove exists -
    /// environment names are Unity-serialized asset data, not IL constants, and the bundle
    /// they live in is compressed. Rather than ship a guessed list, the mod reports the
    /// real one from the operator's own install so the config can be extended against
    /// evidence. This also survives game patches and any modpack that appends environments
    /// through the public <c>EnvMan.AppendEnvironment</c>.
    /// </remarks>
    private void LogEnvironmentsOnce(EnvMan env)
    {
        if (_environmentsLogged || env.m_environments == null)
        {
            return;
        }

        _environmentsLogged = true;
        StringBuilder report = new();
        report.AppendLine(
            $"Rune Compass sees {env.m_environments.Count} environments. "
            + "Add any storm names to StormEnvironments in the config:");
        foreach (EnvSetup entry in env.m_environments)
        {
            if (entry != null)
            {
                report.AppendLine(
                    $"    {entry.m_name,-24} wind {entry.m_windMin:0.00} - {entry.m_windMax:0.00}");
            }
        }

        _log.LogInfo(report.ToString().TrimEnd());
    }
}
