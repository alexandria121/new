---
estimated_steps: 12
estimated_files: 2
skills_used: []
---

# T03: Battle controller + game state

1. obj_battle_controller Create event:
   - Initialize player character (health=30, mana=0, max_mana=0)
   - Initialize opponent character
   - Create creature slots (6 player, 6 opponent)
   - Load card database
   - Initialize deck (create copies of each base card, shuffle)
   - Draw 4 starting cards to hand
2. obj_battle_controller Step event:
   - Check for game over (health <= 0)
   - Update UI elements (health bars, mana display)
3. Draw pile UI: show top card face-down with count
4. Discard pile UI: show top card face-up with count

## Inputs

- `MagicalDeckbuilder.Shared/Decks/DeckManager.cs`
- `MagicalDeckbuilder.Shared/Game/Game.cs`

## Expected Output

- `Battle controller manages game state`
- `Draw pile, hand, discard pile all functional`
- `Starting hand of 4 cards drawn from deck`

## Verification

Restart game, verify hand always has 4 cards after draw
