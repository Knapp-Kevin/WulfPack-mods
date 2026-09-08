using UnityEngine;

namespace WulfPack.RuneCompass;

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

        Vector3 forward = camera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            headingDegrees = 0f;
            return false;
        }

        forward.Normalize();
        float raw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        headingDegrees = Normalize(raw + calibrationOffset);
        return true;
    }

    private static float Normalize(float degrees)
    {
        degrees %= 360f;
        return degrees < 0f ? degrees + 360f : degrees;
    }
}
