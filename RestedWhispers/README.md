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

The first-warning threshold is never allowed to occur later than the final-warning threshold. Negative warning values are treated as zero at runtime.

## Local build only

This repository does not use GitHub Actions. The GitHub Actions budget is zero. Build, packaging, and game validation are performed locally.

### Windows helper

With Valheim and BepInExPack installed in the default Steam location:

```powershell
.\RestedWhispers\build-local.ps1
```

To build and copy the DLL directly into `BepInEx/plugins/RestedWhispers/`:

```powershell
.\RestedWhispers\build-local.ps1 -Install
```

If Valheim is installed elsewhere:

```powershell
.\RestedWhispers\build-local.ps1 -ValheimRoot "D:\SteamLibrary\steamapps\common\Valheim" -Install
```

The helper validates the .NET SDK, `assembly_valheim.dll`, and `BepInEx.dll` before attempting the build.

### Manual build

Set:

- `VALHEIM_MANAGED`: the game's `valheim_Data/Managed` directory
- `BEPINEX_CORE`: the active BepInEx `core` directory

Then run:

```sh
dotnet build RestedWhispers/RestedWhispers.csproj -c Release
```

The project intentionally references the locally installed game assemblies rather than redistributing Iron Gate or Unity binaries.

## Initial acceptance test

1. Build and install the DLL locally.
2. Launch Valheim with an otherwise vanilla test character/world.
3. Confirm the BepInEx log reports `Rested Whispers 0.1.0 loaded.` without plugin errors.
4. Acquire the Rested effect.
5. Verify exactly one notification appears at each configured threshold.
6. Verify one expiration message appears when Rested ends.
7. Acquire Rested again and verify the warning cycle resets.
8. Disable the mod while Rested, allow Rested to expire, re-enable the mod, and verify no stale expiration message appears.
9. Disable the mod in config and verify no notifications appear.
10. Remove the DLL and verify the character/world remain usable without migration or repair.
11. Smoke-test joining a multiplayer or dedicated-server world as a client.

## Packaging status

`manifest.json` is present, but a Thunderstore release package is intentionally not considered complete yet. A final package still needs a proper `icon.png`, the validated release DLL, and a local package-content check after in-game acceptance testing.

## Scope boundary

The first release deliberately avoids Harmony patches, custom UI, assets, prefabs, save changes, server state, and gameplay automation. Those are future complexity boundaries, not requirements for proving the basic WulfPack mod workflow.
