---
estimated_steps: 4
estimated_files: 1
skills_used: []
---

# T02: Add GameEndWindow and EndTurn Game-Over Guards

EndTurn() calls Application.Current.Dispatcher.Invoke to show GameEndWindow after player combat, player weapon, and opponent turn. Each check is redundant (if/else if/else if chain). Also add game-over guards after opponent combat and before incrementing TurnCount. Also ensure ResolveOpponentWeaponDamage() properly decrements OpponentHealth and triggers the game-end check.

1. **EndTurn()**: Add game-over guard after ResolveOpponentCombat() (before opponent's weapon). The check after weapon damage is already present.
2. **ExecuteOpponentTurn()**: Ensure RemoveDeadCreatures() is called after opponent combat (already present at line 2963). Add game-over guard after ResolveOpponentCombat() but before ResolveOpponentWeaponDamage() — opponent weapon should not fire if game is already over.
3. **Verify**: Each check calls GameEndWindow and returns early.

## Inputs

- None specified.

## Expected Output

- `NewGame.UI/ViewModels/MainViewModel.cs`

## Verification

grep -n "GameEndWindow\|OpponentHealth <= 0" MainViewModel.cs shows all game-over guards with 5 checks total
