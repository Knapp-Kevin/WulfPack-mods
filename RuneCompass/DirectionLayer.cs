using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// One rotating indicator on the compass: the transform it turns, how far storm
/// interference is allowed to push it, and where in the wander waveform it sits.
/// </summary>
/// <remarks>
/// This exists because the compass carries four indicators that differ only in which
/// bearing they are handed and how much they suffer in a storm. Without it, each one
/// repeats the same rotation and interference bookkeeping, and adding the fifth would
/// repeat it again.
///
/// <para><b>Wind immunity is structural, not conditional.</b> The wind gust rune is
/// constructed with <see cref="AmplitudeScale"/> of zero, so <see cref="Point"/>
/// annihilates any deflection handed to it. There is no "if this is the wind layer"
/// branch anywhere, because a branch can be forgotten by a later edit and a multiply by
/// zero cannot.</para>
///
/// <para>The fields are <c>readonly</c> and the transform is private for the same reason:
/// immunity that a caller can reach around is a convention, not an invariant.</para>
/// </remarks>
internal sealed class DirectionLayer
{
    private readonly RectTransform _pivot;

    /// <summary>How much of the storm deflection this layer receives, 0 to 1.</summary>
    public readonly float AmplitudeScale;

    /// <summary>Phase offset into the wander waveform. Zero means "move with the others".</summary>
    public readonly float PhaseSeed;

    public DirectionLayer(RectTransform pivot, float amplitudeScale, float phaseSeed)
    {
        _pivot = pivot;
        AmplitudeScale = amplitudeScale;
        PhaseSeed = phaseSeed;
    }

    /// <summary>
    /// Turns the layer to <paramref name="worldBearing"/>, displaced by
    /// <paramref name="deflectionDegrees"/> scaled to this layer's sensitivity.
    /// </summary>
    /// <remarks>
    /// A bearing is clockwise from north and <c>localEulerAngles.z</c> is counter-clockwise
    /// positive, so both terms are subtracted: the bearing through
    /// <see cref="Bearing.BearingRotationZ"/>, and the deflection because a positive
    /// deflection should swing the same way a positive bearing does.
    /// </remarks>
    public void Point(float worldBearing, float deflectionDegrees)
    {
        float z = Bearing.BearingRotationZ(worldBearing) - deflectionDegrees * AmplitudeScale;
        _pivot.localEulerAngles = new Vector3(0f, 0f, z);
    }

    /// <summary>Shows or hides the layer, for the case where its bearing is unavailable.</summary>
    public void SetVisible(bool visible)
    {
        _pivot.gameObject.SetActive(visible);
    }
}
