using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Resolves the direction the player's body is facing, as a world bearing.
/// </summary>
/// <remarks>
/// Uses <c>Player.m_localPlayer.transform.forward</c> - the body's own rendered yaw.
///
/// <para><b>Not <c>Character.GetLookDir()</c> or <c>GetLookYaw()</c>.</b> Verified against
/// the installed <c>assembly_valheim.dll</c>: <c>GetLookDir()</c> returns
/// <c>m_eye.forward</c>, <c>GetLookYaw()</c> returns <c>m_lookYaw</c>, and
/// <c>SetLookDir</c> assigns both from the same camera-tracking input. Either would track
/// the camera, collapsing this layer onto <see cref="HeadingProvider"/> and making the
/// whole camera-versus-facing distinction invisible.</para>
///
/// <para><c>Character.UpdateRotation</c> turns the body toward the look yaw over time, so
/// the body genuinely lags and diverges - which is exactly what makes the character arrow
/// hold still while the camera orbits.</para>
/// </remarks>
internal sealed class FacingProvider
{
    public bool TryGetFacingDegrees(float calibrationOffset, out float facingDegrees)
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
        {
            facingDegrees = 0f;
            return false;
        }

        if (!Bearing.TryBearingFromDirection(player.transform.forward, out float raw))
        {
            facingDegrees = 0f;
            return false;
        }

        facingDegrees = Bearing.Normalize(raw + calibrationOffset);
        return true;
    }
}
