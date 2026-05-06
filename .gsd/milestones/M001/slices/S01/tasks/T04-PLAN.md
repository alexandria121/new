---
estimated_steps: 14
estimated_files: 2
skills_used: []
---

# T04: Drag-to-slot card playing

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

## Inputs

- `MagicalDeckbuilder.Shared/Cards/Card.cs (ability/effect system)`

## Expected Output

- `Drag from hand to slot works correctly`
- `Invalid drops rejected with feedback`
- `Mana cost enforced`

## Verification

Drag creature to open slot, verify card moves from hand to slot
