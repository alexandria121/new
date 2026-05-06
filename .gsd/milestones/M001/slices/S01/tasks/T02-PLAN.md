---
estimated_steps: 4
estimated_files: 2
skills_used: []
---

# T02: Export card data from C# to GameMaker format

1. Create data/cards.json with all 22 base cards exported
2. Export card data: id, name, type, element, mana_cost, power, health, rarity, is_combo, is_quest_card, abilities
3. Abilities array: { mana_cost, effect_type, target_type, requires_target, is_passive, description }
4. Export to scripts/scr_card_defs.gml as GML-native syntax

## Inputs

- `MagicalDeckbuilder.Shared/Cards/Card.cs`
- `MagicalDeckbuilder.Shared/Game/CardDatabase.cs`

## Expected Output

- `data/cards.json with all 22 base cards`
- `scr_card_defs.gml with GML card definitions`

## Verification

Verify all 22 cards load correctly with json_decode
