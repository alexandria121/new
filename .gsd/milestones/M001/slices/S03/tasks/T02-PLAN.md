---
estimated_steps: 12
estimated_files: 3
skills_used: []
---

# T02: Write combining unit tests

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

## Inputs

- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/BattleResolutionTests.csproj`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/NewGame.UI/ViewModels/MainViewModel.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/MagicalDeckbuilder.Shared/Combining/CombinationLookup.cs`

## Expected Output

- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/CombiningTests.cs`
- `C:/Users/Derek/Desktop/new/.gsd/worktrees/M001/Tests/CombiningTests.csproj`

## Verification

cd C:/Users/Derek/Desktop/new/.gsd/worktrees/M001 && dotnet test Tests/ --no-restore 2>&1 | tail -20
