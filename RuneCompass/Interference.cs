using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// The storm interference model: how far a compass layer is pushed off true, and how
/// that displacement grows and fades.
/// </summary>
/// <remarks>
/// Pure math with no Unity object graph, so <c>verify-local.ps1</c> can invoke every part
/// of it by reflection against the compiled assembly and compare with hand-computed
/// values. Nothing here reads game state; the caller supplies time and storm state.
/// </remarks>
internal static class Interference
{
    // Incommensurable frequencies, so the sum never repeats on a period a player could
    // learn. Weights fall off with frequency: the slow term carries the drift, the fast
    // one only roughens it. Chosen to mirror EnvMan.AddWindOctave, which is how the game
    // itself builds its wind.
    private const float SlowRate = 0.37f;
    private const float MidRate = 0.91f;
    private const float FastRate = 2.30f;
    private const float SlowWeight = 0.60f;
    private const float MidWeight = 0.30f;
    private const float FastWeight = 0.10f;

    /// <summary>
    /// A smoothly varying displacement in [-1, 1] for a layer at
    /// <paramref name="phaseSeed"/>.
    /// </summary>
    /// <remarks>
    /// Deterministic from game time. Deliberately not <c>UnityEngine.Random</c>: a
    /// per-frame draw would flicker rather than wander, and would make the result
    /// unreproducible in verification. Nothing is stored between calls, which is what keeps
    /// the whole feature free of save state.
    ///
    /// <para>Weights sum to exactly 1, so the range is closed at [-1, 1] and
    /// <c>MaxDeflectionDegrees</c> means what it says.</para>
    /// </remarks>
    public static float Wander(float timeSeconds, float phaseSeed)
    {
        return SlowWeight * Mathf.Sin(SlowRate * timeSeconds + phaseSeed)
            + MidWeight * Mathf.Sin(MidRate * timeSeconds + phaseSeed)
            + FastWeight * Mathf.Sin(FastRate * timeSeconds + phaseSeed);
    }

    /// <summary>
    /// The displacement in degrees for one layer, given the current envelope level.
    /// </summary>
    /// <remarks>
    /// Returns exactly zero at <paramref name="level"/> zero, whatever the time or seed.
    /// That is the calm-weather guarantee: in clear weather the interference term
    /// contributes no rotation at all, and every layer rests on its true bearing.
    /// </remarks>
    public static float Deflection(
        float level, float maxDeflectionDegrees, float timeSeconds, float phaseSeed)
    {
        if (level <= 0f)
        {
            return 0f;
        }

        return level * maxDeflectionDegrees * Wander(timeSeconds, phaseSeed);
    }
}

/// <summary>
/// Ramps interference in and out, so a storm's arrival and departure are gradual.
/// </summary>
/// <remarks>
/// <b>This exists because the storm predicate cannot be smoothed.</b> Verified in the
/// installed <c>assembly_valheim.dll</c>: <c>EnvMan.InterpolateEnvironment(a, b, i)</c>
/// does <c>clone = a.Clone(); clone.m_name = b.m_name</c> at blend fraction 0, so the
/// current environment's <i>name</i> becomes the incoming one the instant a weather
/// transition starts. The predicate is a step function that leads the visible sky by the
/// whole transition and trails it on the way out. It carries no blend information, so none
/// can be borrowed from it.
///
/// <para>The ramp is linear via <see cref="Mathf.MoveTowards"/>, which releases from
/// whatever level was actually reached. A storm that ends one second into a four-second
/// attack falls from 0.25, not from 1.0, with no special case.</para>
/// </remarks>
internal sealed class InterferenceEnvelope
{
    /// <summary>Current interference strength, 0 (clear) to 1 (fully disturbed).</summary>
    public float Level { get; private set; }

    /// <summary>
    /// Advances the envelope one frame toward the state <paramref name="stormActive"/>
    /// implies.
    /// </summary>
    public void Tick(bool stormActive, float deltaSeconds, float rampSeconds)
    {
        float target = stormActive ? 1f : 0f;
        if (rampSeconds <= 0f)
        {
            Level = target;
            return;
        }

        Level = Mathf.MoveTowards(Level, target, deltaSeconds / rampSeconds);
    }

    /// <summary>Drops interference immediately, for when the HUD is hidden or disabled.</summary>
    public void Reset()
    {
        Level = 0f;
    }
}
