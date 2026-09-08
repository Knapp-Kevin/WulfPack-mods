# Rested Whispers

A small, client-side Valheim quality-of-life mod that gently warns you as the
existing Rested effect begins to fade.

Rested Whispers does not add a Tired status effect, change player stats, alter
the Rested mechanic, modify saves, or synchronize new gameplay state. It only
observes the existing Rested effect and displays native Valheim notifications.

## Default notifications

- 2 minutes remaining: `You're getting tired.`
- 30 seconds remaining: `You long for the warmth of a fire.`

Rested Whispers deliberately says **nothing** when Rested actually ends. Valheim
already announces that itself through the status effect's own stop message
("You're no longer rested"), so a second message would just duplicate it.

## Configuration

BepInEx configuration (`BepInEx/config/com.wulfpack.restedwhispers.cfg`) exposes:

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Master switch. `false` suppresses all notifications. |
| `FirstWarningSeconds` | `120` | Seconds of Rested **remaining** when the first warning fires. |
| `FinalWarningSeconds` | `30` | Seconds remaining when the final warning fires. |
| `MessagePosition` | `Center` | `Center` for Valheim's large banner text, or `TopLeft` for the small corner text used for item pickups. |

Thresholds are measured in time **remaining**, not time elapsed, so the
wall-clock moment they fire moves with your comfort level (total Rested duration
is a base value plus a per-comfort-level bonus).

The first-warning threshold is never allowed to occur later than the
final-warning threshold: if you set `FirstWarningSeconds` below
`FinalWarningSeconds`, the first is pulled up to match. Negative warning values
are treated as zero at runtime.

Each message fires at most once per Rested cycle, and re-acquiring Rested arms
both again. If the mod first sees Rested when you are already below the final
threshold, only the final warning fires — you do not get both at once.

Two exceptions to "once per cycle", both deliberate. Toggling `Enabled` off and
back on re-arms both warnings, so they can fire a second time within the same
Rested cycle — that is what makes re-enabling behave like a fresh start rather
than a dead mod. And a warning whose message could not be delivered (the HUD not
yet being up) is retried on the next poll rather than being marked as shown.

If you ran an earlier build, your config file may still contain a
`NotifyOnExpiration` entry. It is inert and can be deleted; BepInEx keeps
orphaned keys rather than removing them.

The config file is written on first launch, so it will not exist until you have
run the game once with the mod installed. Edit it while the game is closed.

---

# Install, disable, re-enable, uninstall

Everything is one command. The helper finds Valheim automatically via the Steam
registry and `libraryfolders.vdf`; pass `-ValheimRoot` only if that fails.

| I want to… | Command |
|---|---|
| Build only | `.\RestedWhispers\build-local.ps1` |
| **Install** | `.\RestedWhispers\build-local.ps1 -Install` |
| **Disable** (keep it around) | `.\RestedWhispers\build-local.ps1 -Disable` |
| **Re-enable** | `.\RestedWhispers\build-local.ps1 -Enable` |
| **Uninstall** (remove entirely) | `.\RestedWhispers\build-local.ps1 -Uninstall` |
| **Check current state** | `.\RestedWhispers\build-local.ps1 -Status` |

**Quit Valheim before installing, disabling, enabling, or uninstalling.** BepInEx
memory-maps loaded plugin DLLs, so those operations cannot succeed while the game
is open; the script refuses them with a clear message rather than a raw error.
`-Status` works at any time.

Non-default Valheim location:

```powershell
.\RestedWhispers\build-local.ps1 -ValheimRoot "D:\SteamLibrary\steamapps\common\Valheim" -Install
```

## Install

```powershell
.\RestedWhispers\build-local.ps1 -Install
```

Builds Release and copies the DLL to
`<Valheim>\BepInEx\plugins\RestedWhispers\RestedWhispers.dll`.

## Disable (without deleting anything)

```powershell
.\RestedWhispers\build-local.ps1 -Disable
```

Moves the mod's folder to `<Valheim>\BepInEx\plugins-disabled\RestedWhispers\`.

**Why a separate folder and not a renamed file.** BepInEx discovers plugins by
scanning for `*.dll` **across the plugins tree**, so a "disabled" subfolder
*inside* `BepInEx\plugins\` would still be loaded. `plugins-disabled` is a
sibling of `plugins`, not a child, so BepInEx never scans it. That holds whether
or not the scan recurses, which is why this approach was chosen over a filename
convention.

Your config file is left in place, so your thresholds survive a disable.

## Re-enable

```powershell
.\RestedWhispers\build-local.ps1 -Enable
```

Moves the folder back and removes the now-empty `plugins-disabled` directory.

## Uninstall completely

```powershell
.\RestedWhispers\build-local.ps1 -Uninstall
```

Deletes exactly these three paths:

1. `<Valheim>\BepInEx\plugins\RestedWhispers\`
2. `<Valheim>\BepInEx\plugins-disabled\RestedWhispers\`
3. `<Valheim>\BepInEx\config\com.wulfpack.restedwhispers.cfg`

Plus one more, only when it is left empty: the `plugins-disabled` directory
itself, if this mod's removal emptied it. It is deleted only when it contains
nothing at all, so a plugin someone else parked there is never destroyed.

There are no wildcards. Every deletion goes through a single guarded function
that refuses any path whose final component is not the expected name, and
refuses reparse points (junctions/symlinks) outright. **Your other BepInEx
plugins are never enumerated, moved, or deleted.**

Rested Whispers writes nothing to your character or world. After uninstalling,
your save is exactly as it was — no migration, no repair, no leftover state.

---

# Verifying you are mod-free before joining a no-mod server

**Read this part before joining a server that prohibits third-party mods.**

### Step 1 — check the files

```powershell
.\RestedWhispers\build-local.ps1 -Status
```

Look for `Rested Whispers : NOT INSTALLED`. The command also lists every other
entry in your `BepInEx\plugins` folder under *"Other entries … NOT managed by
this script"* — read that list.

### Step 2 — restart Valheim, then check the log

`-Status` reads the files on disk plus the **previous** run's log. Only a fresh
launch proves what actually loaded. Start Valheim, quit, then open:

```
<Valheim>\BepInEx\LogOutput.log
```

Near the top you will see a line like `N plugins to load`, followed by a
`Loading [...]` line per plugin. There must be **no** `Rested Whispers` entry.
Running `-Status` again after that launch will also report the log as clean.

### Step 3 — the honest caveat

> **Removing Rested Whispers does not make a modded Valheim install acceptable
> to a server that forbids third-party mods.**

This helper manages Rested Whispers and nothing else, by design. If your
`BepInEx\plugins` folder contains anything else — or if you launch through
Thunderstore Mod Manager / r2modman, which use their own separate profile
folders that this script never touches — those mods are still installed and are
your responsibility to remove.

For a genuinely vanilla client, the reliable route is to launch a profile with
no mods at all, or to remove BepInEx itself (delete `winhttp.dll`,
`doorstop_config.ini`, and the `BepInEx` folder from the game directory) and
verify via Steam's *Verify integrity of game files*.

---

# Building

This repository does not use GitHub Actions. The GitHub Actions budget is zero.
Build, packaging, and game validation are performed locally.

The helper validates the .NET SDK, `assembly_valheim.dll`, and `BepInEx.dll`
before attempting a build.

### Manual build

Set:

- `VALHEIM_MANAGED`: the game's `valheim_Data/Managed` directory
- `BEPINEX_CORE`: the active BepInEx `core` directory

Then run:

```sh
dotnet build RestedWhispers/RestedWhispers.csproj -c Release
```

Output lands at `RestedWhispers/bin/Release/netstandard2.1/RestedWhispers.dll`.

The project references the locally installed game assemblies rather than
redistributing Iron Gate or Unity binaries.

### Toolchain notes (verified against the current game build)

- **`netstandard2.1`**, not `2.0`. Valheim now runs on Unity 6
  (`6000.0.61`), whose `UnityEngine.CoreModule` references netstandard 2.1;
  targeting 2.0 fails with `CS1705`.
- **`UnityEngine.dll` must be referenced** alongside `UnityEngine.CoreModule.dll`.
  It is the type-forwarding shim `MonoBehaviour` resolves through; without it the
  build fails with `CS0012`.
- **The Rested effect is looked up by hash**, not by name:
  `SEMan.GetStatusEffect(SEMan.s_statusEffectRested)`. The `string` overload of
  `GetStatusEffect` no longer exists — using it fails with `CS1503`.
- `MSB3277` reference-conflict warnings are demoted to messages in the csproj.
  They are inherent to referencing Unity's assembly set and do not affect the
  emitted assembly.

## Acceptance test

1. `.\RestedWhispers\build-local.ps1 -Install`
2. Launch Valheim with an otherwise vanilla test character/world.
3. Confirm `BepInEx\LogOutput.log` reports `Rested Whispers 0.1.0 loaded.`
   without plugin errors.
4. Acquire the Rested effect.
5. Verify exactly one notification appears at each configured threshold.
6. Verify the mod stays silent when Rested ends (Valheim's own "You're no longer rested" should be the only message).
7. Acquire Rested again and verify the warning cycle resets.
8. Set `Enabled = false` while Rested, let Rested expire, set it back to `true`,
   and verify no stale message appears. The mod emits no expiry message at all,
   so this failure mode is impossible by construction rather than merely guarded.
9. With `Enabled = false`, verify no notifications appear at all.
10. `.\RestedWhispers\build-local.ps1 -Uninstall`, then confirm the character and
    world still load with no migration or repair.
11. Smoke-test joining a multiplayer or dedicated-server world as a client.

## Packaging status

`manifest.json` is present and its BepInEx dependency is pinned to the version
actually in use (`5.4.2202`). A Thunderstore release is **not** ready: the
package still needs a real `icon.png`, the validated release DLL, and a local
package-content check after in-game acceptance testing.

## Scope boundary

The first release deliberately avoids Harmony patches, custom UI, assets,
prefabs, save changes, server state, and gameplay automation. Those are future
complexity boundaries, not requirements for proving the basic WulfPack mod
workflow.
