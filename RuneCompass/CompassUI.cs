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
    private static readonly Color PanelColor = new(0.055f, 0.045f, 0.035f, 0.78f);
    private static readonly Color LubberColor = new(0.95f, 0.75f, 0.28f, 1f);
    private static readonly Color WindColor = new(0.42f, 0.78f, 1f, 0.95f);
    private static readonly Color CardinalColor = new(0.94f, 0.86f, 0.70f, 1f);

    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _rose;
    private readonly RectTransform _windNeedle;
    private readonly Text _headingText;
    private readonly Text _windText;
    private readonly CanvasGroup _canvasGroup;

    // Rendered-value caches. Nullable so "wind unavailable" is distinguishable from a
    // cached bearing; without that, losing and regaining wind at the same rounded
    // bearing would leave the stale string on screen.
    private int? _shownHeading;
    private int? _shownWind;

    public CompassUI()
    {
        _root = BuildRoot();
        _canvasGroup = _root.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _panel = BuildPanel(_root.transform);
        _rose = BuildRose(_panel);

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        AddCardinal("N", _rose, new Vector2(0f, 72f), font);
        AddCardinal("E", _rose, new Vector2(72f, 0f), font);
        AddCardinal("S", _rose, new Vector2(0f, -72f), font);
        AddCardinal("W", _rose, new Vector2(-72f, 0f), font);

        _windNeedle = CreateNeedle("WindNeedle", _rose, 3f, 44f, WindColor);
        CreateLubber(_panel);

        _headingText = CreateText("HeadingText", _panel, new Vector2(0f, -104f), font, 18);
        _windText = CreateText("WindText", _panel, new Vector2(0f, -127f), font, 15);

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

    private static GameObject BuildRoot()
    {
        GameObject root = new("RuneCompassHud");
        UnityEngine.Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>().enabled = false;
        return root;
    }

    private static RectTransform BuildPanel(Transform parent)
    {
        GameObject panelObject = CreateUiObject("CompassPanel", parent);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(190f, 190f);

        Image background = panelObject.AddComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;
        return panel;
    }

    /// <summary>
    /// The rotating compass card.
    /// </summary>
    /// <remarks>
    /// Anchors, pivot and offset are all set explicitly. A default <see cref="RectTransform"/>
    /// anchors to the parent's bottom-left, which would make the card orbit the panel's
    /// corner instead of spinning about its centre.
    /// </remarks>
    private static RectTransform BuildRose(Transform parent)
    {
        GameObject roseObject = CreateUiObject("CompassRose", parent);
        RectTransform rose = roseObject.GetComponent<RectTransform>();
        rose.anchorMin = new Vector2(0.5f, 0.5f);
        rose.anchorMax = new Vector2(0.5f, 0.5f);
        rose.pivot = new Vector2(0.5f, 0.5f);
        rose.anchoredPosition = Vector2.zero;
        rose.sizeDelta = Vector2.zero;
        return rose;
    }

    /// <summary>The static "you are looking this way" marker at the top of the dial.</summary>
    private static void CreateLubber(Transform parent)
    {
        GameObject markerObject = CreateUiObject("LubberMarker", parent);
        RectTransform marker = markerObject.GetComponent<RectTransform>();
        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.pivot = new Vector2(0.5f, 0.5f);
        marker.anchoredPosition = new Vector2(0f, 88f);
        marker.sizeDelta = new Vector2(9f, 14f);

        Image image = markerObject.AddComponent<Image>();
        image.color = LubberColor;
        image.raycastTarget = false;
    }

    private static RectTransform CreateNeedle(string name, Transform parent, float width, float length, Color color)
    {
        GameObject pivotObject = CreateUiObject(name + "Pivot", parent);
        RectTransform pivot = pivotObject.GetComponent<RectTransform>();
        pivot.anchorMin = new Vector2(0.5f, 0.5f);
        pivot.anchorMax = new Vector2(0.5f, 0.5f);
        pivot.pivot = new Vector2(0.5f, 0.5f);
        pivot.anchoredPosition = Vector2.zero;
        pivot.sizeDelta = Vector2.zero;

        GameObject needleObject = CreateUiObject(name, pivot);
        RectTransform needle = needleObject.GetComponent<RectTransform>();
        needle.anchorMin = new Vector2(0.5f, 0.5f);
        needle.anchorMax = new Vector2(0.5f, 0.5f);
        needle.pivot = new Vector2(0.5f, 0f);
        needle.anchoredPosition = Vector2.zero;
        needle.sizeDelta = new Vector2(width, length);

        Image image = needleObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return pivot;
    }

    private static void AddCardinal(string value, Transform parent, Vector2 position, Font font)
    {
        Text text = CreateText("Cardinal" + value, parent, position, font, 22);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.color = CardinalColor;
    }

    private static Text CreateText(string name, Transform parent, Vector2 position, Font font, int size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 28f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }
}
