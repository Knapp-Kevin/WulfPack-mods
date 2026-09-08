# Pied Piper status

## Current status

**Phase 1 implementation candidate written. Local compile and in-game validation remain.**

The first implementation is intentionally tiny. Public decompiled Valheim source shows that `Tameable` already owns the native `Command` RPC and follow-target machinery, and that normal interaction invokes it when `m_commandable` is enabled.

Pied Piper now enables that existing command path for tameables rather than introducing custom pet AI.

## Implemented candidate

- BepInEx plugin entry point.
- Harmony patch on `Tameable.Awake` enabling `m_commandable`.
- Existing vanilla Follow / Stay behavior remains the command implementation.
- Wild / untamed creatures remain gated by vanilla `Tameable.Interact` behavior.
- Normal Valheim Use / interact input is the command input. No second keybind is introduced.
- Generic saddle guard suppresses Follow / Stay switching while a tameable currently has an attached saddle.
- Local build / install / disable / enable / status / uninstall helper.
- No custom AI or pathfinding.
- No save/world schema.
- Zero GitHub Actions.

## Why the implementation is this small

The current public `Tameable` contract contains:

- `m_commandable`
- a registered `Command` RPC
- the native interaction path
- `MonsterAI` follow behavior behind that command
- saddle state owned by `Tameable`

Older Valheim mods have independently used the same `m_commandable = true` seam to expose native follow commands, which provides useful corroboration. The installed game assemblies are still authoritative and may require API-drift corrections before this candidate is accepted.

## Local validation gate

Run:

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
```

Then verify:

1. current installed assemblies compile with the Harmony and `Tameable` contracts used here;
2. plugin loads with no BepInEx / Harmony errors;
3. vanilla wolf Follow / Stay still works;
4. tamed boar gains Follow / Stay;
5. tamed hen/chicken gains Follow / Stay;
6. unsaddled lox gains Follow / Stay;
7. unsaddled asksvin gains Follow / Stay;
8. saddled rideables do not switch into Follow through the normal Use interaction;
9. wild / untamed creatures remain unaffected;
10. petting and renaming remain usable;
11. disable / enable works after restart;
12. uninstall restores vanilla behavior and leaves other plugins untouched.

## Current boundary

Do not claim Pied Piper is playable or validated until the local compile and game pass succeeds. If current Valheim has changed `Tameable`, saddle state, Harmony method signatures, or the command path, correct the implementation against the installed assemblies rather than preserving this candidate for its own sake.

Issue #6 remains the authoritative implementation and validation tracker.
