using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// The heading-up compass HUD.
/// </summary>
/// <remarks>
/// Screen-up is always where the player is looking. The rose carries the cardinal
/// letters and rotates to put each on its true bearing; the lubber marker is static at
/// the top of the dial. Heading is applied to the rose and nowhere else, so anything
/// mounted on the rose is placed by pure world bearing.
/// </remarks>
internal sealed class CompassUI : IDisposable
{
    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _rose;
    private readonly RectTransform _windNeedle;
    private readonly Text _headingText;
    private readonly Text _windText;
    private readonly CanvasGroup _canvasGroup;

    private int? _shownHeading;
    private int? _shownWind;

    public CompassUI()
    {
        _root = CompassUiFactory.BuildRoot();
        _canvasGroup = _root.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _panel = CompassUiFactory.BuildPanel(_root.transform);
        _rose = CompassUiFactory.BuildRose(_panel);

        Font font = CompassUiFactory.CreateFont();
        CompassUiFactory.AddCardinals(_rose, font);
        _windNeedle = CompassUiFactory.CreateWindNeedle(_rose);
        CompassUiFactory.CreateLubber(_panel);

        _headingText = CompassUiFactory.CreateReadout(
            "HeadingText",
            _panel,
            new Vector2(0f, -104f),
            font,
            18);
        _windText = CompassUiFactory.CreateReadout(
            "WindText",
            _panel,
            new Vector2(0f, -127f),
            font,
            15);

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public void ApplyLayout(float scale, float opacity, Vector2 offset)
    {
        _panel.localScale = Vector3.one * scale;
        _panel.anchoredPosition = offset;
        _canvasGroup.alpha = opacity;
    }

    public void SetHeading(float degrees)
    {
        _rose.localEulerAngles = new Vector3(0f, 0f, Bearing.RoseRotationZ(degrees));

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
            _windNeedle.gameObject.SetActive(false);
            if (_shownWind.HasValue)
            {
                _shownWind = null;
                _windText.text = "Wind unavailable";
            }

            return;
        }

        _windNeedle.gameObject.SetActive(true);
        _windNeedle.localEulerAngles = new Vector3(0f, 0f, Bearing.WindRotationZ(degrees.Value));

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
