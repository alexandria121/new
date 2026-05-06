# S02: Battle + Turn Structure

**Goal:** After this slice, creatures placed in slots attack when the player ends their turn, damage is correctly applied to opposing creatures or the opponent directly, dead creatures are removed, and the game correctly detects win/lose conditions when health reaches 0.
**Demo:** After S02: creatures attack when turn ends, damage is applied, dead creatures are removed

## Must-Haves

- Creatures in player slots (6-11) attack opponent creatures in slots (0-5) when End Turn is clicked
- Damage is applied to opposing creatures first; if no opposing creature, damage goes direct to opponent Health
- Dead creatures (Health <= 0) are removed from slots
- Player mana refills to PlayerMaxMana (not +2 flat) at start of each turn
- GameEndWindow appears immediately when OpponentHealth or PlayerHealth reaches 0
- Opponent weapon deals damage to player after opponent combat
- TurnCount increments correctly after each full round

## Proof Level

- This slice proves: fixture

## Integration Closure

Integration with MainViewModel EndTurn() is verified by unit tests. Full end-to-end UI test requires manual testing in WPF.

## Verification

- Debug messages already exist in all scripts via show_debug_message equivalent (LogToFile)
- Battle log entries for every combat action (AddBattleLog)
- No new logging needed — existing observability surfaces cover combat resolution

## Tasks

- [ ] **T01: Fix Mana Refill and Opponent Weapon Damage** `est:30m`
  The EndTurn() flow is mostly wired but has two correctness bugs:
  - Files: `NewGame.UI/ViewModels/MainViewModel.cs`
  - Verify: grep -n "PlayerMana = Math.Min(PlayerMana + 2" MainViewModel.cs shows the bug; after fix, grep shows PlayerMana = PlayerMaxMana

- [ ] **T02: Add GameEndWindow and EndTurn Game-Over Guards** `est:45m`
  EndTurn() calls Application.Current.Dispatcher.Invoke to show GameEndWindow after player combat, player weapon, and opponent turn. Each check is redundant (if/else if/else if chain). Also add game-over guards after opponent combat and before incrementing TurnCount. Also ensure ResolveOpponentWeaponDamage() properly decrements OpponentHealth and triggers the game-end check.
  - Files: `NewGame.UI/ViewModels/MainViewModel.cs`
  - Verify: grep -n "GameEndWindow\|OpponentHealth <= 0" MainViewModel.cs shows all game-over guards with 5 checks total

- [ ] **T03: Add Unit Tests for Combat Resolution** `est:1h`
  Write unit tests that verify:
  1. EndTurn triggers combat resolution
  2. Creature attacks apply damage to opposing creature or direct to opponent health
  3. Dead creatures (Health <= 0) are removed from slots
  4. Game ends when OpponentHealth or PlayerHealth reaches 0
  5. Mana refills to max on new turn
  6. Turn count increments after each complete round
  - Files: `Tests/BattleResolutionTests.cs`
  - Verify: dotnet test --filter BattleResolutionTests

## Files Likely Touched

- NewGame.UI/ViewModels/MainViewModel.cs
- Tests/BattleResolutionTests.cs
