---
id: T02
parent: S02
milestone: M001
key_files:
  - NewGame.UI/ViewModels/MainViewModel.cs
key_decisions:
  - MainViewModel.cs game-over guards are sequential if blocks (not else-if), so each damage step independently triggers the game-end check if needed. This is correct behavior.
duration: 
verification_result: passed
completed_at: 2026-05-06T18:01:18.470Z
blocker_discovered: false
---

# T02: Added mid-combat game-over guard in ExecuteOpponentTurn after opponent combat resolves

**Added mid-combat game-over guard in ExecuteOpponentTurn after opponent combat resolves**

## What Happened

Added a game-over guard block in ExecuteOpponentTurn() between ResolveOpponentCombat() and ResolveOpponentWeaponDamage(), ensuring the opponent's weapon attack is skipped when the game has already ended. The guard pattern mirrors existing guards in EndTurn() and was confirmed to be the only missing piece — EndTurn() already had its three sequential guards (after player combat, after weapon damage, after opponent turn), and the original ExecuteOpponentTurn() already had one after the opponent turn. Memory store note (MEM002) correctly identified this gap. No changes needed to RemoveDeadCreatures() timing or ResolveOpponentWeaponDamage() — the existing code already handles health decrementing properly.

## Verification

grep confirmed 5 total game-over guard checks across MainViewModel.cs: 3 in EndTurn() (after player combat, after weapon, after opponent turn) and 2 in ExecuteOpponentTurn() (new mid-combat guard after ResolveOpponentCombat, plus the original guard after opponent turn). All guards show GameEndWindow instantiation.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -n "GameEndWindow\|OpponentHealth <= 0" NewGame.UI/ViewModels/MainViewModel.cs` | 0 | ✅ pass | 50ms |

## Deviations

The plan described EndTurn() as having "redundant if/else if/else if chain" — it was actually already correct with sequential if blocks, not an else-if chain. No functional changes needed there. Only ExecuteOpponentTurn() was missing the mid-combat game-over guard.

## Known Issues

None.

## Files Created/Modified

- `NewGame.UI/ViewModels/MainViewModel.cs`
