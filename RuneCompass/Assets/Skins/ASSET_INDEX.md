# Rune Compass skin asset index

This index tracks the artwork the current Rune Compass runtime actually consumes. It is intentionally based on the wired `CompassSkin` contract rather than older concept terminology.

## Current runtime slots

Each shipping skin may provide these presentation-only textures:

| Slot | File/key | Motion and meaning | Current state |
| --- | --- | --- | --- |
| Base | `base.png` / `baseTexture` | static back plate | present in all three prepared skins |
| Ring | `ring.png` / `ringTexture` | fixed north-up card; required for a skin to be usable | present in all three prepared skins |
| Camera wedge | `camera_wedge.png` / `cameraWedgeTexture` | semi-transparent sector showing camera view; rotates to camera bearing | **missing in all three prepared skins** |
| Character arrow | `character_arrow.png` / `characterArrowTexture` | primary center-mounted needle showing character facing | **missing in all three prepared skins** |
| Wind gust | `wind_gust.png` / `windGustTexture` | small external rune/gust marking the quarter the wind comes from | **missing in all three prepared skins** |

The loader still understands `windPointerTexture` and `lubberMarkerTexture` because older skin bundles contain those files, but those visual roles are superseded by the current north-up hierarchy. New art work should target the three missing slots above, not revive the old center wind pointer or lubber-marker model.

## Prepared skins

| Skin | Base | Ring | Camera wedge | Character arrow | Wind gust |
| --- | --- | --- | --- | --- | --- |
| `ClassicWood` | yes | yes | **missing** | **missing** | **missing** |
| `RuneRing` | yes | yes | **missing** | **missing** | **missing** |
| `MinimalNordic` | yes | yes | **missing** | **missing** | **missing** |

That is the nine-asset gap reported by the current status: three missing assets across each of three skins.

## Generation strategy

Generate and review one skin at a time so visual mistakes do not get multiplied across nine files.

Recommended order:

1. `ClassicWood/camera_wedge.png`
2. `ClassicWood/character_arrow.png`
3. `ClassicWood/wind_gust.png`
4. bind and validate the complete Classic Wood skin in-game;
5. repeat the same three slots for `RuneRing`;
6. repeat the same three slots for `MinimalNordic`.

Do not generate all nine in one pass.

## Camera wedge contract

The camera wedge is specifically a HUD overlay, not a physical compass component.

- centered on the same 512×512 canvas as the other layers;
- points to 12 o'clock at bearing 0;
- broad enough to communicate camera view, but visually quieter than the character arrow;
- predominantly transparent with a semi-transparent fill and restrained edge treatment;
- no wood slab, forged-metal slice, or other solid object appearance;
- must remain legible over the dial without obscuring the ring.

## Character arrow contract

- center-mounted;
- points to 12 o'clock at bearing 0;
- longest and strongest directional element on the dial;
- visually unmistakable from the camera sector and wind rune;
- symmetric enough to rotate cleanly through every bearing.

## Wind gust contract

- small presentation glyph, roughly one-third the visual weight of the character arrow;
- represents the quarter the wind comes **from**;
- rendered outside the rim by runtime layout;
- must read as wind/gust rather than a second navigation needle;
- unaffected by storm interference.

## Persistent toggle

The planned persistent map/instrument toggle is a separate HUD affordance and is **not currently part of `CompassSkin`**. Do not pretend it is already wired by adding a `toggleTexture` key to skin manifests. Its artwork and runtime contract should be added together when the toggle implementation begins.

See [../../CONCEPT.md](../../CONCEPT.md) for the product/visual contract and [README.md](README.md) for the broader skin rules.