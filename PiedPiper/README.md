# Pied Piper

Pied Piper is a deliberately small Valheim quality-of-life mod that gives eligible tamed creatures one consistent command: **Follow / Stay**.

> One command. One behavior. Follow or stay.

The mod exists to make tameable creatures easier to move without becoming a pet-management overhaul.

## Current implementation direction

Public decompiled Valheim source shows that `Tameable` already owns the native command RPC and follow-target machinery. The normal interaction path calls that command only when `m_commandable` is enabled.

That gives Pied Piper a very small implementation seam:

1. enable Valheim's existing command path on tameables;
2. leave wild / untamed creatures unaffected because vanilla `Tameable.Interact` already gates command behavior on tame state;
3. preserve wolves' native behavior rather than replacing it;
4. suppress Follow / Stay switching while a rideable tameable currently has a saddle attached;
5. otherwise let Valheim's own command RPC and `MonsterAI` follow state do the work.

The **installed Valheim assemblies remain authoritative**. The current source is an implementation candidate until it compiles and runs against the installed game.

## Input behavior

Pied Piper intentionally uses Valheim's normal **Use / interact** input instead of inventing a second pet-command key.

For an eligible tamed creature:

```text
Look at tameable
    ↓
Press Use
    ↓
Valheim native Follow / Stay command
```

This keeps wolves familiar and makes boars, hens/chickens, lox, asksvin, and compatible future tameables behave consistently.

## Scope

Initial supported creature families:

- wolf
- boar
- hen / chicken
- lox
- asksvin

Future tameable creatures can be supported automatically when they use a compatible `Tameable` / native follow contract. A future rideable tameable should inherit the same saddle rule rather than requiring a special-case creature class.

## Rideable tameables

Pied Piper does not fight Valheim's saddle authority.

When a tameable exposes a saddle component and currently has a saddle attached, normal petting / rename behavior remains available, but Pied Piper does not allow that interaction to switch the creature into Follow.

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

- the current installed Valheim assemblies compile cleanly against the patch;
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
