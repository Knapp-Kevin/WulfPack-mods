using System;
using UnityEngine;

namespace WulfPack.RuneCompass;

/// <summary>
/// Live accessors for every configurable value the compass reads.
/// </summary>
/// <remarks>
/// Each member is a getter rather than a value so the compass always sees the current
/// config, including edits applied at runtime by <see cref="ConfigWatcher"/>.
///
/// This exists because the controller had grown to eight constructor parameters and the
/// skin cycle will add more. Bundling them keeps the call site readable and makes adding
/// a setting a one-line change instead of threading another parameter through two classes.
/// Defaults match the shipped config defaults, so a partially-populated instance still
/// behaves sensibly.
/// </remarks>
internal sealed class CompassSettings
{
    public Func<bool> Enabled = () => true;
    public Func<bool> OnlyInNoMap = () => true;
    public Func<HudAnchor> Anchor = () => HudAnchor.TopRight;
    public Func<float> Scale = () => 1f;
    public Func<float> Opacity = () => 0.9f;
    public Func<Vector2> Offset = () => Vector2.zero;
    public Func<bool> ShowReadouts = () => false;
    public Func<float> HeadingOffset = () => 0f;
}
