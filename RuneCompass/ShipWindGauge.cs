namespace WulfPack.RuneCompass;

/// <summary>
/// Suppresses Valheim's own ship wind gauge while Rune Compass is showing wind, so a boat
/// does not carry two wind readouts in two different conventions a few centimetres apart.
/// </summary>
/// <remarks>
/// <b>This is the only vanilla state Rune Compass touches</b>, and it is therefore the only
/// place the mod owes a restore. The rest of the HUD is built on its own Canvas and can be
/// destroyed without trace; this cannot.
///
/// <para>No Harmony patch is involved. <c>Hud.m_shipWindIndicatorRoot</c> is a public
/// <c>RectTransform</c> on a public singleton, so suppression is a <c>SetActive</c> call.
/// Keeping the mod free of patches is worth something: a prefix or transpiler on
/// <c>Hud.UpdateShipHud</c> would be a far larger commitment than this, and would collide
/// with any other mod doing the same.</para>
///
/// <para><b>Restores to the state it found, not to visible.</b> If another mod had already
/// hidden the gauge, forcing it back on would be this mod overriding a decision that was not
/// its own — the same reasoning that makes the build script's delete scope name-confirmed
/// rather than wildcarded.</para>
///
/// <para><c>Hud.UpdateShipHud</c> re-activates the root every frame while aboard, so the hide
/// has to be reasserted rather than done once. That also makes restoring cheap: the game puts
/// the gauge back by itself the moment this stops suppressing it.</para>
/// </remarks>
internal sealed class ShipWindGauge
{
    private bool _originalActive;
    private bool _captured;

    /// <summary>Hides the gauge, remembering what it looked like the first time.</summary>
    public void Hide()
    {
        UnityEngine.RectTransform? root = Root();
        if (root == null)
        {
            return;
        }

        if (!_captured)
        {
            _captured = true;
            _originalActive = root.gameObject.activeSelf;
        }

        if (root.gameObject.activeSelf)
        {
            root.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Puts the gauge back exactly as it was found, and forgets it — so a later session
    /// captures afresh rather than restoring a stale value.
    /// </summary>
    public void Restore()
    {
        if (!_captured)
        {
            return;
        }

        UnityEngine.RectTransform? root = Root();
        if (root != null && root.gameObject.activeSelf != _originalActive)
        {
            root.gameObject.SetActive(_originalActive);
        }

        _captured = false;
    }

    /// <summary>
    /// The gauge, or null when there is no HUD yet — which is most of the main menu, and
    /// every frame before a world finishes loading.
    /// </summary>
    private static UnityEngine.RectTransform? Root()
    {
        Hud? hud = Hud.instance;
        return hud == null ? null : hud.m_shipWindIndicatorRoot;
    }
}
