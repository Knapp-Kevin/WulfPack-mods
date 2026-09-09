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
/// <para><b>One of these clamps is load-bearing</b>, and its bound is not a cosmetic
/// default - see <see cref="ClampRamp"/>. Bundling the
/// settings here rather than threading them as parameters is also what keeps the clamp in
/// one place instead of at each call site.</para>
/// </remarks>
internal sealed class CompassSettings
{
    /// <summary>
    /// Upper bound on storm turbulence.
    /// </summary>
    /// <remarks>
    /// <b>This replaces a degree cap that outlived two separate rationales.</b> A ceiling on
    /// deflection first existed because a rotating card inverted its baked N/E/S/W glyphs;
    /// fixing the card retired that. It was then re-justified as keeping a pointer readable
    /// as "wandering rather than spinning" - and the operator then asked for spinning, which
    /// retired that too. A bound argued twice and wrong twice is removed rather than argued a
    /// third time.
    ///
    /// <para>What remains is a sanity bound on the one knob that scales the storm's drift,
    /// wander and lurch together. It exists so a typo cannot produce something unwatchable,
    /// not to protect a property of the display.</para>
    /// </remarks>
    public const float MaxTurbulence = 3f;

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
    public Func<bool> WindShowsSource = () => true;
    public Func<string> SelectedSkin = () => "ClassicWood";
    public Func<string> SkinsRoot = () => string.Empty;

    public Func<bool> InterferenceEnabled = () => true;
    public Func<IReadOnlyList<string>> StormEnvironments = () => Array.Empty<string>();
    public Func<Heightmap.Biome> StormBiomes = () => Heightmap.Biome.None;
    public Func<float> StormTurbulence = () => 1f;
    public Func<float> InterferenceRampSeconds = () => 4f;
    public Func<float> InterferenceReleaseSeconds = () => 12f;
    public Func<bool> HideShipWindIndicator = () => true;
    public Func<bool> IndependentLayerInterference = () => true;

    // Per-layer interference amplitude, live-tunable so a storm can be balanced by eye.
    // There is deliberately no wind amplitude: DirectionLayer.Immune carries that invariant
    // structurally, and a setting would turn it back into a value someone can change.
    public Func<float> AmplitudeCamera = () => 0.85f;
    public Func<float> AmplitudeArrow = () => 1f;

    /// <summary>Keeps turbulence inside the range that still renders as weather.</summary>
    public static float ClampTurbulence(float turbulence)
    {
        return Mathf.Clamp(turbulence, 0f, MaxTurbulence);
    }

    /// <summary>Holds the attack slow enough that the predicate's lead stays invisible.</summary>
    public static float ClampRamp(float seconds)
    {
        return Mathf.Max(MinRampSeconds, seconds);
    }

    /// <summary>
    /// Holds the release above a sanity minimum only.
    /// </summary>
    /// <remarks>
    /// Unlike the attack, this carries no information-leak constraint: the predicate goes
    /// false while the storm is still visibly clearing, so a slow release trails the weather
    /// rather than anticipating it. The floor exists so a typo cannot make the compass snap
    /// back, not to protect a property.
    /// </remarks>
    public static float ClampRelease(float seconds)
    {
        return Mathf.Max(0.5f, seconds);
    }

    /// <summary>
    /// Holds a per-layer capture weight in [0, 1]. Above 1 the interpolation would
    /// overshoot past the storm's own bearing, which is meaningless; below 0 it would swing
    /// away from both.
    /// </summary>
    public static float ClampAmplitude(float scale)
    {
        return Mathf.Clamp01(scale);
    }

    /// <summary>
    /// The biome bits, held here rather than read from <c>Heightmap.Biome</c>.
    /// </summary>
    /// <remarks>
    /// <b>Duplicating an enum is normally a mistake; here it buys testability.</b>
    /// <c>verify-local.ps1</c> runs under Windows PowerShell, whose .NET Framework runtime
    /// cannot load <c>assembly_valheim.dll</c> at all - it uses C# 8 default interface
    /// members, which that runtime rejects outright. Any method that so much as names a
    /// Valheim type, in its signature or its body, is therefore unverifiable, and an
    /// untested parser that silently produced an empty mask would disable the feature with
    /// no symptom at all.
    ///
    /// <para>The duplication does not drift, because it is not trusted: the verify script
    /// reads the real <c>Heightmap/Biome</c> enum out of the installed assembly with
    /// Mono.Cecil - which reads metadata rather than loading it - and fails if this table
    /// and the game disagree on any name or value.</para>
    /// </remarks>
    private static readonly (string Name, int Bit)[] BiomeBits =
    {
        ("Meadows", 1), ("Swamp", 2), ("Mountain", 4), ("BlackForest", 8),
        ("Plains", 16), ("AshLands", 32), ("DeepNorth", 64), ("Ocean", 256),
        ("Mistlands", 512),
    };

    /// <summary>
    /// Parses a comma-separated list of biome names into a single flags mask.
    /// </summary>
    /// <remarks>
    /// Unrecognised entries are skipped rather than throwing: this is a hand-edited file, and
    /// one typo should cost the player that biome, not the whole feature.
    /// </remarks>
    public static int ParseBiomes(string csv)
    {
        int mask = 0;
        foreach (string name in ParseNames(csv))
        {
            foreach ((string biome, int bit) in BiomeBits)
            {
                if (string.Equals(biome, name, StringComparison.OrdinalIgnoreCase))
                {
                    mask |= bit;
                    break;
                }
            }
        }

        return mask;
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
