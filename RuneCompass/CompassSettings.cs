using System;
using System.Collections.Generic;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Live accessors for every configurable value the compass reads.
/// </summary>
/// <remarks>
/// Each member is a getter rather than a value so the compass always sees the current
/// config, including edits applied at runtime by <see cref="ConfigWatcher"/>.
///
/// <para><b>Two of these clamps are load-bearing</b>, and their bounds are not cosmetic
/// defaults - see <see cref="ClampDeflection"/> and <see cref="ClampRamp"/>. Bundling the
/// settings here rather than threading them as parameters is also what keeps the clamp in
/// one place instead of at each call site.</para>
/// </remarks>
internal sealed class CompassSettings
{
    /// <summary>
    /// Upper bound on storm deflection, in degrees.
    /// </summary>
    /// <remarks>
    /// A skin's <c>ring.png</c> bakes N/E/S/W into the card that interference rotates. Past
    /// roughly this angle those glyphs stop reading, which is the exact legibility failure
    /// that caused this mod to abandon the heading-up model - at heading 178 a rotating card
    /// rendered E as a mirrored glyph and W as M. A default alone would not prevent it,
    /// because the value is operator-editable and live-reloaded, so the bound is enforced
    /// here rather than merely recommended.
    /// </remarks>
    public const float MaxDeflectionCeiling = 35f;

    /// <summary>
    /// Lower bound on the interference ramp, in seconds.
    /// </summary>
    /// <remarks>
    /// <b>This floor is what keeps the compass from leaking information.</b> Valheim adopts
    /// the incoming environment's name at the very start of a weather transition, so the
    /// storm predicate goes true roughly two seconds before the sky visibly changes. A slow
    /// ramp makes that lead imperceptible - at four seconds the envelope is near 12 percent
    /// half a second in, well below notice. Shorten the ramp for a snappier feel and the
    /// compass becomes a two-second storm early-warning device, which is information the
    /// player cannot get any other way.
    /// </remarks>
    public const float MinRampSeconds = 3f;

    public Func<bool> Enabled = () => true;
    public Func<bool> OnlyInNoMap = () => true;
    public Func<HudAnchor> Anchor = () => HudAnchor.TopRight;
    public Func<float> Scale = () => 1f;
    public Func<float> Opacity = () => 0.9f;
    public Func<Vector2> Offset = () => Vector2.zero;
    public Func<bool> ShowReadouts = () => false;
    public Func<float> HeadingOffset = () => 0f;
    public Func<bool> WindPointsToward = () => true;
    public Func<string> SelectedSkin = () => "ClassicWood";
    public Func<string> SkinsRoot = () => string.Empty;

    public Func<bool> InterferenceEnabled = () => true;
    public Func<IReadOnlyList<string>> StormEnvironments = () => Array.Empty<string>();
    public Func<float> MaxDeflectionDegrees = () => 22f;
    public Func<float> InterferenceRampSeconds = () => 4f;
    public Func<bool> IndependentLayerInterference = () => true;

    /// <summary>Holds deflection inside the range where the card's glyphs stay readable.</summary>
    public static float ClampDeflection(float degrees)
    {
        return Mathf.Clamp(degrees, 0f, MaxDeflectionCeiling);
    }

    /// <summary>Holds the ramp slow enough that the predicate's lead stays invisible.</summary>
    public static float ClampRamp(float seconds)
    {
        return Mathf.Max(MinRampSeconds, seconds);
    }

    /// <summary>
    /// Splits a comma-separated config string into environment names, dropping blanks and
    /// trimming whitespace so a hand-edited list is forgiving.
    /// </summary>
    public static string[] ParseNames(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        // Split(params char[]), not Split(char): the single-char overload is .NET Core 2.0+
        // and is absent from the Mono runtime Valheim ships, so it binds at compile time
        // and throws MissingMethodException in game.
        string[] parts = csv.Split(new[] { ',' });
        List<string> names = new(parts.Length);
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                names.Add(trimmed);
            }
        }

        return names.ToArray();
    }
}
