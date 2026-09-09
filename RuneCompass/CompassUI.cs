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
/// <item>the <b>card</b>, carrying the north/south reference, subordinate by design;</item>
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
    // How hard a storm pushes each layer. The card is the magnetically sensitive element
    // and suffers most; the character arrow is the primary read and suffers least, so the
    // compass degrades without becoming unusable. Wind is exactly zero - see LD-7.
    private const float CardAmplitude = 1.00f;
    private const float WedgeAmplitude = 0.70f;
    private const float ArrowAmplitude = 0.50f;
    private const float WindAmplitude = 0.00f;

    // Phase offsets used when layers wander independently rather than in lockstep.
    // Public because the controller samples the wander waveform per layer; keeping the
    // seeds here keeps them beside the amplitudes they pair with.
    public const float CardPhaseSeed = 0f;
    public const float WedgePhaseSeed = 2.1f;
    public const float ArrowPhaseSeed = 4.7f;

    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly DirectionLayer _card;
    private readonly DirectionLayer _cameraWedge;
    private readonly DirectionLayer _characterArrow;
    private readonly DirectionLayer _windGust;
    private readonly Text _headingText;
    private readonly Text _windText;
    private readonly CanvasGroup _canvasGroup;
    private readonly float _skinScale;

    private int? _shownHeading;
    private int? _shownWind;
    private bool _readoutsVisible = true;

    public CompassUI(CompassSkin? skin)
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

        _cameraWedge = new DirectionLayer(
            CompassLayers.CreateCameraWedge(_panel, skin?.CameraWedge), WedgeAmplitude, WedgePhaseSeed);
        _card = new DirectionLayer(BuildCard(skin, skinned, font), CardAmplitude, CardPhaseSeed);
        _characterArrow = new DirectionLayer(
            CompassLayers.CreateCharacterArrow(_panel, skin?.CharacterArrow),
            ArrowAmplitude,
            ArrowPhaseSeed);
        _windGust = new DirectionLayer(
            CompassLayers.CreateWindGust(_panel, skin?.WindGust), WindAmplitude, 0f);

        const float below = CompassUiFactory.DialSize * 0.5f;
        _headingText = CompassUiFactory.CreateReadout(
            "HeadingText", _panel, new Vector2(0f, -(below + 14f)), font, 14);
        _windText = CompassUiFactory.CreateReadout(
            "WindText", _panel, new Vector2(0f, -(below + 32f)), font, 12);

        SetVisible(false);
    }

    /// <summary>
    /// Builds the card that carries the north/south reference: skin ring when available,
    /// primitive cardinal glyphs otherwise.
    /// </summary>
    private RectTransform BuildCard(CompassSkin? skin, bool skinned, Font font)
    {
        RectTransform card = CompassUiFactory.BuildCard(_panel);
        if (skinned)
        {
            CompassUiFactory.CreateSkinLayer("SkinRing", card, skin!.Ring!);
        }
        else
        {
            CompassLayers.AddCardinals(card, font);
        }

        return card;
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

    /// <summary>
    /// Numeric readouts are diagnostic, not the normal presentation - Rune Compass gives
    /// direction, not instrumentation. Camera heading and wind only; the character-facing
    /// bearing drives the arrow and is deliberately not given a third numeric line.
    /// </summary>
    public void SetReadoutsVisible(bool visible)
    {
        if (_readoutsVisible == visible)
        {
            return;
        }

        _readoutsVisible = visible;
        _headingText.gameObject.SetActive(visible);
        _windText.gameObject.SetActive(visible);
    }

    /// <summary>Points the card at true north, displaced by storm interference.</summary>
    public void SetNorth(float deflection)
    {
        _card.Point(0f, deflection);
    }

    /// <summary>Points the camera wedge at the bearing the view faces.</summary>
    public void SetCameraHeading(float degrees, float deflection)
    {
        _cameraWedge.Point(degrees, deflection);

        int shown = Mathf.RoundToInt(degrees);
        if (_shownHeading == shown)
        {
            return;
        }

        _shownHeading = shown;
        _headingText.text = $"{shown:000}° {Bearing.Cardinal(shown)}";
    }

    /// <summary>Points the character arrow at the bearing the player's body faces.</summary>
    public void SetFacing(float degrees, float deflection)
    {
        _characterArrow.Point(degrees, deflection);
    }

    /// <summary>
    /// Orbits the wind rune to the bearing the wind blows toward. It is handed the same
    /// deflection as every other layer and immune to it by construction, because its
    /// amplitude scale is zero.
    /// </summary>
    public void SetWind(float? degrees, float deflection)
    {
        if (!degrees.HasValue)
        {
            _windGust.SetVisible(false);
            if (_shownWind.HasValue)
            {
                _shownWind = null;
                _windText.text = "Wind unavailable";
            }

            return;
        }

        _windGust.SetVisible(true);
        _windGust.Point(degrees.Value, deflection);

        int shown = Mathf.RoundToInt(degrees.Value);
        if (_shownWind == shown)
        {
            return;
        }

        _shownWind = shown;
        _windText.text = $"Wind → {shown:000}° {Bearing.Cardinal(shown)}";
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(_root);
    }
}
