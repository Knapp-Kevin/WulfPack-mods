using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// The north-up compass HUD and its four directional signals.
/// </summary>
/// <remarks>
/// North is the fixed reference. Every indicator is handed an absolute world bearing and
/// rendered at <see cref="Bearing.BearingRotationZ"/>; nothing needs to know where the
/// player is looking.
///
/// <para>The four signals are ranked by prominence, back to front, so they are told apart
/// by place and size rather than by colour alone:</para>
/// <list type="number">
/// <item>the <b>camera wedge</b>, a quiet background sector showing where the view points;</item>
/// <item>the <b>dial face</b>, a fixed frame that never rotates and carries north;</item>
/// <item>the <b>character arrow</b>, centre-mounted and the longest thing on the dial - this
/// is what the compass is for;</item>
/// <item>the <b>wind rune</b>, a small glyph orbiting outside the rim, about a third the
/// arrow's size so wind never competes with facing.</item>
/// </list>
///
/// <para>Which layers move is not a skin's choice. A skin supplies artwork and a resting
/// scale.</para>
/// </remarks>
internal sealed class CompassUI : IDisposable
{
    // Phase offsets used when layers wander independently rather than in lockstep.
    // Public because the controller samples the wander waveform per layer.
    public const float WedgePhaseSeed = 2.1f;
    public const float ArrowPhaseSeed = 4.7f;

    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly DirectionLayer _cameraWedge;
    private readonly DirectionLayer _characterArrow;
    private readonly DirectionLayer _windGust;
    private readonly CompassReadouts _readouts;
    private readonly CanvasGroup _canvasGroup;
    private readonly float _skinScale;

    public CompassUI(CompassSkin? skin, CompassSettings settings)
    {
        bool skinned = skin is { IsUsable: true };
        _skinScale = skinned ? skin!.DefaultScale : 1f;

        _root = CompassUiFactory.BuildRoot();
        _canvasGroup = CreateCanvasGroup(_root);
        _panel = CompassUiFactory.BuildPanel(_root.transform);
        Font font = CompassUiFactory.CreateFont();

        // A Canvas draws depth-first in pre-order, so construction order is z-order.
        if (skinned && skin!.Base != null)
        {
            CompassUiFactory.CreateSkinLayer("SkinBase", _panel, skin.Base);
        }

        // Amplitude suppliers are built once here, never per frame: this HUD ticks
        // continuously, and a delegate allocated inside the render path would churn.
        _cameraWedge = new DirectionLayer(
            CompassLayers.CreateCameraWedge(_panel, skin?.CameraWedge),
            settings.AmplitudeCamera,
            WedgePhaseSeed);

        // The dial face is a FIXED frame, not a rotating layer. It was originally a
        // DirectionLayer at full amplitude, which made the whole card swing in a storm -
        // and a moving frame both reads as a broken HUD and drowns the pointers' own
        // motion, since motion is only legible against something still.
        BuildFrame(skin, skinned, font);

        (_characterArrow, _windGust) = BuildPointers(skin, settings);
        _readouts = new CompassReadouts(_panel, font);
        SetVisible(false);
    }

    /// <summary>
    /// The two pointers above the dial face: the character arrow - which is the compass's
    /// needle, there being only one - and the small wind rune orbiting outside the rim.
    /// </summary>
    /// <remarks>
    /// Wind is constructed with <see cref="DirectionLayer.Immune"/> rather than a settings
    /// accessor. That is the whole of its protection: there is no configuration key behind
    /// it to raise, so no edit anywhere can make the wind rune lie.
    /// </remarks>
    private (DirectionLayer Arrow, DirectionLayer Wind) BuildPointers(
        CompassSkin? skin, CompassSettings settings)
    {
        return (
            new DirectionLayer(
                CompassLayers.CreateCharacterArrow(_panel, skin?.CharacterArrow),
                settings.AmplitudeArrow,
                ArrowPhaseSeed),
            new DirectionLayer(
                CompassLayers.CreateWindGust(_panel, skin?.WindGust),
                DirectionLayer.Immune,
                0f));
    }

    /// <summary>
    /// Builds the dial face: the skin's ring when available, primitive cardinal glyphs
    /// otherwise. It never rotates, so it returns nothing to turn.
    /// </summary>
    /// <remarks>
    /// Because it holds still, <c>N</c> is permanently at 12 o'clock and the needle rests
    /// exactly on it in calm weather. A storm then shows as the needle drifting off the mark
    /// it should be sitting on, which reads far better than drift against nothing - and the
    /// baked N/E/S/W glyphs can never tilt, which retires the legibility hazard that made
    /// this mod abandon the heading-up model.
    /// </remarks>
    private void BuildFrame(CompassSkin? skin, bool skinned, Font font)
    {
        RectTransform frame = CompassUiFactory.BuildCard(_panel);
        if (skinned)
        {
            CompassUiFactory.CreateSkinLayer("SkinRing", frame, skin!.Ring!);
        }
        else
        {
            CompassLayers.AddCardinals(frame, font);
        }
    }

    private static CanvasGroup CreateCanvasGroup(GameObject root)
    {
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public void ApplyLayout(float scale, float opacity, Vector2 offset, HudAnchor anchor)
    {
        // The skin's defaultScale is the size its art was drawn for; the player's Scale
        // multiplies it rather than replacing it.
        _panel.localScale = Vector3.one * (scale * _skinScale);
        HudAnchorLayout.Apply(_panel, anchor, offset);
        _canvasGroup.alpha = opacity;
    }

    public void SetReadoutsVisible(bool visible)
    {
        _readouts.SetVisible(visible);
    }

    /// <summary>Points the camera wedge at the bearing the view faces.</summary>
    /// <remarks>
    /// The readout keeps reporting the TRUE bearing even while the wedge is captured. It is a
    /// calibration instrument, not part of the fiction, and it is off by default.
    /// </remarks>
    public void SetCameraHeading(float degrees, StormState storm)
    {
        _cameraWedge.Point(degrees, storm);
        _readouts.SetHeading(degrees);
    }

    /// <summary>Points the character arrow at the bearing the player's body faces.</summary>
    public void SetFacing(float degrees, StormState storm)
    {
        _characterArrow.Point(degrees, storm);
    }

    /// <summary>
    /// Orbits the wind rune to the bearing the wind blows toward. It is handed the same
    /// deflection as every other layer and immune to it by construction, because its
    /// amplitude scale is zero.
    /// </summary>
    public void SetWind(float? degrees, StormState storm)
    {
        _readouts.SetWind(degrees);
        if (!degrees.HasValue)
        {
            _windGust.SetVisible(false);
            return;
        }

        _windGust.SetVisible(true);
        _windGust.Point(degrees.Value, storm);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(_root);
    }
}
