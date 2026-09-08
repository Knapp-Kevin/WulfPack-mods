using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// Builds the compass HUD's Unity UI objects.
/// </summary>
/// <remarks>
/// Geometry lives here as named constants so the dial can be resized in one place. There
/// is deliberately no opaque backing plate: the compass reads over the world with an
/// outline on each glyph instead, which is far less intrusive than a filled rectangle and
/// is what the placeholder art is meant to evolve into.
/// </remarks>
internal static class CompassUiFactory
{
    // Dial geometry, in reference-resolution pixels.
    private const float DialSize = 120f;
    private const float CardinalRadius = 45f;
    private const int CardinalFontSize = 15;
    private const float NeedleWidth = 2.5f;
    private const float NeedleLength = 30f;
    private const float LubberRadius = 56f;
    private const float LubberWidth = 7f;
    private const float LubberHeight = 11f;

    private static readonly Color LubberColor = new(0.95f, 0.75f, 0.28f, 1f);
    private static readonly Color WindColor = new(0.42f, 0.78f, 1f, 0.95f);
    private static readonly Color CardinalColor = new(0.96f, 0.91f, 0.80f, 1f);
    private static readonly Color OutlineColor = new(0f, 0f, 0f, 0.85f);

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

    /// <summary>The dial container. Transparent — anchor and position come from config.</summary>
    public static RectTransform BuildPanel(Transform parent)
    {
        GameObject panelObject = CreateUiObject("CompassPanel", parent);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(DialSize, DialSize);
        return panel;
    }

    public static RectTransform BuildRose(Transform parent)
    {
        GameObject roseObject = CreateUiObject("CompassRose", parent);
        RectTransform rose = roseObject.GetComponent<RectTransform>();
        Centre(rose);
        rose.sizeDelta = Vector2.zero;
        return rose;
    }

    public static Font CreateFont()
    {
        return Font.CreateDynamicFontFromOSFont("Arial", 24);
    }

    /// <summary>
    /// The four cardinal glyphs, returned so the caller can keep them upright while the
    /// rose beneath them rotates.
    /// </summary>
    public static RectTransform[] AddCardinals(Transform rose, Font font)
    {
        return new[]
        {
            AddCardinal("N", rose, new Vector2(0f, CardinalRadius), font),
            AddCardinal("E", rose, new Vector2(CardinalRadius, 0f), font),
            AddCardinal("S", rose, new Vector2(0f, -CardinalRadius), font),
            AddCardinal("W", rose, new Vector2(-CardinalRadius, 0f), font),
        };
    }

    public static RectTransform CreateWindNeedle(Transform parent)
    {
        return CreateNeedle("WindNeedle", parent, NeedleWidth, NeedleLength, WindColor);
    }

    /// <summary>The static "you are looking this way" marker at the top of the dial.</summary>
    public static void CreateLubber(Transform parent)
    {
        GameObject markerObject = CreateUiObject("LubberMarker", parent);
        RectTransform marker = markerObject.GetComponent<RectTransform>();
        Centre(marker);
        marker.anchoredPosition = new Vector2(0f, LubberRadius);
        marker.sizeDelta = new Vector2(LubberWidth, LubberHeight);

        Image image = markerObject.AddComponent<Image>();
        image.color = LubberColor;
        image.raycastTarget = false;
    }

    /// <summary>
    /// A full-dial artwork layer, centred so it rotates about the point the art was drawn
    /// around. Used for the skin's base, ring and wind pointer.
    /// </summary>
    public static RectTransform CreateSkinLayer(string name, Transform parent, Sprite sprite)
    {
        GameObject layerObject = CreateUiObject(name, parent);
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        Centre(rect);
        rect.sizeDelta = new Vector2(DialSize, DialSize);

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return rect;
    }

    /// <summary>
    /// The skin's static facing marker. Drawn at the top of the dial on the same centred
    /// canvas as every other layer, so it needs no separate placement.
    /// </summary>
    public static void CreateSkinLubber(Transform parent, Sprite sprite)
    {
        CreateSkinLayer("LubberMarker", parent, sprite);
    }

    public static Text CreateReadout(string name, Transform parent, Vector2 position, Font font, int size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Centre(rect);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 28f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        AddOutline(textObject);
        return text;
    }

    private static RectTransform CreateNeedle(string name, Transform parent, float width, float length, Color color)
    {
        GameObject pivotObject = CreateUiObject(name + "Pivot", parent);
        RectTransform pivot = pivotObject.GetComponent<RectTransform>();
        Centre(pivot);
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

    private static RectTransform AddCardinal(string value, Transform parent, Vector2 position, Font font)
    {
        Text text = CreateReadout("Cardinal" + value, parent, position, font, CardinalFontSize);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.color = CardinalColor;
        text.rectTransform.sizeDelta = new Vector2(30f, 22f);
        return text.rectTransform;
    }

    /// <summary>
    /// A dark outline so glyphs stay readable over bright terrain now that there is no
    /// backing plate behind them.
    /// </summary>
    private static void AddOutline(GameObject target)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = true;
    }

    private static void Centre(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }
}
