# GSD context snapshot (2026-05-06T18:07:41.431Z)

## Top project memories
- [MEM001] (architecture) In MainViewModel.cs, RemoveDeadCreatures() nulls out FieldSlots but does NOT call DeckManager.RemoveCreatureFromSlot() or OpponentDeck.RemoveCreatureFromSlot(). This means the logical model (DeckManager's InPlay list) stays out of sync with the visual model (FieldSlots). No test project exists — no xUnit/NUnit/MSTest in the solution. Any testing task must scaffold a Tests/ directory and .csproj first.
- [MEM002] (gotcha) ExecuteOpponentTurn() has no PlayerHealth <= 0 check after ResolveOpponentCombat() or ResolveOpponentWeaponDamage(). A player can die mid-opponent-turn and the opponent continues playing (drawing cards, AI loop continues). Fix: add game-over guard blocks after each damage step, mirroring EndTurn()'s pattern of checking after player combat, weapon, and opponent turn.
- [MEM003] (architecture) Game-over guards in MainViewModel.cs should be sequential `if` blocks (not `else if`), so each damage step independently triggers the game-end check
- [MEM004] (pattern) RollForward=Major in test .csproj allows net9.0-windows tests to run against net8.0 host runtime
- [MEM005] (gotcha) GameEndWindow.ShowDialog() called via Dispatcher.Invoke throws InvalidOperationException in headless test environment — must be caught and ignored in unit tests

## Recent gsd_exec runs
- [3b357553-7abb-4cca-807b-3e56da2320ce] bash exit:127 — Run battle resolution tests
- [e5f97831-6f58-4f61-bc9e-ff728b12aff1] bash exit:1 — Verify game-over guard locations
- [4a5176f1-167c-4b46-8330-80b77c94578a] bash exit:1 — Verify mana refill fix in EndTurn
