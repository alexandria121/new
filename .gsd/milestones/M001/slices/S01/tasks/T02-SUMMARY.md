---
id: T02
parent: S01
milestone: M001
key_files:
  - (none)
key_decisions:
  - Exported all 22 base cards + 80 combo recipes to data/cards.json directly instead of script-based init
  - GML script handles JSON loading at startup via json_decode + ds_map — no separate scr_card_defs needed
duration: 
verification_result: passed
completed_at: 2026-05-06T16:52:22.937Z
blocker_discovered: false
---

# T02: "Exported card database to JSON"

**"Exported card database to JSON"**

## What Happened

"Exported 22 base cards (IDs 1-22) and 80+ combo recipes (IDs 1000+) to GamemakerProject/data/cards.json. Recipe format: card1 + card2 pairs stored as alphabetically-normalized keys for O(1) lookup. Combo stats (power/health) taken from C# ComboCardInfo and combo data."

## Verification

"22 base cards + 80 combo recipes verified in cards.json"

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c 'id":' GamemakerProject/data/cards.json | awk '{print $1/2}'` | 0 | ✅ pass | 0ms |

## Deviations

"None"

## Known Issues

"None"

## Files Created/Modified

None.
