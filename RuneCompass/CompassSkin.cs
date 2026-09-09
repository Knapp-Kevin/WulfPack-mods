using System;
using System.IO;
using BepInEx.Logging;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// The `skin.json` manifest, as Unity's <see cref="JsonUtility"/> sees it.
/// </summary>
/// <remarks>
/// Field names match the JSON keys exactly. Keys the compass does not consume
/// (<c>description</c>, <c>notes</c>, <c>orientationConventions</c>) are documentation for
/// whoever authors the art and are deliberately absent here; <see cref="JsonUtility"/>
/// ignores what it cannot map.
/// </remarks>
[Serializable]
internal sealed class SkinManifest
{
    public string name = string.Empty;
    public string baseTexture = string.Empty;
    public string ringTexture = string.Empty;
    public string headingPointerTexture = string.Empty;
    public string windPointerTexture = string.Empty;
    public string lubberMarkerTexture = string.Empty;
    public float defaultScale = 1f;
}

/// <summary>
/// A loaded skin: artwork for each layer, plus the scale the art was drawn for.
/// </summary>
/// <remarks>
/// Presentation only. A skin supplies textures and a resting scale; it cannot change which
/// layers rotate. Under north-up, the ring is static while the heading and wind pointers
/// each carry an absolute world bearing. That is a behavioural invariant, not a per-skin
/// choice.
/// </remarks>
internal sealed class CompassSkin
{
    public string Name = string.Empty;
    public float DefaultScale = 1f;
    public Sprite? Base;
    public Sprite? Ring;
    public Sprite? HeadingPointer;
    public Sprite? WindPointer;
    public Sprite? LubberMarker;

    /// <summary>A skin is only usable if it can draw the two layers that carry direction.</summary>
    public bool IsUsable => Ring != null && WindPointer != null;
}

internal static class SkinLoader
{
    /// <summary>
    /// Loads <paramref name="skinName"/> from <c>Assets/Skins</c> beside the plugin DLL.
    /// Returns null on any failure — the caller falls back to the primitive HUD rather
    /// than leaving the player with no compass.
    /// </summary>
    public static CompassSkin? Load(string skinsRoot, string skinName, ManualLogSource log)
    {
        try
        {
            string dir = Path.Combine(skinsRoot, skinName);
            string manifestPath = Path.Combine(dir, "skin.json");
            if (!File.Exists(manifestPath))
            {
                log.LogWarning($"Rune Compass skin '{skinName}' has no skin.json at {dir}.");
                return null;
            }

            SkinManifest manifest = JsonUtility.FromJson<SkinManifest>(File.ReadAllText(manifestPath));
            CompassSkin skin = Build(manifest, dir, skinName, log);

            if (!skin.IsUsable)
            {
                log.LogWarning($"Rune Compass skin '{skinName}' is missing a ring or wind pointer.");
                return null;
            }

            log.LogInfo($"Rune Compass skin loaded: {skin.Name} (defaultScale {skin.DefaultScale}).");
            return skin;
        }
        catch (Exception ex)
        {
            log.LogWarning($"Rune Compass could not load skin '{skinName}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Maps a parsed manifest onto loaded sprites. Split out so <see cref="Load"/> stays
    /// inside the Section 4 line limit as skins gain layers — it was one line short of the
    /// cap when the heading pointer was added.
    /// </summary>
    private static CompassSkin Build(SkinManifest manifest, string dir, string skinName, ManualLogSource log)
    {
        return new CompassSkin
        {
            Name = string.IsNullOrEmpty(manifest.name) ? skinName : manifest.name,
            DefaultScale = manifest.defaultScale > 0f ? manifest.defaultScale : 1f,
            Base = LoadSprite(dir, manifest.baseTexture, log),
            Ring = LoadSprite(dir, manifest.ringTexture, log),
            HeadingPointer = LoadSprite(dir, manifest.headingPointerTexture, log),
            WindPointer = LoadSprite(dir, manifest.windPointerTexture, log),
            LubberMarker = LoadSprite(dir, manifest.lubberMarkerTexture, log),
        };
    }

    private static Sprite? LoadSprite(string dir, string fileName, ManualLogSource log)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        string path = Path.Combine(dir, fileName);
        if (!File.Exists(path))
        {
            log.LogWarning($"Rune Compass skin texture missing: {path}");
            return null;
        }

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
        {
            log.LogWarning($"Rune Compass could not decode {path}.");
            return null;
        }

        // Pivot at the texture centre: every layer shares a centred 512x512 canvas, so both
        // pointers rotate about the same point the art was drawn around.
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
