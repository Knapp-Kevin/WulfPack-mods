using System;
using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.CelestialDial;

internal sealed class InstrumentToggleUI : IDisposable
{
    private readonly GameObject _root;
    private readonly Image _background;

    public InstrumentToggleUI()
    {
        _root = BuildRoot();
        Button button = BuildButton(_root.transform, out _background);
        button.onClick.AddListener(HandleClick);
        SetSelected(false);
        SetVisible(false);
    }

    public event Action? Clicked;

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public void SetSelected(bool selected)
    {
        _background.color = selected
            ? new Color(0.42f, 0.32f, 0.16f, 0.9f)
            : new Color(0.08f, 0.08f, 0.08f, 0.72f);
    }

    public void Dispose()
    {
        Clicked = null;
        UnityEngine.Object.Destroy(_root);
    }

    private void HandleClick()
    {
        Clicked?.Invoke();
    }

    private static GameObject BuildRoot()
    {
        GameObject root = new("WulfPackInstrumentToggle");
        UnityEngine.Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        return root;
    }

    private static Button BuildButton(Transform parent, out Image background)
    {
        GameObject objectButton = new("InstrumentToggle", typeof(RectTransform));
        objectButton.transform.SetParent(parent, false);
        RectTransform rect = objectButton.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.sizeDelta = new Vector2(32f, 32f);
        rect.anchoredPosition = new Vector2(-10f, -10f);

        background = objectButton.AddComponent<Image>();
        Button button = objectButton.AddComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = new("Glyph", typeof(RectTransform));
        labelObject.transform.SetParent(objectButton.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.AddComponent<Text>();
        label.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "↔";
        label.color = Color.white;
        label.raycastTarget = false;
        return button;
    }
}
