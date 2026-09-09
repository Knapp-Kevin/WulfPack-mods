using System;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// One rotating indicator on the compass: the transform it turns, how far storm
/// interference is allowed to push it, and where in the wander waveform it sits.
/// </summary>
/// <remarks>
/// This exists because the compass carries several indicators that differ only in which
/// bearing they are handed and how much they suffer in a storm. Without it, each one
/// repeats the same rotation and interference bookkeeping.
///
/// <para><b>Wind immunity is structural, not conditional.</b> The wind gust rune is
/// constructed with <see cref="Immune"/>, so <see cref="Point"/> annihilates any deflection
/// handed to it. There is no "if this is the wind layer" branch anywhere, because a branch
/// can be forgotten by a later edit and a multiply by zero cannot.</para>
///
/// <para>The fields are <c>readonly</c> and the transform is private for the same reason:
/// immunity that a caller can reach around is a convention, not an invariant.</para>
/// </remarks>
internal sealed class DirectionLayer
{
    /// <summary>
    /// An amplitude supplier that always yields zero, for a layer that must never be
    /// disturbed no matter what any configuration says.
    /// </summary>
    /// <remarks>
    /// The wind rune is built with this. It is a literal, not a settings read, which is what
    /// keeps wind's immunity structural now that the other amplitudes are live-tunable:
    /// there is no configuration key that can reach this value, because there is no
    /// configuration behind it.
    /// </remarks>
    public static readonly Func<float> Immune = () => 0f;

    private readonly RectTransform _pivot;
    private readonly Func<float> _amplitudeScale;

    /// <summary>Phase offset into the wander waveform. Zero means "move with the others".</summary>
    public readonly float PhaseSeed;

    /// <param name="amplitudeScale">
    /// Read per frame so a live config edit takes effect without a rebuild. Built once by
    /// the caller and held here; constructing it per frame would allocate a delegate on
    /// every tick of a HUD that never stops ticking.
    /// </param>
    public DirectionLayer(RectTransform pivot, Func<float> amplitudeScale, float phaseSeed)
    {
        _pivot = pivot;
        _amplitudeScale = amplitudeScale;
        PhaseSeed = phaseSeed;
    }

    /// <summary>
    /// Turns the layer to <paramref name="worldBearing"/>, or toward wherever the storm has
    /// dragged it, according to how far <paramref name="storm"/> has captured it.
    /// </summary>
    /// <remarks>
    /// The capture is computed here rather than by the caller, which is what keeps wind's
    /// immunity structural: the wind layer's supplier is
    /// <see cref="Immune"/>, so its capture is always zero and
    /// <see cref="Interference.Captured"/> hands back the true bearing untouched. A caller
    /// cannot route a storm into this layer by passing a different argument, because the
    /// argument it would have to change is not one it holds.
    /// </remarks>
    public void Point(float worldBearing, StormState storm)
    {
        float shown = Interference.Captured(
            worldBearing,
            storm.Level * _amplitudeScale(),
            storm.Time,
            storm.IndependentLayers ? PhaseSeed : 0f,
            storm.Turbulence);
        _pivot.localEulerAngles =
            new Vector3(0f, 0f, Bearing.BearingRotationZ(shown));
    }

    /// <summary>Shows or hides the layer, for the case where its bearing is unavailable.</summary>
    public void SetVisible(bool visible)
    {
        _pivot.gameObject.SetActive(visible);
    }
}

/// <summary>
/// How hard the storm is gripping the compass this frame, and where in its cycle it is.
/// </summary>
/// <remarks>
/// Bundled so a layer takes one argument rather than three, and so adding a term to the storm
/// model does not change every call site. A value type: it is read many times per frame and
/// never mutated.
/// </remarks>
internal readonly struct StormState
{
    /// <summary>0 = clear, 1 = the storm has full hold.</summary>
    public readonly float Level;

    /// <summary>Game time, the only input the storm's own bearing takes.</summary>
    public readonly float Time;

    /// <summary>Scales drift, wander and lurch together: unsettled through to violent.</summary>
    public readonly float Turbulence;

    /// <summary>
    /// Whether each layer is captured toward its own storm bearing, or all toward one.
    /// </summary>
    /// <remarks>
    /// Carried here rather than read by each layer, because a layer consulting settings
    /// directly is how this stopped being honoured in the first place: the previous model
    /// read the toggle inside a helper, the helper was deleted along with that model, and the
    /// setting silently became decoration that nothing consumed.
    /// </remarks>
    public readonly bool IndependentLayers;

    public StormState(float level, float time, float turbulence, bool independentLayers)
    {
        Level = level;
        Time = time;
        Turbulence = turbulence;
        IndependentLayers = independentLayers;
    }
}
