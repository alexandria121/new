---
estimated_steps: 8
estimated_files: 1
skills_used: []
---

# T03: Add Unit Tests for Combat Resolution

Write unit tests that verify:
1. EndTurn triggers combat resolution
2. Creature attacks apply damage to opposing creature or direct to opponent health
3. Dead creatures (Health <= 0) are removed from slots
4. Game ends when OpponentHealth or PlayerHealth reaches 0
5. Mana refills to max on new turn
6. Turn count increments after each complete round

Place tests in: `Tests/BattleResolutionTests.cs`

## Inputs

- None specified.

## Expected Output

- `Tests/BattleResolutionTests.cs`

## Verification

dotnet test --filter BattleResolutionTests
