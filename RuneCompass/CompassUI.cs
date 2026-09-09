using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// The north-up compass HUD.
/// </summary>
/// <remarks>
/// North is fixed at 12 o'clock. The card never moves; the indicators do. A prominent
/// centre needle travels to the bearing the player is facing, and the wind pointer travels
/// to the bearing the wind is blowing toward.
///
/// That makes every rotation absolute: each indicator is handed a world bearing and
/// rendered at <see cref="Bearing.BearingRotationZ"/>, with nothing needing to know where
/// the player is looking. Heading and wind are told apart by visual mass and shape rather
/// than colour alone: the solid heading needle dominates the slimmer wind pointer.
///
/// Which layers move is not a skin's choice. A skin supplies artwork and a resting scale.
/// </remarks>
internal sealed class CompassUI : IDisposable
{
    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _dial;
    private readonly RectTransform _headingMarker;
    private readonly RectTransform _windPointer;
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

        // Base first so it renders beneath the card: a Canvas draws in depth-first
        // pre-order, and an object's whole subtree is emitted before its next sibling.
        if (skinned && skin!.Base != null)
        {
            CompassUiFactory.CreateSkinLayer("SkinBase", _panel, skin.Base);
        }

        _dial = CompassUiFactory.BuildDial(_panel);
        Font font = CompassUiFactory.CreateFont();

        _windPointer = skinned
            ? CompassUiFactory.CreateSkinLayer("WindPointer", _panel, skin!.WindPointer!)
            : CompassUiFactory.CreateWindNeedle(_panel);
        _headingMarker = BuildCard(skin, skinned, font);

        // Readouts sit just below the dial, so they follow its size.
        const float below = CompassUiFactory.DialSize * 0.5f;
        _headingText = CompassUiFactory.CreateReadout(
            "HeadingText", _panel, new Vector2(0f, -(below + 14f)), font, 14);
        _windText = CompassUiFactory.CreateReadout(
            "WindText", _panel, new Vector2(0f, -(below + 32f)), font, 12);

        SetVisible(false);
    }

    /// <summary>
    /// Populates the fixed card and returns the marker that travels to the player's
    /// bearing. A skin supplies the ring and heading artwork; primitive geometry remains
    /// the safe fallback when either the skin or its optional heading texture is absent.
    /// </summary>
    private RectTransform BuildCard(CompassSkin? skin, bool skinned, Font font)
    {
        if (!skinned)
        {
            CompassUiFactory.AddCardinals(_dial, font);
        }
        else
        {
            CompassUiFactory.CreateSkinLayer("SkinRing", _dial, skin!.Ring!);
        }

        // Heading is drawn last so its stronger silhouette sits above the wind signal.
        return skinned && skin!.HeadingPointer != null
            ? CompassUiFactory.CreateSkinLayer("HeadingPointer", _panel, skin.HeadingPointer)
            : CompassUiFactory.CreateHeadingNeedle(_panel);
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
    /// Numeric bearing readouts are diagnostic, not the normal presentation — Rune Compass
    /// gives direction, not instrumentation.
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

    public void SetHeading(float degrees)
    {
        _headingMarker.localEulerAngles = new Vector3(0f, 0f, Bearing.BearingRotationZ(degrees));

        int shown = Mathf.RoundToInt(degrees);
        if (_shownHeading == shown)
        {
            return;
        }

        _shownHeading = shown;
        _headingText.text = $"{shown:000}° {Bearing.Cardinal(shown)}";
    }

    public void SetWind(float? degrees)
    {
        if (!degrees.HasValue)
        {
            _windPointer.gameObject.SetActive(false);
            if (_shownWind.HasValue)
            {
                _shownWind = null;
                _windText.text = "Wind unavailable";
            }

            return;
        }

        _windPointer.gameObject.SetActive(true);
        _windPointer.localEulerAngles =
            new Vector3(0f, 0f, Bearing.BearingRotationZ(degrees.Value));

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
