# Rested Whispers

A small, client-side Valheim quality-of-life mod that gently warns you as the existing Rested effect begins to fade.

Rested Whispers does not add a Tired status effect, change player stats, alter the Rested mechanic, modify saves, or synchronize new gameplay state. It only observes the existing Rested effect and displays native Valheim notifications.

## Default notifications

- 2 minutes remaining: `You're getting tired.`
- 30 seconds remaining: `You long for the warmth of a fire.`
- Rested expires: `You feel weary.`

## Configuration

BepInEx configuration exposes:

- `Enabled`
- `FirstWarningSeconds`
- `FinalWarningSeconds`
- `NotifyOnExpiration`

The final-warning threshold is clamped so it cannot occur after the first-warning threshold.

## Build prerequisites

Install Valheim and BepInExPack for Valheim, then set these environment variables before building:

- `VALHEIM_MANAGED`: the game's `valheim_Data/Managed` directory
- `BEPINEX_CORE`: the active BepInEx `core` directory

Then run:

```sh
dotnet build RestedWhispers/RestedWhispers.csproj -c Release
```

The project intentionally references the locally installed game assemblies rather than redistributing Iron Gate or Unity binaries.

## Initial acceptance test

1. Install the built DLL under `BepInEx/plugins/RestedWhispers/`.
2. Launch Valheim with an otherwise vanilla test character/world.
3. Acquire the Rested effect.
4. Verify exactly one notification appears at each configured threshold.
5. Verify one expiration message appears when Rested ends.
6. Acquire Rested again and verify the warning cycle resets.
7. Disable the mod in config and verify no notifications appear.
8. Remove the DLL and verify the character/world remain usable without migration or repair.

## Scope boundary

The first release deliberately avoids Harmony patches, custom UI, assets, prefabs, save changes, server state, and gameplay automation. Those are future complexity boundaries, not requirements for proving the basic WulfPack mod workflow.
