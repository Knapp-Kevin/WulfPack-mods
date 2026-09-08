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
    /// <summary>
    /// Dial diameter in reference-resolution pixels. Everything else is a fraction of it,
    /// so resizing the compass is a one-constant change and the unskinned geometry keeps
    /// its proportions instead of being left behind.
    /// </summary>
    public const float DialSize = 204f;

    private const float CardinalRadius = DialSize * 0.375f;
    private const int CardinalFontSize = (int)(DialSize * 0.125f);
    private const float NeedleWidth = DialSize * 0.021f;
    private const float NeedleLength = DialSize * 0.25f;
    private const float LubberRadius = DialSize * 0.467f;
    private const float LubberWidth = DialSize * 0.058f;
    private const float LubberHeight = DialSize * 0.092f;

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

    /// <summary>
    /// The compass card. Static under north-up: N stays at 12 o'clock and the indicators
    /// move instead, so this carries no rotation at all.
    /// </summary>
    public static RectTransform BuildDial(Transform parent)
    {
        GameObject dialObject = CreateUiObject("CompassDial", parent);
        RectTransform dial = dialObject.GetComponent<RectTransform>();
        Centre(dial);
        dial.sizeDelta = Vector2.zero;
        return dial;
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

    /// <summary>
    /// The unskinned heading marker: a wedge riding the rim at the bearing the player
    /// faces. Returned as a centred pivot so rotating it carries the wedge around the rim.
    /// </summary>
    public static RectTransform CreateHeadingMarker(Transform parent)
    {
        GameObject pivotObject = CreateUiObject("HeadingMarkerPivot", parent);
        RectTransform pivot = pivotObject.GetComponent<RectTransform>();
        Centre(pivot);
        pivot.sizeDelta = Vector2.zero;

        GameObject markerObject = CreateUiObject("HeadingMarker", pivot);
        RectTransform marker = markerObject.GetComponent<RectTransform>();
        Centre(marker);
        marker.anchoredPosition = new Vector2(0f, LubberRadius);
        marker.sizeDelta = new Vector2(LubberWidth, LubberHeight);

        Image image = markerObject.AddComponent<Image>();
        image.color = LubberColor;
        image.raycastTarget = false;
        return pivot;
    }

    /// <summary>
    /// A full-dial artwork layer, centred so it rotates about the point the art was drawn
    /// around. Used for every skin layer: the base and ring never rotate, while the
    /// heading marker and wind pointer are handed a world bearing each frame.
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
