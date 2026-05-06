---
id: S02
parent: M001
milestone: M001
provides:
  - Combat resolution (creature-vs-creature and creature-vs-face damage)
  - Dead creature removal (Health <= 0)
  - Game-end detection on health reaching 0
  - Mana refill to PlayerMaxMana each turn
  - TurnCount increment after each complete round
  - Opponent weapon damage to player health
  - 12 unit tests validating combat resolution logic
requires:
  - slice: slice: S01
    provides: 
  - slice: provides: Card objects (obj_card_creature), hand management, creature slot placement (slots 6-11), EndTurn button hook
    provides: 
affects:
  - S03
  - S04
key_files:
  - NewGame.UI/ViewModels/MainViewModel.cs
  - Tests/BattleResolutionTests.cs
  - Tests/BattleResolutionTests.csproj
key_decisions:
  - Game-over guards must be sequential `if` blocks (not `else if`) so each damage step independently triggers the game-end check — verified in EndTurn() and ExecuteOpponentTurn()
  - RollForward=Major in test .csproj allows net9.0-windows tests to run on net8.0 host without runtime upgrade
  - GameEndWindow.ShowDialog() via Dispatcher.Invoke throws InvalidOperationException in headless tests — caught and ignored in test code
  - FieldSlots is a public auto-property on MainViewModel — no reflection needed for test access
patterns_established:
  - Game-over guards as independent `if` blocks for immediate game-end detection
  - Mana refill to PlayerMaxMana (variable cap) not flat +2
  - Headless unit test access via public auto-properties (FieldSlots)
  - Unit test isolation from UI Dispatcher via exception catching
observability_surfaces:
  - AddBattleLog entries for all combat actions (player combat, player weapon, opponent combat, opponent weapon, dead creature removal)
  - show_debug_message equivalent via LogToFile for battle events
  - GameEndWindow shows immediately on health <= 0
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-06T18:03:39.144Z
blocker_discovered: false
---

# S02: Battle + Turn Structure

**Battle + turn structure complete: mana refills to max, creatures attack and die correctly, game ends on health=0, 12 unit tests passing**

## What Happened

S02 completed all three tasks successfully. T01 fixed the mana refill bug: changed `Math.Min(PlayerMana + 2, 10)` to `PlayerMaxMana` at the start of the new turn, ensuring player mana refills to their maximum pool (which scales with turns) rather than a flat +2. T02 added the missing mid-combat game-over guard in ExecuteOpponentTurn() between ResolveOpponentCombat() and ResolveOpponentWeaponDamage(), completing the 5-guard game-over coverage: 3 in EndTurn() (after player combat, after player weapon, after opponent turn) and 2 in ExecuteOpponentTurn() (after opponent combat, after opponent weapon). The plan's description of EndTurn() needing an "if/else if/else if" restructure was incorrect — EndTurn() was already correct with sequential if blocks. T03 scaffolded a full xUnit test project with 12 passing tests covering all combat resolution scenarios. Key gotchas captured: Dispatcher.Invoke blocking in headless tests, RollForward=Major for cross-TFM test execution, and CardFactory name-keying pattern.

## Verification

grep confirmed mana fix at line 2044 (PlayerMana = PlayerMaxMana); grep confirmed 5 game-over guards (3 in EndTurn + 2 in ExecuteOpponentTurn); dotnet test --filter BattleResolutionTests returned 12 passed (12/0 failed) in 149ms

## Requirements Advanced

None.

## Requirements Validated

- R001 — Creatures attack when turn ends — proven by EndTurn_RaisesCombatAndRefreshesMana test and DamageRouting tests
- R002 — Damage applied to opposing creatures or direct — proven by DamageRouting_CreatureVsCreature, DamageRouting_CreatureVsFace tests
- R003 — Dead creatures removed — proven by DeadCreatureRemoval test

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

The plan described EndTurn() as having a "redundant if/else if/else if chain" that needed fixing — it was actually already correct with sequential `if` blocks. No functional changes were needed there. Only ExecuteOpponentTurn() was missing a mid-combat game-over guard between ResolveOpponentCombat() and ResolveOpponentWeaponDamage().

## Known Limitations

GameEndWindow.Dialog is caught as exception in tests — the full UI appearance and user interaction cannot be verified headlessly; S04 (AI Opponent slice) will provide the complete end-to-end game-over flow

## Follow-ups

None.

## Files Created/Modified

- `{path: 'NewGame.UI/ViewModels/MainViewModel.cs', description: 'Fixed mana refill to use PlayerMaxMana; added mid-combat game-over guard in ExecuteOpponentTurn; confirmed 5 total game-over guards (3 in EndTurn, 2 in ExecuteOpponentTurn)'}` — 
- `{path: 'Tests/BattleResolutionTests.cs', description: '12 passing unit tests covering EndTurn combat, creature damage routing, dead creature removal, game-end conditions, mana refill, and turn count increment'}` — 
- `{path: 'Tests/BattleResolutionTests.csproj', description: 'xUnit test project with RollForward=Major to run net9.0-windows tests on net8.0 host runtime'}` — 
