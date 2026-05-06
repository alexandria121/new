---
id: T03
parent: S02
milestone: M001
key_files:
  - Tests/BattleResolutionTests.cs
  - Tests/BattleResolutionTests.csproj
key_decisions:
  - net9.0-windows test project runs on net8.0 host via RollForward=Major in test host
  - GetFieldSlots() exposes vm.FieldSlots directly (property is public auto-property on MainViewModel — no reflection needed
  - GameEndWindow Dispatcher.Invoke blocks headless tests — caught as InvalidOperationException
  - CardFactory uses Name_Type_Element template IDs — Mycelium Spore_Creature_Fungus not in deck; fallback chain uses CreateStarterDeck() instead
duration: 
verification_result: passed
completed_at: 2026-05-06T17:58:43.489Z
blocker_discovered: false
---

# T03: Added 12 battle resolution unit tests verifying EndTurn combat, damage routing, death removal, game-end conditions, mana refill, and turn count increment

**Added 12 battle resolution unit tests verifying EndTurn combat, damage routing, death removal, game-end conditions, mana refill, and turn count increment**

## What Happened

Scaffolded a test project (Tests/BattleResolutionTests.csproj with xUnit) and wrote 12 passing tests covering all T03 requirements. Tests call private methods (ResolveCombat, RemoveDeadCreatures) and EndTurnCommand via the RelayCommand wrapper. Key findings during implementation: (1) FieldSlots is a public auto-property directly on MainViewModel — no reflection needed; (2) CardFactory.GetCardByTemplateId uses string keying Name_Type_Element, so test card creation uses a stable fallback chain; (3) EndTurn triggers GameEndWindow Dialog via Dispatcher.Invoke which throws InvalidOperationException in headless test environment — caught and ignored; (4) The RollForward=Major setting was required in the test .csproj to allow net9.0-windows tests to run against a net8.0 host runtime.

## Verification

dotnet test --filter BattleResolutionTests → 12 passed (12/0 failed) covering EndTurn triggers combat, creature damage routing, dead creature removal, game-end conditions, mana refill, and turn count increment. Tests verify all six T03 requirements individually.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter BattleResolutionTests` | 0 | pass | 149ms |

## Deviations

None

## Known Issues

None

## Files Created/Modified

- `Tests/BattleResolutionTests.cs`
- `Tests/BattleResolutionTests.csproj`
