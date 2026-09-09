using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// The storm interference model: where a captured compass layer actually points, and how
/// completely the storm has taken it over.
/// </summary>
/// <remarks>
/// <b>Interference is capture, not displacement.</b> An earlier revision added a wandering
/// offset to the true bearing, which meant the pointer never stopped tracking the player —
/// it tracked them and wobbled. Turning the character turned the pointer with you, so true
/// direction stayed recoverable by moving and watching what followed, and no increase in the
/// offset could have fixed that: a wider offset is still an offset.
///
/// <para>Here the storm supplies its own bearing and the layer is interpolated toward it. At
/// full capture the player's facing has no influence whatsoever.</para>
///
/// <para>Pure math with no Unity object graph, so <c>verify-local.ps1</c> can invoke every
/// part of it by reflection and compare against hand-computed values. Nothing reads game
/// state; the caller supplies time.</para>
/// </remarks>
internal static class Interference
{
    // Incommensurable frequencies, so the sum never repeats on a period a player could
    // learn. Weights fall off with frequency: the slow term carries the drift, the fast one
    // roughens it. Mirrors EnvMan.AddWindOctave, which is how the game builds its own wind.
    private const float SlowRate = 0.37f;
    private const float MidRate = 0.91f;
    private const float FastRate = 2.30f;
    private const float SlowWeight = 0.60f;
    private const float MidWeight = 0.30f;
    private const float FastWeight = 0.10f;

    /// <summary>Degrees per second the storm's field turns of its own accord.</summary>
    private const float DriftRate = 11f;

    /// <summary>How far the smooth wander swings, before turbulence scaling.</summary>
    private const float WanderSpan = 70f;

    /// <summary>Seconds between lurches, and how hard each one hits.</summary>
    private const float LurchPeriod = 3.1f;
    private const float LurchSpan = 120f;

    /// <summary>
    /// Sharpness of the impulse. Higher concentrates the kick into a shorter burst.
    /// </summary>
    private const float LurchSharpness = 6f;

    /// <summary>
    /// A smoothly varying value in [-1, 1] for a layer at <paramref name="phaseSeed"/>.
    /// </summary>
    /// <remarks>
    /// Deterministic from game time. Deliberately not <c>UnityEngine.Random</c>: a per-frame
    /// draw would flicker rather than wander, and would be unreproducible in verification.
    /// Weights sum to exactly 1, so the range is closed at [-1, 1].
    /// </remarks>
    public static float Wander(float timeSeconds, float phaseSeed)
    {
        return SlowWeight * Mathf.Sin(SlowRate * timeSeconds + phaseSeed)
            + MidWeight * Mathf.Sin(MidRate * timeSeconds + phaseSeed)
            + FastWeight * Mathf.Sin(FastRate * timeSeconds + phaseSeed);
    }

    /// <summary>
    /// Sparse, sharp kicks in degrees: the needle being thrown rather than eased.
    /// </summary>
    /// <remarks>
    /// One impulse per <see cref="LurchPeriod"/>, its size and sign taken from a hash of the
    /// pulse index. Because the hash is a function of the index rather than of the frame, the
    /// same moment always produces the same kick — a per-frame random draw would judder.
    ///
    /// <para><b>The envelope must reach zero at both ends of the period, and an earlier
    /// revision did not.</b> It began at full magnitude and decayed, so every pulse boundary
    /// stepped the term from near-nothing to as much as the whole span in a single frame — a
    /// hard discontinuity in the storm's bearing every few seconds. In game that read as
    /// jerkiness, and because it lives in the storm term rather than the player term, it
    /// persisted no matter how the player moved. A kick should rise fast, not teleport.</para>
    ///
    /// <para>The shape is <c>sin(pi * within)</c> raised to <see cref="LurchSharpness"/>:
    /// zero at both ends of the period <b>and</b> zero in its slope there, so a pulse eases
    /// out as the next eases in. A triangular envelope would also have been continuous, but
    /// its corner at the peak leaves a discontinuous derivative, which reads as a snap even
    /// though the value never jumps.</para>
    /// </remarks>
    public static float Lurch(float timeSeconds, float phaseSeed, float turbulence)
    {
        float scaled = timeSeconds / LurchPeriod + phaseSeed;
        float index = Mathf.Floor(scaled);
        float within = scaled - index;

        float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * within), LurchSharpness);
        float magnitude = (Hash(index) * 2f - 1f) * LurchSpan * turbulence;
        return magnitude * envelope;
    }

    /// <summary>
    /// Where the storm is pointing, in degrees. <b>A function of time alone.</b>
    /// </summary>
    /// <remarks>
    /// It takes no bearing, no facing and no input, which is precisely why a captured pointer
    /// cannot be read through by moving: the player is not one of its terms. Drift turns the
    /// field steadily, wander makes it breathe, and lurches throw it.
    /// </remarks>
    public static float StormBearing(float timeSeconds, float phaseSeed, float turbulence)
    {
        return Bearing.Normalize(
            DriftRate * turbulence * timeSeconds
            + WanderSpan * turbulence * Wander(timeSeconds, phaseSeed)
            + Lurch(timeSeconds, phaseSeed, turbulence));
    }

    /// <summary>
    /// The bearing a layer should actually show, given how far the storm has captured it.
    /// </summary>
    /// <remarks>
    /// At <paramref name="capture"/> zero this returns <paramref name="trueBearing"/>
    /// exactly, so clear weather is untouched. At one, the result is the storm's bearing and
    /// the true bearing is irrelevant.
    ///
    /// <para><b>The two bearings are summed as vectors, not interpolated as angles.</b>
    /// <c>Mathf.LerpAngle</c> takes the shortest arc between them, and that arc flips
    /// direction when the bearings pass near 180 degrees apart, so the displayed angle jumps
    /// discontinuously. Because the player's own rotation is one of the inputs, moving
    /// triggers the jump: it read in game as jerkiness tied to movement.</para>
    ///
    /// <para>A weighted sum of two unit vectors is continuous everywhere instead, and it is
    /// the honest model besides: a needle settles at the resultant of the fields acting on
    /// it, which is exactly what a storm overpowering the earth's field would do.</para>
    /// </remarks>
    public static float Captured(
        float trueBearing, float capture, float timeSeconds, float phaseSeed, float turbulence)
    {
        if (capture <= 0f)
        {
            return trueBearing;
        }

        float storm = StormBearing(timeSeconds, phaseSeed, turbulence);
        if (capture >= 1f)
        {
            return storm;
        }

        float trueRad = trueBearing * Mathf.Deg2Rad;
        float stormRad = storm * Mathf.Deg2Rad;
        float earth = 1f - capture;
        float x = earth * Mathf.Sin(trueRad) + capture * Mathf.Sin(stormRad);
        float y = earth * Mathf.Cos(trueRad) + capture * Mathf.Cos(stormRad);

        // Exact cancellation: equal weights on opposing bearings leave no resultant to point
        // at. Vanishingly rare, but Atan2(0, 0) would silently answer "north".
        if (x * x + y * y < 1e-8f)
        {
            return storm;
        }

        return Bearing.Normalize(Mathf.Atan2(x, y) * Mathf.Rad2Deg);
    }

    /// <summary>A stable 0-1 hash of a pulse index. The usual shader idiom.</summary>
    private static float Hash(float n)
    {
        float s = Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }
}

/// <summary>
/// Ramps the storm's hold on the compass in and out, so it takes over and lets go gradually.
/// </summary>
/// <remarks>
/// <b>This exists because the storm predicate cannot be smoothed.</b> Verified in the
/// installed <c>assembly_valheim.dll</c>: <c>EnvMan.InterpolateEnvironment(a, b, i)</c> does
/// <c>clone = a.Clone(); clone.m_name = b.m_name</c> at blend fraction 0, so the current
/// environment's <i>name</i> becomes the incoming one the instant a weather transition
/// starts. The predicate is a step function that leads the visible sky by the whole
/// transition and trails it on the way out. It carries no blend information, so none can be
/// borrowed from it.
///
/// <para>The ramp is linear via <see cref="Mathf.MoveTowards"/>, which releases from whatever
/// level was actually reached. A storm that ends one second into a four-second attack falls
/// from 0.25, not from 1.0, with no special case.</para>
///
/// <para><b>Attack and release are separate durations because their constraints differ.</b>
/// The attack has a floor: the predicate fires about two seconds before the sky visibly
/// changes, and only a slow attack keeps that lead imperceptible. The release has no lead to
/// hide — the predicate goes false while the storm is still clearing — so a long release
/// simply trails the weather, which is how a storm's grip should slacken.</para>
/// </remarks>
internal sealed class InterferenceEnvelope
{
    /// <summary>How completely the storm holds the compass, 0 (clear) to 1 (captured).</summary>
    public float Level { get; private set; }

    /// <summary>
    /// Advances the envelope one frame toward the state <paramref name="stormActive"/>
    /// implies.
    /// </summary>
    public void Tick(
        bool stormActive, float deltaSeconds, float attackSeconds, float releaseSeconds)
    {
        float target = stormActive ? 1f : 0f;
        float duration = stormActive ? attackSeconds : releaseSeconds;
        if (duration <= 0f)
        {
            Level = target;
            return;
        }

        Level = Mathf.MoveTowards(Level, target, deltaSeconds / duration);
    }

    /// <summary>Releases the compass immediately, for when the HUD is hidden or disabled.</summary>
    public void Reset()
    {
        Level = 0f;
    }
}
