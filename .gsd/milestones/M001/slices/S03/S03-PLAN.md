# S03: Card Combining

**Goal:** Wire the drag-to-combine UI: dragging one hand card over another auto-populates the combo slots, visual highlight shows valid targets, and pressing Combine produces the result card in hand. Depends on S01 (hand display + drag-drop infrastructure) — S01 already has `SetComboCard`, `UpdateComboPreview`, `CombineCardsCommand`, `CanCombine`, `ComboResult`, and the XAML combo slots + Combine button. This slice adds the hand-card-to-hand-card drag detection that makes the feature actually work.
**Demo:** After S03: dragging one card over another in hand produces a combo card if recipe exists

## Must-Haves

- Drag one hand card over another → combo slot 1 or 2 auto-populates with that card (if recipe exists)\n- Valid combine target cards highlighted with accent color during drag\n- Combine button enabled only when CanCombine=true\n- Pressing Combine → both ingredient cards removed from hand, result card added to hand\n- Result card appears in the combo result display area\n- All unit tests pass

## Proof Level

- This slice proves: fixture

## Integration Closure

Wiring requires GameView.xaml changes (drag handlers + XAML bindings) and GameView.xaml.cs (event handlers). Both are tracked. The Combine button and ComboResult display are already bound in XAML; this slice completes the drag path that populates them.

## Verification

- LogToFile calls in GameView.xaml.cs for drag-enter/leave/combine events enable runtime inspection when the feature fails. MainViewModel already logs at [CardCombiner] prefix.

## Tasks

- [ ] **T01: Wire hand-card-to-card drag-to-combine + visual target highlighting** `est:45m`
  Add mouse-drag handlers for hand cards that detect when one card is dragged over another hand card and auto-populate the empty combo slot. Also add a DragEnter handler that highlights valid combine targets (cards with IsCombinable=true and a valid recipe).  Steps:
  1. Read GameView.xaml.cs around lines 50-90 to confirm current hand card drag pattern.
  2. Add OnHandCardDragEnter handler: on drag-enter over a hand card (Border with Tag=CardViewModel), call ViewModel?.SetComboCard(card, emptySlotIndex). Empty slot = 1 if ComboCard1==null else 2. Only set if card.IsCombinable and the resulting pair has a valid recipe (check _combinationLookup.HasCombination). Also apply a glowing highlight style (e.g., BorderBrush = AccentBrush or a dedicated "combine-highlight" color) to the hovered card.
  3. Add OnHandCardDragLeave to remove the highlight.
  4. Add OnHandCardDrop handler: on drop over a hand card, call ViewModel?.SetComboCard(droppedCard, emptySlotIndex) — same slot logic as DragEnter.
  5. Wire these handlers in GameView.xaml for all hand card Borders (the ones already handling OnHandCardPreviewMouseLeftButtonDown/Up/Move). Add AllowDrop=True and the new DragEnter/DragLeave/Drop handlers to hand card Border elements in GameView.xaml. 
  6. Ensure ComboResult shows the card name/power/health in the result Border (GameView.xaml lines ~245-250 already have a placeholder — bind TextBlock content to ComboResult properties).
  7. Ensure the Combine button is disabled when CanCombine=false (it already binds to CombineCardsCommand which has the CanExecute guard).
  8. Add LogToFile calls for drag-enter/leave/combo events for runtime debugging.
  - Files: `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml`, `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs`, `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/ViewModels/MainViewModel.cs`
  - Verify: grep -n "OnHandCardDragEnter\|OnHandCardDragLeave\|OnHandCardDrop\|IsCombineTarget" C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs

- [ ] **T02: Write combining unit tests** `est:30m`
  Add xUnit tests for the combining flow covering the key scenarios. Steps:
  1. Add CombiningTests.cs to the Tests/ directory alongside BattleResolutionTests.cs.
  2. Tests to cover:
     a. CombineCardsCommand enabled when two IsCombinable cards with valid recipe are set
     b. CombineCardsCommand disabled when no combo recipe exists
     c. CombineCardsCommand disabled when either card is not IsCombinable
     d. ExecuteCombine removes both ingredient cards from hand and adds result card
     e. ComboResult preview shows correct card name for valid recipe
     f. Null result when no recipe exists
  3. Use MainViewModel reflection helpers from BattleResolutionTests.cs (Application init, InitializeOpponentAI, GetFieldSlots).
  4. Name the test project and class so `dotnet test` discovers them.
  5. Run `dotnet test` from Tests/ directory to confirm all tests pass.
  - Files: `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.cs`, `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.csproj`, `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/ViewModels/MainViewModel.cs`
  - Verify: cd C:/Users/Derek/Desktop/new/.gsd/worktrees/M001 && dotnet test Tests/ --no-restore 2>&1 | tail -20

## Files Likely Touched

- C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml
- C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/Views/GameView.xaml.cs
- C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/ViewModels/MainViewModel.cs
- C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.cs
- C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.csproj
