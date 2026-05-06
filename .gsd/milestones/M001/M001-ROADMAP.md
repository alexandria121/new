# M001: Card Object Hierarchy + Battle Foundation

**Vision:** Establish the core card object architecture in GameMaker and a basic battle room where cards can be played, combined, and resolved. This is the skeleton everything else hangs off — card objects with working properties, a live battle field, and the card database hooked up and queryable.

## Success Criteria

- Cards appear in hand with correct name/type/cost/power/health
- Cards can be dragged to creature slots
- Drag-combine produces correct combo cards
- Combat resolves each turn
- AI opponent takes turns
- Game ends with correct result

## Slices

- [x] **S01: S01** `risk:medium` `depends:[]`
  > After this: After S01: player can see their hand of cards, play a creature to a slot, and end a turn

- [ ] **S02: Battle + Turn Structure** `risk:low` `depends:[S01]`
  > After this: After S02: creatures attack when turn ends, damage is applied, dead creatures are removed

- [ ] **S03: Card Combining** `risk:medium` `depends:[S01]`
  > After this: After S03: dragging one card over another in hand produces a combo card if recipe exists

- [ ] **S04: AI Opponent + Win/Lose** `risk:medium` `depends:[S01,S02]`
  > After this: After S04: AI character takes a turn after player ends turn, game ends when health reaches 0

## Boundary Map

```
GameMaker Project
├── rooms/
│   └── BattleRoom          ← main battle UI
├── objects/
│   ├── obj_card_base       ← abstract parent for all cards
│   ├── obj_card_creature   ← creature cards
│   ├── obj_card_spell      ← spell cards
│   ├── obj_card_artifact   ← artifact cards
│   ├── obj_card_event      ← event cards
│   ├── obj_card_weapon     ← weapon cards
│   ├── obj_card_armor      ← armor cards
│   ├── obj_card_blank      ← unknown/missing card
│   └── obj_card_combo      ← combo-created cards
├── scripts/
│   ├── scr_card_database   ← JSON loading + ds_map storage
│   ├── scr_card_defs       ← base card definitions
│   ├── scr_combo_lookup    ← recipe matching
│   ├── scr_deck_manager    ← draw/hand/discard pile logic
│   ├── scr_battle_logic    ← combat resolution
│   ├── scr_ai_opponent     ← AI decision making
│   └── scr_constants       ← shared constants
└── data/
    └── cards.json           ← exported card data
```
