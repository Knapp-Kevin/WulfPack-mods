using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

internal sealed class CompassUI : IDisposable
{
    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _headingNeedle;
    private readonly RectTransform _windNeedle;
    private readonly Text _headingText;
    private readonly Text _windText;
    private readonly CanvasGroup _canvasGroup;

    public CompassUI()
    {
        _root = new GameObject("RuneCompassHud");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        Canvas canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>().enabled = false;
        _canvasGroup = _root.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        GameObject panelObject = CreateUiObject("CompassPanel", _root.transform);
        _panel = panelObject.AddComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.5f, 1f);
        _panel.anchorMax = new Vector2(0.5f, 1f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(190f, 190f);

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.045f, 0.035f, 0.78f);
        background.raycastTarget = false;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        AddCardinal("N", new Vector2(0f, 72f), font);
        AddCardinal("E", new Vector2(72f, 0f), font);
        AddCardinal("S", new Vector2(0f, -72f), font);
        AddCardinal("W", new Vector2(-72f, 0f), font);

        _headingNeedle = CreateNeedle("HeadingNeedle", 5f, 58f, new Color(0.95f, 0.75f, 0.28f, 1f));
        _windNeedle = CreateNeedle("WindNeedle", 3f, 44f, new Color(0.42f, 0.78f, 1f, 0.95f));

        _headingText = CreateText("HeadingText", new Vector2(0f, -104f), font, 18);
        _windText = CreateText("WindText", new Vector2(0f, -127f), font, 15);

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
        _headingNeedle.localEulerAngles = new Vector3(0f, 0f, -degrees);
        _headingText.text = $"{degrees:000}° {Cardinal(degrees)}";
    }

    public void SetWind(float? degrees)
    {
        if (degrees.HasValue)
        {
            _windNeedle.gameObject.SetActive(true);
            _windNeedle.localEulerAngles = new Vector3(0f, 0f, -degrees.Value);
            _windText.text = $"Wind → {degrees.Value:000}° {Cardinal(degrees.Value)}";
        }
        else
        {
            _windNeedle.gameObject.SetActive(false);
            _windText.text = "Wind unavailable";
        }
    }

    public void Dispose()
    {
        if (_root != null)
        {
            UnityEngine.Object.Destroy(_root);
        }
    }

    private RectTransform CreateNeedle(string name, float width, float length, Color color)
    {
        GameObject pivotObject = CreateUiObject(name + "Pivot", _panel);
        RectTransform pivot = pivotObject.AddComponent<RectTransform>();
        pivot.anchorMin = new Vector2(0.5f, 0.5f);
        pivot.anchorMax = new Vector2(0.5f, 0.5f);
        pivot.pivot = new Vector2(0.5f, 0.5f);
        pivot.anchoredPosition = Vector2.zero;
        pivot.sizeDelta = Vector2.zero;

        GameObject needleObject = CreateUiObject(name, pivot);
        RectTransform needle = needleObject.AddComponent<RectTransform>();
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

    private void AddCardinal(string value, Vector2 position, Font font)
    {
        Text text = CreateText("Cardinal" + value, position, font, 22);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.94f, 0.86f, 0.70f, 1f);
    }

    private Text CreateText(string name, Vector2 position, Font font, int size)
    {
        GameObject textObject = CreateUiObject(name, _panel);
        RectTransform rect = textObject.AddComponent<RectTransform>();
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
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static string Cardinal(float degrees)
    {
        string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int index = Mathf.RoundToInt(degrees / 45f) % 8;
        return names[index];
    }
}
