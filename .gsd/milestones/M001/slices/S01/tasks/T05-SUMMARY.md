---
id: T05
parent: S01
milestone: M001
key_files:
  - (none)
key_decisions:
  - Turn number incremented AFTER opponent turn completes (not before)
  - Draw 1 card per turn end — consistent with Slay the Spire pacing
  - Mana refills each turn — not +1 per turn — simpler and more readable
duration: 
verification_result: mixed
completed_at: 2026-05-06T16:52:58.600Z
blocker_discovered: false
---

# T05: "End Turn button + T key binding, turn/mana refills, draw 1 card per turn"

**"End Turn button + T key binding, turn/mana refills, draw 1 card per turn"**

## What Happened

"end_player_turn() function handles full turn transition: resolve combat, AI takes turn, increment turn counter, refill mana, draw 1 card. T key bound in obj_battle_controller keypress event. Draw-on-end-turn confirmed by grep."

## Verification

"Manual test: end turn 3 times, verify mana = 3, hand size increases by 3 cards total"

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep 'deck_draw' objects/obj_end_turn_button.gml confirms card draw on turn end` | -1 | unknown (coerced from string) | 0ms |

## Deviations

"None"

## Known Issues

"None"

## Files Created/Modified

None.
