---
estimated_steps: 22
estimated_files: 17
skills_used: []
---

# T01: Project structure + card database

1. Create GameMaker project structure:
   - rooms/BattleRoom (800x600, the main battle room)
   - scripts/scr_constants.gml (mana, turn, slot positions)
2. Create placeholder card sprites:
   - spr_card_creature, spr_card_spell, spr_card_artifact, spr_card_event, spr_card_weapon, spr_card_armor (colored rectangles 80x120)
3. Create obj_card_base:
   - Stores card_id, card_name, card_type, mana_cost, power, health, element, rarity, is_combo
   - Sprite changes based on type
   - Displays name, cost in room
   - Mouse events for drag
4. Create child objects for each card type
5. Create scr_card_database:
   - Loads base cards (22 creatures) from JSON at startup
   - Stores in ds_map keyed by card_id
6. Create obj_battle_controller:
   - Manages player/opponent state
   - Handles draw pile, hand, discard
   - Starting hand of 4 cards drawn at game start
7. Create obj_creature_slot (x6 player side, x6 opponent side):
   - Shows slot background
   - Accepts dropped cards
   - Visual feedback on valid/invalid drop

## Inputs

- `MagicalDeckbuilder.Shared/Cards/Card.cs`
- `MagicalDeckbuilder.Shared/Game/CardDatabase.cs`

## Expected Output

- `GameMaker project with BattleRoom, card objects, creature slots, and placeholder sprites`
- `Card database loads at startup`
- `Player hand displays 4 starting cards`

## Verification

Open BattleRoom, verify 4 cards appear in player hand
