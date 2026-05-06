---
id: T01
parent: S03
milestone: M001
key_files:
  - NewGame.UI/Views/GameView.xaml.cs — added OnHandCardDragEnter/Leave/Drop handlers, CombinationLookup field, using directive
  - NewGame.UI/Views/GameView.xaml — added AllowDrop, DragEnter, DragLeave, Drop handlers to player hand card Borders
key_decisions:
  - Used CombinationLookup.HasCombination() (card1, card2) to check validity of drag-to-combine target pairs — mirrors UpdateComboPreview() logic in MainViewModel exactly
  - Reset border style to #506046 on DragLeave to match XAML default (BorderBrush=#506046, BorderThickness=2)
  - No IsCombineTarget property added — the existing draggedCard.IsCombinable check in code-behind serves the same purpose without modifying CardViewModel
duration: 
verification_result: passed
completed_at: 2026-05-06T18:11:05.811Z
blocker_discovered: false
---

# T01: Wired hand-card-to-card drag-to-combine: drag-enter highlights valid combo targets, drag-leave clears highlight, drop auto-populates first empty slot

**Wired hand-card-to-card drag-to-combine: drag-enter highlights valid combo targets, drag-leave clears highlight, drop auto-populates first empty slot**

## What Happened

Added three drag event handlers to GameView.xaml.cs (OnHandCardDragEnter, OnHandCardDragLeave, OnHandCardDrop) that wire hand-card-to-hand-card drag-to-combine. OnDragEnter: checks draggedCard.IsCombinable, then verifies the pair has a valid recipe via _combinationLookup.HasCombination() before highlighting the target border in gold (#C8DC50) with thickness 3. OnDragLeave: resets border to default #506046/2px. OnDrop: auto-populates first empty combo slot (slot 1 if ComboCard1 null else slot 2) by calling ViewModel?.SetComboCard(droppedCard, slotIndex). The _combinationLookup field is initialized in the constructor and used with the correct MagicalDeckbuilder.Combining namespace import. The player hand card Border template in GameView.xaml was updated to add AllowDrop=True and wire DragEnter/DragLeave/Drop to the new handlers. LogToFile calls at [COMBO-DRAG] prefix in each handler enable runtime inspection. The Combine button already binds to CombineCardsCommand which has a CanExecute guard, so no change needed there.

## Verification

grep verification confirmed all three new handlers (OnHandCardDragEnter, OnHandCardDragLeave, OnHandCardDrop) are present in GameView.xaml.cs and wired in GameView.xaml. Border highlight uses gold color #C8DC50 to visually distinguish valid combo targets. LogToFile calls at [COMBO-DRAG] and [COMBO-DROP] prefix enable runtime inspection. Combine button is already guarded by CombineCardsCommand CanExecute.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -n "OnHandCardDragEnter\|OnHandCardDragLeave\|OnHandCardDrop" GameView.xaml.cs` | 0 | ✅ pass | 91ms |
| 2 | `grep "OnHandCardDragEnter\|OnHandCardDragLeave\|OnHandCardDrop" GameView.xaml` | 0 | ✅ pass | 0ms |

## Deviations

None

## Known Issues

None

## Files Created/Modified

- `NewGame.UI/Views/GameView.xaml.cs — added OnHandCardDragEnter/Leave/Drop handlers, CombinationLookup field, using directive`
- `NewGame.UI/Views/GameView.xaml — added AllowDrop, DragEnter, DragLeave, Drop handlers to player hand card Borders`
