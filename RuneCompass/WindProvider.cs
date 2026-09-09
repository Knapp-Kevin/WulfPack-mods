using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Reads Valheim's live wind vector.
/// </summary>
/// <remarks>
/// Verified against the installed <c>assembly_valheim.dll</c>:
/// <c>EnvMan.GetWindDir()</c> is a public instance method returning <see cref="Vector3"/>,
/// so it is bound at compile time. There is no <c>m_windDir</c> field to fall back to; the
/// backing state is the non-public <c>m_wind</c> / <c>m_windDir1</c> / <c>m_windDir2</c>
/// <see cref="Vector4"/> fields, and <c>GetWindDir()</c> simply returns <c>m_wind.xyz</c>.
///
/// Convention: <c>GetWindDir()</c> returns the direction the wind is blowing TOWARD.
/// Three independent confirmations from the installed assembly, because an operator report
/// initially suggested the opposite:
///
/// 1. <c>Ship.GetWindAngleFactor()</c> computes <c>Dot(GetWindDir(), -transform.forward)</c>
///    and drives sail power to zero as that dot approaches +1 — the cannot-sail-into-the-wind
///    case. That only holds if the vector points downwind.
/// 2. <c>Cinder.FixedUpdate</c> accelerates embers with
///    <c>m_vel += GetWindForce() * dt * m_windStrength</c>, and <c>GetWindForce()</c> is
///    <c>GetWindDir() * m_wind.w</c>. Airborne debris drifts along the vector, so the vector
///    is where things are blown to.
/// 3. <c>Minimap.UpdateWindMarker</c> — the game's own north-up wind indicator — does
///    <c>Euler(0, 0, -LookRotation(GetWindDir()).eulerAngles.y)</c>, which is exactly
///    <see cref="Bearing.BearingRotationZ"/> applied to this bearing. Rune Compass and the
///    vanilla minimap marker therefore point the same way, which is the quickest check
///    available if the convention is ever doubted again.
/// </remarks>
internal sealed class WindProvider
{
    public bool TryGetWindTowardDegrees(out float degrees)
    {
        EnvMan? env = EnvMan.instance;
        if (env == null)
        {
            degrees = 0f;
            return false;
        }

        return Bearing.TryBearingFromDirection(env.GetWindDir(), out degrees);
    }
}
