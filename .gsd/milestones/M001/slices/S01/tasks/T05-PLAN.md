---
estimated_steps: 12
estimated_files: 2
skills_used: []
---

# T05: Turn structure + end turn

1. obj_battle_controller End Turn button (or press T key)
2. On end turn:
   - Increment turn counter
   - Increment player max_mana (+1 per turn, cap at 10)
   - Refill player mana to max_mana
   - Draw 1 card from deck (if cards remain)
   - Trigger opponent turn
3. Turn indicator UI:
   - Shows Turn X
   - Highlights active player
4. End turn button or T key to end turn
5. Visual turn transition (brief flash or text)

## Inputs

- `MagicalDeckbuilder.Shared/Game/Game.cs (turn structure)`

## Expected Output

- `End turn button advances turn`
- `Mana refills each turn`
- `One card drawn per turn end`

## Verification

End turn 3 times, verify mana = 3, hand size increases by 3 cards total
