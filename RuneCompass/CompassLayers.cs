using UnityEngine;
using UnityEngine.UI;

namespace WulfPack.RuneCompass;

/// <summary>
/// Builds the compass's four directional indicators.
/// </summary>
/// <remarks>
/// Split out of <see cref="CompassUiFactory"/>, which owns the generic Unity UI primitives.
/// This file owns the things that carry direction; that one owns the things that do not.
///
/// <para>Every layer is built on a centred zero-size pivot and turned by rotating that
/// pivot. That single mechanism is what lets the wind rune orbit <i>outside</i> the rim
/// through the same transform every other layer uses, and it is why a skin's artwork stays
/// on a centred 512x512 canvas no matter where on the dial the glyph is drawn.</para>
/// </remarks>
internal static class CompassLayers
{
    private const float DialSize = CompassUiFactory.DialSize;
    private const float CardinalRadius = DialSize * 0.375f;
    private const int CardinalFontSize = (int)(DialSize * 0.125f);

    private const float ArrowWidth = DialSize * 0.038f;
    private const float ArrowLength = DialSize * 0.42f;
    private const float WedgeWidth = DialSize * 0.014f;
    private const float WedgeLength = DialSize * 0.43f;
    private const float WedgeHalfAngle = 30f;
    private const float GustSize = DialSize * 0.13f;
    private const float GustOrbitRadius = DialSize * 0.56f;

    private static readonly Color ArrowColor = new(0.95f, 0.46f, 0.20f, 1f);
    private static readonly Color WedgeColor = new(0.62f, 0.78f, 0.92f, 0.30f);
    private static readonly Color GustColor = new(0.72f, 0.88f, 0.98f, 0.85f);
    private static readonly Color CardinalColor = new(0.96f, 0.91f, 0.80f, 1f);

    /// <summary>
    /// The camera-view layer: where the player is looking, kept deliberately quiet.
    /// </summary>
    /// <remarks>
    /// With art this is a filled sector. Without art it is two thin ticks at the sector's
    /// edges, because a filled sector is not constructible from what this mod has: every
    /// primitive here is a rectangular <see cref="Image"/>, and an <see cref="Image"/> with
    /// no sprite renders a quad, so a radial fill would produce a rotating rectangle rather
    /// than a disc sector. Two ticks read as a bracketed sector and cost fifteen lines;
    /// generating a wedge texture would cost forty and buy a background element nobody is
    /// meant to look at directly.
    /// </remarks>
    public static RectTransform CreateCameraWedge(Transform parent, Sprite? art)
    {
        if (art != null)
        {
            return CompassUiFactory.CreateSkinLayer("CameraWedge", parent, art, DialSize * 0.86f);
        }

        RectTransform pivot = CompassUiFactory.CreatePivot("CameraWedgePivot", parent);
        AddWedgeTick(pivot, -WedgeHalfAngle);
        AddWedgeTick(pivot, WedgeHalfAngle);
        return pivot;
    }

    /// <summary>
    /// The character-facing arrow: the compass's primary read, and the only layer meant to
    /// draw the eye. Mounted at the centre and the longest thing on the dial.
    /// </summary>
    public static RectTransform CreateCharacterArrow(Transform parent, Sprite? art)
    {
        return art != null
            ? CompassUiFactory.CreateSkinLayer("CharacterArrow", parent, art, DialSize)
            : CreateNeedle("CharacterArrow", parent, ArrowWidth, ArrowLength, ArrowColor);
    }

    /// <summary>
    /// The wind rune: a small glyph orbiting outside the rim at the bearing the wind blows
    /// toward. Deliberately about a third the arrow's size, so wind never competes with
    /// facing for attention.
    /// </summary>
    /// <remarks>
    /// Offsetting the glyph along local up and rotating the pivot is what makes it orbit.
    /// A skin's 512x512 canvas is drawn around its own centre and mounts at the offset, so
    /// art needs no knowledge of the orbit radius.
    /// </remarks>
    public static RectTransform CreateWindGust(Transform parent, Sprite? art)
    {
        RectTransform pivot = CompassUiFactory.CreatePivot("WindGustPivot", parent);
        GameObject glyph = CompassUiFactory.CreateUiObject("WindGust", pivot);
        RectTransform rect = glyph.GetComponent<RectTransform>();
        CompassUiFactory.Centre(rect);
        rect.anchoredPosition = new Vector2(0f, GustOrbitRadius);
        rect.sizeDelta = new Vector2(GustSize, GustSize);

        Image image = glyph.AddComponent<Image>();
        image.raycastTarget = false;
        if (art != null)
        {
            image.sprite = art;
            image.preserveAspect = true;
        }
        else
        {
            image.color = GustColor;
        }

        return pivot;
    }

    /// <summary>
    /// The four cardinal glyphs for the unskinned card. A skin bakes these into its ring.
    /// </summary>
    public static void AddCardinals(Transform card, Font font)
    {
        AddCardinal("N", card, new Vector2(0f, CardinalRadius), font);
        AddCardinal("E", card, new Vector2(CardinalRadius, 0f), font);
        AddCardinal("S", card, new Vector2(0f, -CardinalRadius), font);
        AddCardinal("W", card, new Vector2(-CardinalRadius, 0f), font);
    }

    /// <summary>A centre-mounted needle on its own pivot, so rotating it swings about the dial centre.</summary>
    public static RectTransform CreateNeedle(
        string name, Transform parent, float width, float length, Color color)
    {
        RectTransform pivot = CompassUiFactory.CreatePivot(name + "Pivot", parent);
        AddBlade(pivot, name, Vector2.zero, width, length, color);
        return pivot;
    }

    /// <summary>One edge tick of the camera sector, rotated to its own edge bearing.</summary>
    private static void AddWedgeTick(RectTransform pivot, float offsetDegrees)
    {
        RectTransform blade = AddBlade(
            pivot, "WedgeTick", Vector2.zero, WedgeWidth, WedgeLength, WedgeColor);
        blade.localEulerAngles = new Vector3(0f, 0f, -offsetDegrees);
    }

    private static RectTransform AddBlade(
        Transform parent, string name, Vector2 position, float width, float length, Color color)
    {
        GameObject bladeObject = CompassUiFactory.CreateUiObject(name, parent);
        RectTransform blade = bladeObject.GetComponent<RectTransform>();
        blade.anchorMin = new Vector2(0.5f, 0.5f);
        blade.anchorMax = new Vector2(0.5f, 0.5f);
        blade.pivot = new Vector2(0.5f, 0f);
        blade.anchoredPosition = position;
        blade.sizeDelta = new Vector2(width, length);

        Image image = bladeObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return blade;
    }

    private static void AddCardinal(string value, Transform parent, Vector2 position, Font font)
    {
        Text text = CompassUiFactory.CreateReadout(
            "Cardinal" + value, parent, position, font, CardinalFontSize);
        text.text = value;
        text.fontStyle = FontStyle.Bold;
        text.color = CardinalColor;
        text.rectTransform.sizeDelta = new Vector2(30f, 22f);
    }
}
