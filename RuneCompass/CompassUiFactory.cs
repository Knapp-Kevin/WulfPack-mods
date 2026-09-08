using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

internal static class CompassUiFactory
{
    private static readonly Color PanelColor = new(0.055f, 0.045f, 0.035f, 0.78f);
    private static readonly Color LubberColor = new(0.95f, 0.75f, 0.28f, 1f);
    private static readonly Color WindColor = new(0.42f, 0.78f, 1f, 0.95f);
    private static readonly Color CardinalColor = new(0.94f, 0.86f, 0.70f, 1f);

    public static GameObject BuildRoot()
    {
        GameObject root = new("RuneCompassHud");
        Object.DontDestroyOnLoad(root);

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

    public static RectTransform BuildPanel(Transform parent)
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

    public static RectTransform BuildRose(Transform parent)
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

    public static Font CreateFont()
    {
        return Font.CreateDynamicFontFromOSFont("Arial", 24);
    }

    public static void AddCardinals(Transform rose, Font font)
    {
        AddCardinal("N", rose, new Vector2(0f, 72f), font);
        AddCardinal("E", rose, new Vector2(72f, 0f), font);
        AddCardinal("S", rose, new Vector2(0f, -72f), font);
        AddCardinal("W", rose, new Vector2(-72f, 0f), font);
    }

    public static RectTransform CreateWindNeedle(Transform parent)
    {
        return CreateNeedle("WindNeedle", parent, 3f, 44f, WindColor);
    }

    public static void CreateLubber(Transform parent)
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

    public static Text CreateReadout(string name, Transform parent, Vector2 position, Font font, int size)
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
        Text text = CreateReadout("Cardinal" + value, parent, position, font, 22);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.color = CardinalColor;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }
}
