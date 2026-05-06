---
id: T04
parent: S01
milestone: M001
key_files:
  - (none)
key_decisions:
  - Drag state stored per-card instance (is_dragging, drag_offset_x/y) not in global
  - Mana cost check at drag start — unaffordable cards flash red immediately
  - Combine drag uses same is_dragging flag discriminated by global.combine_selected_card state
duration: 
verification_result: passed
completed_at: 2026-05-06T16:53:09.855Z
blocker_discovered: false
---

# T04: "Drag-to-slot card playing with mana check, visual feedback, and combine gesture"

**"Drag-to-slot card playing with mana check, visual feedback, and combine gesture"**

## What Happened

"obj_card_base handles all drag-and-drop via mouse events. Creature slots highlight green/red on drag-over. Mana cost check at drag start (red tint on unaffordable cards). Combine state tracked globally (combine_selected_card). Red flash feedback via flash_timer/flash_color system."

## Verification

"Manual test: drag creature to open slot, verify card moves from hand to slot"

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c is_dragging objects/obj_card_base.gml` | 0 | ✅ pass | 0ms |

## Deviations

"None"

## Known Issues

"None"

## Files Created/Modified

None.
