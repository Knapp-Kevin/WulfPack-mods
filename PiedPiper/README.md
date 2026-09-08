# Pied Piper

Pied Piper is a deliberately small Valheim quality-of-life mod that gives eligible tamed creatures one consistent command: **Follow / Stay**.

> One command. One behavior. Follow or stay.

The mod exists to make tameable creatures easier to move without becoming a pet-management overhaul.

## Scope

Initial supported creature families:

- wolf
- boar
- hen / chicken
- lox
- asksvin

Future tameable creatures can be added later when Valheim exposes them through a compatible tame/follow contract. The planned Valheim 1.0 moose is therefore a future compatibility target, not a current dependency.

## Behavior contract

Pied Piper should:

- operate only on tamed creatures
- expose one configurable command input for Follow / Stay
- preserve wolves' existing native command behavior where practical
- use Valheim's native follow-target / tameable machinery wherever possible
- ignore wild and untamed creatures
- avoid custom pathfinding when the game already has a usable follow state
- remain independently removable
- introduce no save or world migration requirement

### Rideable tameables

Lox and asksvin must not be forced into Follow while Valheim's saddle or riding behavior owns movement.

The rule is simple: **Pied Piper does not fight the mounted state.**

Any future rideable tameable should follow the same rule.

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
├── README.md
├── IMPLEMENTATION_PLAN.md
├── STATUS.md
└── manifest.json
```

Implementation source and local lifecycle tooling will be added in Phase 1 after the current installed Valheim assemblies have been inspected for the authoritative tame/follow command seam.

## Implementation principle

Historical mod source can help locate likely APIs, but the installed Valheim assemblies are authoritative.

The preferred implementation is the smallest possible adapter over Valheim's own tame/follow state. Custom AI should be a last resort, not the opening move.

## Planned local lifecycle

Pied Piper will follow the repository's established local workflow:

```powershell
.\PiedPiper\build-local.ps1
.\PiedPiper\build-local.ps1 -Install
.\PiedPiper\build-local.ps1 -Status
.\PiedPiper\build-local.ps1 -Disable
.\PiedPiper\build-local.ps1 -Enable
.\PiedPiper\build-local.ps1 -Uninstall
```

Those commands do not exist yet. They are part of Phase 1 and must manage only Pied Piper-owned files.

## Validation target

The first release is successful when a player can use one consistent command to switch every supported eligible tamed creature between Follow and Stay, without changing stats, saves, breeding, combat behavior, or unrelated AI.

Development and validation are tracked in [issue #6](https://github.com/Knapp-Kevin/WulfPack-mods/issues/6).

## Repository policy

GitHub Actions are prohibited in WulfPack Mods. Pied Piper will be built and validated locally with zero Actions runs.
