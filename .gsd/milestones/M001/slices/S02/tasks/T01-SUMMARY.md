---
id: T01
parent: S02
milestone: M001
key_files:
  - NewGame.UI/ViewModels/MainViewModel.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-06T17:49:07.017Z
blocker_discovered: false
---

# T01: Fixed mana refill to use PlayerMaxMana instead of flat +2

**Fixed mana refill to use PlayerMaxMana instead of flat +2**

## What Happened

Changed the mana refill logic in MainViewModel.cs EndTurn() from `Math.Min(PlayerMana + 2, 10)` to `PlayerMaxMana`. This ensures the player's mana refills to their maximum mana pool (which scales with turns played, capped at 10) rather than a flat +2. The other Math.Min usages in the file (ability mana gains at lines 738 and 2334) are unrelated and were left untouched.

## Verification

Ran grep to verify the fix. Confirmed line 2044 now reads `PlayerMana = PlayerMaxMana` and the old `Math.Min(PlayerMana + 2, 10)` pattern no longer appears at that location.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -n "PlayerMana = PlayerMaxMana\|PlayerMana = Math.Min" NewGame.UI/ViewModels/MainViewModel.cs` | 0 | ✅ pass | 50ms |

## Deviations

None

## Known Issues

None

## Files Created/Modified

- `NewGame.UI/ViewModels/MainViewModel.cs`
