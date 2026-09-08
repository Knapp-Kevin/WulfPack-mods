# Pied Piper status

## Current status

**Phase 0: repository mesh established.**

No gameplay implementation has been claimed yet.

## Confirmed product decisions

- Name: Pied Piper.
- Purpose: one consistent Follow / Stay command for eligible tamed creatures.
- Current target families: wolf, boar, hen/chicken, lox, asksvin.
- Future tameable creatures may be added when Valheim exposes a compatible contract.
- Rideable tameables must not be forced into Follow while mounted/saddle behavior owns movement.
- Prefer native Valheim follow machinery over custom AI.
- Wild/untamed creatures are ignored.
- No teleporting, formations, pet inventory, breeding automation, stat changes, or general tame overhaul.
- No save/world migration dependency.
- Zero GitHub Actions.

## Next gate

Inspect the current installed Valheim assemblies and identify the authoritative tame/follow command seam before writing gameplay code.

Questions Phase 1 must answer:

1. What exact method/state makes a vanilla wolf follow or stay today?
2. Do boar and hen/chicken already expose compatible dormant follow machinery?
3. What does lox/asksvin use for follow state, and how is active riding/saddle ownership represented?
4. Can the entire mod be implemented through one narrow native adapter rather than creature-specific AI patches?
5. Which input seam gives a clean configurable command without breaking normal interaction behavior?

Issue #6 is the authoritative implementation and validation tracker.
