using UnityEngine;

namespace WulfPack.VidarShrugged;

/// <summary>
/// The frame-pacing state a benchmark run must be interpreted against.
/// </summary>
/// <remarks>
/// Captured from a running frame rather than from <c>Awake</c>. At plugin load the engine
/// has not yet applied display settings, so <see cref="Screen"/> reports a placeholder
/// surface — on the reference machine that was <c>304x201, fullscreen=False</c> against a
/// real <c>3440x1440</c> fullscreen window. Recording that would stamp every baseline with
/// a resolution the game never ran at.
///
/// It is also re-checked each report, because these values can change mid-session: the
/// player alt-tabs, toggles VSync, or changes resolution. A frame-duration comparison
/// across such a change is not a comparison, so the change is logged where the numbers are.
/// </remarks>
internal readonly struct FramePacingContext
{
    private FramePacingContext(int vSyncCount, int targetFrameRate, int width, int height, bool fullScreen)
    {
        VSyncCount = vSyncCount;
        TargetFrameRate = targetFrameRate;
        Width = width;
        Height = height;
        FullScreen = fullScreen;
    }

    public int VSyncCount { get; }
    public int TargetFrameRate { get; }
    public int Width { get; }
    public int Height { get; }
    public bool FullScreen { get; }

    public static FramePacingContext Capture()
    {
        return new FramePacingContext(
            QualitySettings.vSyncCount,
            Application.targetFrameRate,
            Screen.width,
            Screen.height,
            Screen.fullScreen);
    }

    public bool Matches(FramePacingContext other)
    {
        return VSyncCount == other.VSyncCount
            && TargetFrameRate == other.TargetFrameRate
            && Width == other.Width
            && Height == other.Height
            && FullScreen == other.FullScreen;
    }

    /// <summary>
    /// Whether frame duration is being held by VSync or a frame cap. When it is, a
    /// frame-duration comparison bounds overhead rather than measuring it.
    /// </summary>
    public bool IsPaced => VSyncCount > 0 || TargetFrameRate > 0;

    public override string ToString()
    {
        string pacing = IsPaced
            ? "PACED - frame duration is capped, so it bounds rather than measures overhead"
            : "unpaced";
        return $"vSyncCount={VSyncCount}, targetFrameRate={TargetFrameRate}, "
            + $"resolution={Width}x{Height}, fullscreen={FullScreen} ({pacing})";
    }
}
