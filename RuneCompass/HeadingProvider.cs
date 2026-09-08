using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Resolves the direction the player is currently looking, as a world bearing.
/// </summary>
/// <remarks>
/// Uses the flattened main-camera forward vector. Valheim's own <c>Utils.GetMainCamera()</c>
/// is only a per-frame cache around <c>Camera.main</c> (verified in the installed
/// <c>assembly_utils.dll</c>), so <see cref="Camera.main"/> is used directly rather than
/// taking a reference on <c>assembly_utils</c> for no behavioural gain.
/// </remarks>
internal sealed class HeadingProvider
{
    public bool TryGetHeadingDegrees(float calibrationOffset, out float headingDegrees)
    {
        Camera? camera = Camera.main;
        if (camera == null)
        {
            headingDegrees = 0f;
            return false;
        }

        if (!Bearing.TryBearingFromDirection(camera.transform.forward, out float raw))
        {
            headingDegrees = 0f;
            return false;
        }

        headingDegrees = Bearing.Normalize(raw + calibrationOffset);
        return true;
    }
}
