# Pied Piper

Pied Piper is a deliberately small Valheim quality-of-life mod that gives eligible tamed creatures one consistent command: **Follow / Stay**.

> One command. One behavior. Follow or stay.

The mod exists to make tameable creatures easier to move without becoming a pet-management overhaul.

## Current implementation direction

Public decompiled Valheim source shows that `Tameable` already owns the native command RPC and follow-target machinery, and vanilla commandable tameables use the normal **Use / interact** action to trigger it.

Pied Piper therefore follows the same model as wolves instead of introducing a second keybind:

1. enable Valheim's existing commandable path on tameables;
2. let vanilla `Tameable.Interact` keep tame-state gating and native messaging;
3. use the player's normal Valheim Use key, **E by default**, to toggle Follow / Stay;
4. leave the actual command RPC, follow target, and pathfinding to Valheim;
5. temporarily suppress commandability for rideables while a saddle is attached;
6. fail closed for rideables if saddle state cannot be verified.

The pet effect still runs through Valheim's normal interaction path before the command, matching commandable tame behavior rather than replacing interaction with a custom controller.

The **installed Valheim assemblies remain authoritative**. The current source is an implementation candidate until it compiles and runs against the installed game.

## Input behavior

Pied Piper does not add a new gameplay key.

```text
Look at tamed creature
        ↓
Press Valheim Use / interact
       E by default
        ↓
Valheim native Follow / Stay command
```

If the player remaps Valheim's normal Use action, Pied Piper follows that native interaction path rather than hard-coding a separate keyboard key.

## Scope

Initial supported creature families:

- wolf
- boar
- hen / chicken
- lox
- asksvin

Future tameable creatures can be supported when they use a compatible `Tameable` / native follow contract. A future rideable tameable should inherit the same saddle rule rather than requiring a special-case creature class.

## Rideable tameables

Pied Piper does not fight Valheim's saddle authority.

When a tameable exposes a saddle component and currently has a saddle attached, Pied Piper temporarily disables commandability for that interaction. If the current game API no longer exposes a verifiable saddle state, Pied Piper also blocks the command for that rideable rather than guessing.

For v0.1 this is intended to cover lox and asksvin generically.

## Explicit non-goals for v0.1

Pied Piper is not intended to provide:

- global army commands
- radius-based mass selection
- formations
- teleport-to-player behavior
- pet inventory
- pet stat changes
- breeding automation
- pet protection systems
- portal management
- map markers
- custom pathfinding
- a general tame overhaul

If a feature cannot be explained as part of Follow / Stay, it probably belongs somewhere else.

## Repository layout

```text
PiedPiper/
├── Plugin.cs
├── TameablePatches.cs
├── PiedPiper.csproj
├── build-local.ps1
├── README.md
├── IMPLEMENTATION_PLAN.md
├── STATUS.md
└── manifest.json
```

## Local lifecycle

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
.\PiedPiper\build-local.ps1 -Status
.\PiedPiper\build-local.ps1 -Disable
.\PiedPiper\build-local.ps1 -Enable
.\PiedPiper\build-local.ps1 -Uninstall
```

The helper manages only Pied Piper-owned files and never touches unrelated plugins.

## Validation target

The initial implementation is successful when:

- the current installed Valheim assemblies compile cleanly against the Harmony patches;
- vanilla Use / interact toggles Follow / Stay on supported tamed creatures;
- wolf behavior remains correct;
- tamed boar and hen/chicken gain native Follow / Stay;
- unsaddled lox and asksvin can Follow / Stay;
- saddled lox and asksvin remain under saddle authority;
- wild creatures remain unaffected;
- rename behavior remains available through Valheim's normal alternate interaction;
- disabling or uninstalling Pied Piper restores vanilla behavior after restart;
- no save/world migration dependency is introduced.

Development and validation are tracked in [issue #6](https://github.com/Knapp-Kevin/WulfPack-mods/issues/6).

## Repository policy

GitHub Actions are prohibited in WulfPack Mods. Pied Piper is built and validated locally with zero Actions runs.
