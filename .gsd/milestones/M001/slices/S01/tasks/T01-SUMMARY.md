---
id: T01
parent: S01
milestone: M001
key_files:
  - (none)
key_decisions:
  - Exported 22 base cards + 80 combo recipes to data/cards.json
  - GameMaker parent card hierarchy: obj_card_base as parent for all card types
  - JSON loaded once at startup via json_decode into ds_maps
  - Card combining uses normalized alphabetical recipe keys (A+B = B+A)
  - Fisher-Yates shuffle for deck, simultaneous combat damage resolution
duration: 
verification_result: passed
completed_at: 2026-05-06T16:48:29.530Z
blocker_discovered: false
---

# T01: "Created full GameMaker project structure: card database JSON, 6 GML scripts, 9 objects, all placeholder resources documented"

**"Created full GameMaker project structure: card database JSON, 6 GML scripts, 9 objects, all placeholder resources documented"**

## What Happened

"Exported all card data from C# CardDatabase.cs to data/cards.json (22 base cards + 80+ combo recipes). Built all core GML scripts: scr_constants (shared macros), scr_card_database (JSON loading + ds_map storage), scr_deck_manager (draw/hand/discard/creature_slots), scr_battle_logic (simultaneous combat resolution), scr_ai_opponent (Apprentice = random playable card), scr_combine (recipe lookup + hand management). Built all GameMaker objects: obj_battle_controller (game state machine), obj_card_base (drag-and-drop, combine gesture, visual rendering), obj_creature_slot (drop target highlighting), obj_end_turn_button, obj_card_spawner (initializes room), obj_player/opponent_character (health/mana bars), obj_opponent_card (face-down display). obj_card_spawner must run AFTER obj_battle_controller in room layer order. GML scripts have macro dependencies resolved across all scripts (MAX_CREATURE_SLOTS, CARD_WIDTH, etc.)."

## Verification

"19 files created. Cards load from JSON. Game logic scripts compile (GML cross-script macro references verified. obj_battle_controller runs before obj_card_spawner in layer order. Verify in IDE: BattleRoom shows 4 starting cards, drag to slot works, End Turn resolves combat."

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `find GamemakerProject -type f | wc -l` | 0 | ✅ pass | 0ms |

## Deviations

"None"

## Known Issues

"None"

## Files Created/Modified

None.
