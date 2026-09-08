# Pied Piper status

## Current status

**Phase 1 implementation candidate written. Local compile and in-game validation remain.**

Public decompiled Valheim source shows that `Tameable` already owns the native Follow / Stay command path and `MonsterAI` follow behavior. Pied Piper therefore does not implement custom pet AI.

The first candidate simply enabled `m_commandable`, but review found that this would replace the normal pet interaction on non-wolf tameables. The candidate has been corrected before validation.

## Current implementation candidate

- BepInEx plugin entry point.
- Dedicated configurable Pied Piper command key, default **G**.
- Configurable command distance, default 5 metres.
- Short camera-forward raycast selects the tameable being looked at.
- Target must be tamed.
- Native private `Tameable.Command` is resolved reflectively and invoked rather than reimplementing Follow / Stay.
- Existing vanilla pet, rename, saddle, and wolf interaction behavior remains untouched.
- Rideables with an attached saddle are blocked from Pied Piper commands.
- Saddle-state API drift fails closed for rideables.
- No Harmony dependency in the revised candidate.
- Local build / install / disable / enable / status / uninstall helper.
- No custom AI or pathfinding.
- No save/world schema.
- Zero GitHub Actions.

## Why reflection is intentional here

The command method is internal/private game machinery. Reflection lets Pied Piper call the existing native command path without patching `Tameable.Interact` or changing `m_commandable` globally.

The adapter also tolerates reasonable signature drift by locating a `Command` method whose first parameter accepts `Player` and filling optional/default trailing parameters. The installed game assemblies remain authoritative and will determine whether this compatibility approach is sufficient.

## Local validation gate

Run:

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
```

Then verify:

1. current installed assemblies compile against `Player`, `Tameable`, Unity raycast, and BepInEx `KeyboardShortcut` contracts;
2. plugin loads with no BepInEx errors;
3. pressing G while looking at a tamed boar toggles native Follow / Stay;
4. ordinary boar petting still works through vanilla Use;
5. tamed hen/chicken Follow / Stay works;
6. vanilla wolf interaction remains unchanged and the Pied Piper key also reaches the native command path;
7. unsaddled lox Follow / Stay works;
8. unsaddled asksvin Follow / Stay works;
9. saddled rideables are refused;
10. wild / untamed creatures are ignored;
11. target distance behaves correctly;
12. config key rebinding works;
13. disable / enable works after restart;
14. uninstall restores vanilla-only behavior and leaves other plugins untouched.

## Current boundary

Do not claim Pied Piper is playable or validated until the local compile and game pass succeeds. If current Valheim has changed the native command method, saddle state, or raycast-relevant component layout, correct the implementation against the installed assemblies rather than preserving this candidate for its own sake.

Issue #6 remains the authoritative implementation and validation tracker.
