# Pied Piper status

## Current status

**Phase 1 implementation candidate written. Local compile and in-game validation remain.**

Public decompiled Valheim source shows that `Tameable` already owns the native `Command` RPC and follow-target machinery. Pied Piper now exposes that native commandable path to tameables and uses Valheim's existing Use / interact action, **E by default**, just like vanilla wolf commands.

## Implemented candidate

- BepInEx plugin entry point.
- Harmony patch on `Tameable.Awake` enabling `m_commandable`.
- Native Valheim Use / interact input is the command input. No second Pied Piper keybind exists.
- Existing vanilla Follow / Stay behavior remains the command implementation.
- Wild / untamed creatures remain gated by vanilla `Tameable.Interact` behavior.
- Generic saddle guard suppresses Follow / Stay switching while a rideable tameable has an attached saddle.
- Saddle-state uncertainty fails closed for rideables.
- Local build / install / disable / enable / status / uninstall helper.
- No custom AI or pathfinding.
- No save/world schema.
- Zero GitHub Actions.

## Why the implementation is this small

The current public `Tameable` contract contains `m_commandable`, the `Command` RPC, native interaction handling, `MonsterAI` follow behavior behind that command, and saddle state. Pied Piper's job is therefore to expose existing behavior rather than reimplement it.

## Local validation gate

Run:

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
```

Then verify:

1. current installed assemblies compile with the Harmony and `Tameable` contracts used here;
2. plugin loads with no BepInEx / Harmony errors;
3. vanilla wolf Follow / Stay still works with Use / E;
4. tamed boar gains Follow / Stay with Use / E;
5. tamed hen/chicken gains Follow / Stay with Use / E;
6. unsaddled lox gains Follow / Stay;
7. unsaddled asksvin gains Follow / Stay;
8. saddled rideables do not switch into Follow;
9. wild / untamed creatures remain unaffected;
10. alternate rename interaction remains available;
11. disable / enable works after restart;
12. uninstall restores vanilla behavior and leaves other plugins untouched.

## Current boundary

Do not claim Pied Piper is playable or validated until the local compile and game pass succeeds. If current Valheim has changed `Tameable`, saddle state, Harmony method signatures, or the command path, correct the implementation against the installed assemblies rather than preserving this candidate for its own sake.

Issue #6 remains the authoritative implementation and validation tracker.
