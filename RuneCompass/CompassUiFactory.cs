using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// Generic Unity UI primitives for the compass HUD.
/// </summary>
/// <remarks>
/// Geometry lives here as named constants so the dial can be resized in one place. There
/// is deliberately no opaque backing plate: the compass reads over the world with an
/// outline on each glyph instead, which is far less intrusive than a filled rectangle.
///
/// <para>Anything that carries direction lives in <see cref="CompassLayers"/>. This file
/// was at 226 of the 250-line Razor limit before that split, with no room for the three
/// layers the directional hierarchy needed.</para>
/// </remarks>
internal static class CompassUiFactory
{
    /// <summary>
    /// Dial diameter in reference-resolution pixels. Everything else is a fraction of it,
    /// so resizing the compass is a one-constant change and the unskinned geometry keeps
    /// its proportions instead of being left behind.
    /// </summary>
    public const float DialSize = 204f;

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

    /// <summary>The dial container. Transparent - anchor and position come from config.</summary>
    public static RectTransform BuildPanel(Transform parent)
    {
        GameObject panelObject = CreateUiObject("CompassPanel", parent);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(DialSize, DialSize);
        return panel;
    }

    /// <summary>
    /// The compass card, carrying the north/south reference. It rests pinned to world
    /// north; storm interference is the only thing that ever turns it.
    /// </summary>
    public static RectTransform BuildCard(Transform parent)
    {
        return CreatePivot("CompassCard", parent);
    }

    /// <summary>
    /// A centred, zero-size transform. Rotating one of these swings its children about the
    /// dial centre, which is the mechanism every directional layer is built on.
    /// </summary>
    public static RectTransform CreatePivot(string name, Transform parent)
    {
        GameObject pivotObject = CreateUiObject(name, parent);
        RectTransform pivot = pivotObject.GetComponent<RectTransform>();
        Centre(pivot);
        pivot.sizeDelta = Vector2.zero;
        return pivot;
    }

    public static Font CreateFont()
    {
        return Font.CreateDynamicFontFromOSFont("Arial", 24);
    }

    /// <summary>
    /// A full-dial artwork layer, centred so it rotates about the point the art was drawn
    /// around. Every skin layer shares this, which is what keeps the centred 512x512 canvas
    /// contract true for rotating and static layers alike.
    /// </summary>
    public static RectTransform CreateSkinLayer(
        string name, Transform parent, Sprite sprite, float size = DialSize)
    {
        GameObject layerObject = CreateUiObject(name, parent);
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        Centre(rect);
        rect.sizeDelta = new Vector2(size, size);

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return rect;
    }

    public static Text CreateReadout(
        string name, Transform parent, Vector2 position, Font font, int size)
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

    /// <summary>
    /// A dark outline so glyphs stay readable over bright terrain, given there is no
    /// backing plate behind them.
    /// </summary>
    private static void AddOutline(GameObject target)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = true;
    }

    public static void Centre(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    public static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }
}
