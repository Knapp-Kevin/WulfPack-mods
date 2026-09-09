using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.CelestialDial;

internal sealed class DialUI : IDisposable
{
    private const float DialSize = 204f;
    private const float MarkerRadius = 79f;

    private readonly GameObject _root;
    private readonly RectTransform _panel;
    private readonly RectTransform _cycleMarker;
    private readonly Text _dayText;

    public DialUI()
    {
        _root = BuildRoot();
        _panel = BuildPanel(_root.transform);
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 24);

        BuildTicks(_panel);
        AddLabel("SolLabel", _panel, "SÓL", new Vector2(0f, 58f), font, 18);
        AddLabel("ManiLabel", _panel, "MÁNI", new Vector2(0f, -58f), font, 18);
        AddLabel("DawnLabel", _panel, "DAWN", new Vector2(-64f, 0f), font, 10);
        AddLabel("DuskLabel", _panel, "DUSK", new Vector2(64f, 0f), font, 10);

        _dayText = AddLabel("Day", _panel, "DAY --", Vector2.zero, font, 20);
        _cycleMarker = BuildMarker(_panel);
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public void SetState(DayCycleState state)
    {
        _dayText.text = $"DAY {state.Day}";

        float angle = (state.Fraction - 0.5f) * 360f;
        float radians = angle * Mathf.Deg2Rad;
        _cycleMarker.anchoredPosition = new Vector2(
            Mathf.Sin(radians) * MarkerRadius,
            Mathf.Cos(radians) * MarkerRadius);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(_root);
    }

    private static GameObject BuildRoot()
    {
        GameObject root = new("CelestialDialHud");
        UnityEngine.Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 205;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>().enabled = false;
        return root;
    }

    private static RectTransform BuildPanel(Transform parent)
    {
        GameObject panelObject = CreateUiObject("CelestialDialPanel", parent);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.one;
        panel.anchorMax = Vector2.one;
        panel.pivot = Vector2.one;
        panel.sizeDelta = new Vector2(DialSize, DialSize);
        panel.anchoredPosition = new Vector2(-18f, -18f);
        return panel;
    }

    private static void BuildTicks(RectTransform panel)
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f;
            float radians = angle * Mathf.Deg2Rad;
            GameObject tickObject = CreateUiObject($"Tick{i:00}", panel);
            RectTransform tick = tickObject.GetComponent<RectTransform>();
            Centre(tick);
            tick.sizeDelta = new Vector2(i % 6 == 0 ? 3f : 2f, i % 6 == 0 ? 14f : 9f);
            tick.anchoredPosition = new Vector2(
                Mathf.Sin(radians) * 82f,
                Mathf.Cos(radians) * 82f);
            tick.localRotation = Quaternion.Euler(0f, 0f, -angle);

            Image image = tickObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.9f, 0.82f, 0.85f);
            image.raycastTarget = false;
        }
    }

    private static RectTransform BuildMarker(Transform parent)
    {
        GameObject markerObject = CreateUiObject("CycleMarker", parent);
        RectTransform marker = markerObject.GetComponent<RectTransform>();
        Centre(marker);
        marker.sizeDelta = new Vector2(12f, 12f);
        marker.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image image = markerObject.AddComponent<Image>();
        image.color = new Color(1f, 0.78f, 0.28f, 1f);
        image.raycastTarget = false;
        return marker;
    }

    private static Text AddLabel(
        string name,
        Transform parent,
        string value,
        Vector2 position,
        Font font,
        int size)
    {
        GameObject textObject = CreateUiObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Centre(rect);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(120f, 28f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = true;
        return text;
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
