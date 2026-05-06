---
estimated_steps: 9
estimated_files: 3
skills_used: []
---

# T01: Wire hand-card-to-card drag-to-combine + visual target highlighting

Add mouse-drag handlers for hand cards that detect when one card is dragged over another hand card and auto-populate the empty combo slot. Also add a DragEnter handler that highlights valid combine targets (cards with IsCombinable=true and a valid recipe).  Steps:
1. Read GameView.xaml.cs around lines 50-90 to confirm current hand card drag pattern.
2. Add OnHandCardDragEnter handler: on drag-enter over a hand card (Border with Tag=CardViewModel), call ViewModel?.SetComboCard(card, emptySlotIndex). Empty slot = 1 if ComboCard1==null else 2. Only set if card.IsCombinable and the resulting pair has a valid recipe (check _combinationLookup.HasCombination). Also apply a glowing highlight style (e.g., BorderBrush = AccentBrush or a dedicated "combine-highlight" color) to the hovered card.
3. Add OnHandCardDragLeave to remove the highlight.
4. Add OnHandCardDrop handler: on drop over a hand card, call ViewModel?.SetComboCard(droppedCard, emptySlotIndex) — same slot logic as DragEnter.
5. Wire these handlers in GameView.xaml for all hand card Borders (the ones already handling OnHandCardPreviewMouseLeftButtonDown/Up/Move). Add AllowDrop=True and the new DragEnter/DragLeave/Drop handlers to hand card Border elements in GameView.xaml. 
6. Ensure ComboResult shows the card name/power/health in the result Border (GameView.xaml lines ~245-250 already have a placeholder — bind TextBlock content to ComboResult properties).
7. Ensure the Combine button is disabled when CanCombine=false (it already binds to CombineCardsCommand which has the CanExecute guard).
8. Add LogToFile calls for drag-enter/leave/combo events for runtime debugging.

## Inputs

- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/ViewModels/MainViewModel.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/MagicalDeckbuilder.Shared/Combining/CombinationLookup.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/MagicalDeckbuilder.Shared/Combining/combos.json`

## Expected Output

- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml`

## Verification

grep -n "OnHandCardDragEnter\|OnHandCardDragLeave\|OnHandCardDrop\|IsCombineTarget" C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs
