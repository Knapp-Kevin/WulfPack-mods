# Pied Piper

Pied Piper is a deliberately small Valheim quality-of-life mod that gives eligible tamed creatures one consistent command: **Follow / Stay**.

> One command. One behavior. Follow or stay.

The mod exists to make tameable creatures easier to move without becoming a pet-management overhaul.

## Current implementation direction

Public decompiled Valheim source shows that `Tameable` already owns the native command RPC and follow-target machinery.

The first candidate enabled `m_commandable` directly, but review exposed an undesirable side effect: that changes the ordinary pet interaction on non-wolf tameables. Pied Piper therefore now leaves vanilla interaction behavior alone and invokes Valheim's existing private `Command` path through a separate configurable key.

The current candidate:

1. uses a short forward raycast from the player's camera to identify the tameable being looked at;
2. requires the target to be tamed;
3. invokes Valheim's existing `Tameable.Command` method rather than implementing custom follow AI;
4. preserves normal pet, rename, saddle, and wolf interactions;
5. blocks rideable commands when a saddle is attached;
6. fails closed on rideables if saddle state cannot be verified;
7. leaves Follow / Stay state and pathfinding to Valheim.

The **installed Valheim assemblies remain authoritative**. The current source is an implementation candidate until it compiles and runs against the installed game.

## Input behavior

Pied Piper uses one configurable command key. The current default is **G**.

```text
Look at tamed creature
        ↓
      Press G
        ↓
Valheim native Follow / Stay command
```

The command distance is configurable and defaults to 5 metres.

Using a dedicated key is deliberate. Normal Valheim petting and rename behavior remain untouched.

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

When a tameable exposes a saddle component and currently has a saddle attached, Pied Piper refuses the Follow / Stay command. If the current game API no longer exposes a verifiable saddle state, Pied Piper also refuses the command for that rideable rather than guessing.

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
├── NativeFollowCommand.cs
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

- the current installed Valheim assemblies compile cleanly against the implementation;
- pressing the Pied Piper key commands the tamed creature under the crosshair;
- ordinary pet and rename interactions still work exactly as vanilla expects;
- wolf behavior remains correct;
- tamed boar and hen/chicken gain native Follow / Stay;
- unsaddled lox and asksvin can Follow / Stay;
- saddled lox and asksvin remain under saddle authority;
- wild creatures remain unaffected;
- disabling or uninstalling Pied Piper restores vanilla behavior after restart;
- no save/world migration dependency is introduced.

Development and validation are tracked in [issue #6](https://github.com/Knapp-Kevin/WulfPack-mods/issues/6).

## Repository policy

GitHub Actions are prohibited in WulfPack Mods. Pied Piper is built and validated locally with zero Actions runs.
