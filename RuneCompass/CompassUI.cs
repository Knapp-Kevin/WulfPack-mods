using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// The heading-up compass HUD.
/// </summary>
/// <remarks>
/// Screen-up is always where the player is looking. The rose rotates to put each cardinal
/// on its true bearing; the lubber marker is static at the top of the dial. Heading is
/// applied to the rose and nowhere else, so anything mounted on the rose is placed by pure
/// world bearing.
///
/// Two presentations share that behaviour. With a skin bound, the ring artwork rides the
/// rose and the wind spear is mounted on it. Without one, primitive glyphs and a bar
/// needle stand in — and those glyphs are counter-rotated to stay upright, because a
/// physical compass card puts its letters upside down on southerly headings.
///
/// Which layers move is not a skin's choice. A skin supplies artwork and a resting scale.
/// </remarks>
internal sealed class CompassUI : IDisposable
{
    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _rose;
    private readonly RectTransform[] _uprightGlyphs;
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

        // Base first so it renders beneath the rose: a Canvas draws in depth-first
        // pre-order, and an object's whole subtree is emitted before its next sibling.
        if (skinned && skin!.Base != null)
        {
            CompassUiFactory.CreateSkinLayer("SkinBase", _panel, skin.Base);
        }

        _rose = CompassUiFactory.BuildRose(_panel);
        Font font = CompassUiFactory.CreateFont();

        _uprightGlyphs = skinned
            ? Array.Empty<RectTransform>()
            : CompassUiFactory.AddCardinals(_rose, font);
        _windPointer = skinned ? BuildSkinnedDial(skin!) : BuildPrimitiveDial();

        _headingText = CompassUiFactory.CreateReadout(
            "HeadingText", _panel, new Vector2(0f, -72f), font, 14);
        _windText = CompassUiFactory.CreateReadout(
            "WindText", _panel, new Vector2(0f, -90f), font, 12);

        SetVisible(false);
    }

    private static CanvasGroup CreateCanvasGroup(GameObject root)
    {
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    /// <summary>Ring on the rose, wind spear on the rose, lubber static. Returns the spear.</summary>
    private RectTransform BuildSkinnedDial(CompassSkin skin)
    {
        CompassUiFactory.CreateSkinLayer("SkinRing", _rose, skin.Ring!);
        RectTransform windPointer =
            CompassUiFactory.CreateSkinLayer("WindPointer", _rose, skin.WindPointer!);
        if (skin.LubberMarker != null)
        {
            CompassUiFactory.CreateSkinLubber(_panel, skin.LubberMarker);
        }

        return windPointer;
    }

    /// <summary>The unskinned stand-in: a bar needle on the rose and a static marker.</summary>
    private RectTransform BuildPrimitiveDial()
    {
        RectTransform needle = CompassUiFactory.CreateWindNeedle(_rose);
        CompassUiFactory.CreateLubber(_panel);
        return needle;
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
        float roseZ = Bearing.RoseRotationZ(degrees);
        _rose.localEulerAngles = new Vector3(0f, 0f, roseZ);

        // Cancel the rose's rotation on any glyph that must stay upright. A skinned ring
        // bakes its letters into the artwork and carries them around instead, so this
        // collection is empty there.
        Vector3 upright = new(0f, 0f, -roseZ);
        foreach (RectTransform glyph in _uprightGlyphs)
        {
            glyph.localEulerAngles = upright;
        }

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
        _windPointer.localEulerAngles = new Vector3(0f, 0f, Bearing.WindRotationZ(degrees.Value));

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
