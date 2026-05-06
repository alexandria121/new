---
estimated_steps: 3
estimated_files: 1
skills_used: []
---

# T01: Fix Mana Refill and Opponent Weapon Damage

The EndTurn() flow is mostly wired but has two correctness bugs:

1. **Mana refill**: Player mana refills by +2 each turn (line 2044), but the game rule is to refill to max mana (PlayerMaxMana), where PlayerMaxMana = min(turn, 10). Currently: `PlayerMana = Math.Min(PlayerMana + 2, 10)` should become `PlayerMana = PlayerMaxMana`.
2. **Opponent weapon damage after combat**: ResolveOpponentWeaponDamage() is called after ResolveOpponentCombat() in ExecuteOpponentTurn() (line 2960), but the player's weapon ResolvePlayerWeaponDamage() is called BEFORE the opponent's turn in EndTurn() (line 2008). Per game rules, weapons deal damage at end of owner's turn, so player weapon fires before opponent's combat, opponent weapon fires after opponent's combat. This is already correct — no change needed.

## Inputs

- None specified.

## Expected Output

- `NewGame.UI/ViewModels/MainViewModel.cs`

## Verification

grep -n "PlayerMana = Math.Min(PlayerMana + 2" MainViewModel.cs shows the bug; after fix, grep shows PlayerMana = PlayerMaxMana
