# S01: Card Object Hierarchy + Hand Display

**Goal:** Build the card object hierarchy and get cards rendering in a hand with placeholder visuals. Cards can be dragged to creature slots. Invalid drops are rejected with feedback.
**Demo:** After S01: player can see their hand of cards, play a creature to a slot, and end a turn

## Must-Haves

- Cards appear in hand with correct name, type, cost, power, health. Dragging a card to an open creature slot plays it. Invalid drops (full slot, wrong card type) are rejected with visual feedback.

## Proof Level

- This slice proves: Visual confirmation in BattleRoom

## Integration Closure

Cards from hand can be dragged to creature slots and played

## Verification

- Card create/destroy events logged to debug console

## Tasks

- [x] **T01: Project structure + card database** `est:3-4 hours`
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
  - Files: `rooms/BattleRoom`, `objects/obj_card_base`, `objects/obj_card_creature`, `objects/obj_card_spell`, `objects/obj_card_artifact`, `objects/obj_card_event`, `objects/obj_card_weapon`, `objects/obj_card_armor`, `objects/obj_card_blank`, `objects/obj_card_combo`, `objects/obj_battle_controller`, `objects/obj_creature_slot`, `objects/obj_player`, `scripts/scr_constants`, `scripts/scr_card_database`, `scripts/scr_card_defs`, `data/cards.json`
  - Verify: Open BattleRoom, verify 4 cards appear in player hand

- [x] **T02: Export card data from C# to GameMaker format** `est:1-2 hours`
  1. Create data/cards.json with all 22 base cards exported
  2. Export card data: id, name, type, element, mana_cost, power, health, rarity, is_combo, is_quest_card, abilities
  3. Abilities array: { mana_cost, effect_type, target_type, requires_target, is_passive, description }
  4. Export to scripts/scr_card_defs.gml as GML-native syntax
  - Files: `data/cards.json`, `scripts/scr_card_defs.gml`
  - Verify: Verify all 22 cards load correctly with json_decode

- [x] **T03: Battle controller + game state** `est:2-3 hours`
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
  - Files: `objects/obj_battle_controller`, `scripts/scr_deck_manager`
  - Verify: Restart game, verify hand always has 4 cards after draw

- [x] **T04: Drag-to-slot card playing** `est:2-3 hours`
  1. Implement drag-and-drop for hand cards:
     - Mouse down on card → begin drag, card follows mouse
     - Card lifts visually (scale up slightly, bring to front)
  2. Implement creature slot dropping:
     - Mouse released over slot → check if slot is empty and card is creature type
     - Valid: card moves to slot, removed from hand
     - Invalid: card snaps back to hand with red flash
  3. Creature slot visual states:
     - Empty: dashed border
     - Occupied: solid border, shows creature card
  4. Implement mana cost check:
     - Cards cost more mana than available → red tint, cannot be dragged
  5. Implement card hover tooltip:
     - Shows name, type, cost, power, health, abilities, effect text
  - Files: `objects/obj_card_base`, `objects/obj_creature_slot`
  - Verify: Drag creature to open slot, verify card moves from hand to slot

- [x] **T05: Turn structure + end turn** `est:1-2 hours`
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
  - Files: `objects/obj_battle_controller`, `scripts/scr_constants`
  - Verify: End turn 3 times, verify mana = 3, hand size increases by 3 cards total

## Files Likely Touched

- rooms/BattleRoom
- objects/obj_card_base
- objects/obj_card_creature
- objects/obj_card_spell
- objects/obj_card_artifact
- objects/obj_card_event
- objects/obj_card_weapon
- objects/obj_card_armor
- objects/obj_card_blank
- objects/obj_card_combo
- objects/obj_battle_controller
- objects/obj_creature_slot
- objects/obj_player
- scripts/scr_constants
- scripts/scr_card_database
- scripts/scr_card_defs
- data/cards.json
- scripts/scr_card_defs.gml
- scripts/scr_deck_manager
