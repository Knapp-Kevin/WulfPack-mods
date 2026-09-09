# Rune Compass

Rune Compass is a lightweight, immersive navigation aid for Valheim No Map play.

> Rune Compass gives you direction, not hidden information.

**Current state:** the runtime behavior is operator-accepted on Valheim 1.0 and all 13 storm/directional protocol rows pass. The remaining visual gap is **nine skin assets**: camera wedge, character arrow, and wind gust for each of the three prepared skins.

See [CONCEPT.md](CONCEPT.md) for the product/visual contract, [Assets/Skins/ASSET_INDEX.md](Assets/Skins/ASSET_INDEX.md) for the exact missing artwork, and [STATUS.md](STATUS.md) for validation evidence.

## Orientation model

Rune Compass is **north-up**.

- `N` stays fixed at 12 o'clock.
- The dial face never rotates, including during storms.
- The character-facing arrow is the compass's primary needle.
- The camera view is a quiet sector behind that arrow.
- The wind rune sits outside the rim on the quarter the wind comes **from**.

The information hierarchy is deliberately positional and size-based rather than color-only:

| Signal | Presentation | Priority |
| --- | --- | --- |
| Character facing | bold center-mounted arrow | primary |
| North/cardinals | fixed on the card | reference |
| Camera view | faint semi-transparent sector | tertiary |
| Wind source | small external rune/gust | secondary but visually subordinate |

Your body's facing and your camera's view are separate because Valheim allows them to differ. Orbit the camera while standing still and the camera sector moves while the character arrow holds.

## Wind semantics

Valheim's `EnvMan.GetWindDir()` reports the direction wind travels **toward**. Rune Compass deliberately draws its external wind rune on the reciprocal quarter, showing where the wind comes **from**, which reads naturally as a position on a compass rim.

If you compare it with the vanilla minimap wind marker, expect them to sit opposite each other. They are driven by the same wind vector but visualize opposite ends of that vector by design.

## Storm interference

Rune Compass behaves like an instrument being disturbed by severe weather rather than a HUD that simply switches off.

During configured storm environments and the configured storm-biome mask:

- the fixed card remains still;
- the character arrow and camera sector are progressively captured by a storm field;
- turning the player cannot recover true direction at full capture;
- interference arrives and releases smoothly rather than snapping;
- the wind rune remains truthful because wind is directly observable in the world.

Valheim exposes no authoritative general "is storming" flag. Rune Compass therefore uses verified environment names plus the explicit biome mask. The default/environment discovery behavior is documented in [STATUS.md](STATUS.md).

## No Map placement and instrument interop

Rune Compass is anchored in the top-right HUD region where the minimap normally lives.

The shared persistent instrument toggle is owned by Celestial Dial as a standalone HUD affordance, not by Rune Compass and not by the minimap object. Rune Compass now exposes a deliberately tiny optional presentation bridge, `RuneCompassInterop.SetExternalSuppressed(bool)`, so Celestial Dial can hide the compass while its own surface is selected and release it afterward.

The bridge changes presentation only:

- it does not change Rune Compass configuration;
- it does not change heading, wind, storm, skin, or gameplay state;
- Rune Compass remains independently installable and usable when Celestial Dial is absent;
- when external suppression is released, Rune Compass returns to its own normal visibility rules;
- suppression calls `Hide()`, so the ship wind gauge is restored and storm interference resets rather than remaining half-captured off screen.

The shared toggle candidate is implemented in the repository, but the Rune Compass ↔ Celestial Dial switching path still requires local Valheim 1.0 acceptance. Celestial Dial's [toggle protocol](../CelestialDial/TOGGLE_TEST.md) owns that evidence.

## Skin system

Skins are presentation-only. They may change materials and ornament, but they cannot change navigation semantics, storm behavior, wind meaning, or gameplay state.

Prepared families:

- **Classic Wood**
- **Rune Ring**
- **Minimal Nordic**

Each currently has a working `base.png`, `ring.png`, and `skin.json`. Older `wind_pointer.png` and `lubber_marker.png` files remain in the bundles from the superseded visual hierarchy.

The current runtime additionally supports three newer slots:

- `cameraWedgeTexture`
- `characterArrowTexture`
- `windGustTexture`

Those three textures are missing from all three prepared skins, producing the current nine-asset visual backlog. Missing optional textures fall back to primitive geometry, so the compass remains functional while art is incomplete.

See [Assets/Skins/ASSET_INDEX.md](Assets/Skins/ASSET_INDEX.md) before generating anything. The intended workflow is one skin at a time: complete and validate Classic Wood first, then propagate the approved component roles to Rune Ring and Minimal Nordic.

## Camera wedge requirement

The camera wedge is specifically a **semi-transparent HUD sector**, not a solid physical slice of the compass.

It should:

- originate at the dial center;
- rotate to the camera bearing;
- sit visually behind the character arrow;
- use a restrained translucent fill and subtle edge treatment;
- remain substantially quieter than the primary needle;
- avoid wood, metal, or other heavy physical construction that makes it read as a cut-out section of the instrument.

## Configuration

BepInEx writes the configuration to:

```text
BepInEx/config/com.wulfpack.runecompass.cfg
```

The current implementation exposes controls for enable state, No Map visibility, HUD anchor/offset/scale/opacity, optional numeric readouts, wind presentation, and storm-interference behavior. Configuration is reloadable in a live world through the mod's config watcher.

## Local build and validation

From the repository root:

```powershell
.\RuneCompass\build-local.ps1
.\RuneCompass\verify-local.ps1
.\RuneCompass\build-local.ps1 -Install
```

Lifecycle commands:

```powershell
.\RuneCompass\build-local.ps1 -Status
.\RuneCompass\build-local.ps1 -Disable
.\RuneCompass\build-local.ps1 -Enable
.\RuneCompass\build-local.ps1 -Uninstall
```

The current evidence includes a clean build, local bearing/interference assertions, lifecycle containment, live rendering, and all 13 operator protocol rows passing. The new optional instrument bridge is not included in that historical acceptance and must pass the Celestial Dial toggle protocol before being called accepted. Details are recorded in [STATUS.md](STATUS.md).

## Explicit non-goals

Rune Compass does not provide:

- minimap tiles or map replacement;
- pins, route guidance, home bearing, trader/boss/player tracking, or hidden-location discovery;
- progression gating;
- a physical equippable compass item;
- server authority or save/world persistence;
- skin-specific gameplay behavior.

GitHub Actions are prohibited in WulfPack Mods. Rune Compass is built and validated locally.
