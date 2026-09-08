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
/// Confirmed from <c>Ship.GetWindAngleFactor()</c>, which computes
/// <c>Dot(GetWindDir(), -transform.forward)</c> and drives sail power to zero as that dot
/// approaches +1 — the no-sailing-into-the-wind case. That only holds if the vector points
/// downwind, which is the convention Rune Compass displays.
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
