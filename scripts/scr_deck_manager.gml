// ============================================================
// scr_deck_manager.gml
// Deck management and pile operations for Magical Deckbuilder
// ============================================================

// --------------------------------------------------------
// Enums (simulated with constants)
// --------------------------------------------------------
#macro DRAW_PILE 0
#macro HAND 1
#macro DISCARD_PILE 2
#macro IN_PLAY 3
#macro CREATURE_SLOTS 4

#macro MAX_CREATURE_SLOTS 6
#macro MAX_HAND_SIZE 7
#macro STARTING_HAND_SIZE 4

// --------------------------------------------------------
// Global deck state
// --------------------------------------------------------
global.draw_pile = ds_list_create();
global.hand = ds_list_create();
global.discard_pile = ds_list_create();
global.in_play = ds_list_create();
global.creature_slots = array_create(MAX_CREATURE_SLOTS);

// Initialize creature slots
for (var i = 0; i < MAX_CREATURE_SLOTS; i++) {
    global.creature_slots[i] = noone; // noone = null in GML
}

// ============================================================
// InitializeDeck(cards)
// Clears all piles, populates draw pile, shuffles
// ============================================================
function InitializeDeck(cards) {
    show_debug_message("[DeckManager] InitializeDeck: starting");
    
    // Clear all piles
    ds_list_clear(global.draw_pile);
    ds_list_clear(global.hand);
    ds_list_clear(global.discard_pile);
    ds_list_clear(global.in_play);
    
    // Clear creature slots
    for (var i = 0; i < MAX_CREATURE_SLOTS; i++) {
        global.creature_slots[i] = noone;
    }
    
    // Populate draw pile
    if (is_array(cards)) {
        for (var i = 0; i < array_length(cards); i++) {
            ds_list_add(global.draw_pile, cards[i]);
        }
    } else if (ds_list_size(cards) > 0) {
        ds_list_copy(global.draw_pile, cards);
    }
    
    // Shuffle draw pile
    ShuffleDrawPile();
    
    show_debug_message("[DeckManager] InitializeDeck: deck initialized with " + string(ds_list_size(global.draw_pile)) + " cards");
}

// ============================================================
// ShuffleDrawPile()
// Fisher-Yates shuffle on ds_list
// ============================================================
function ShuffleDrawPile() {
    var n = ds_list_size(global.draw_pile);
    if (n <= 1) return;
    
    for (var i = n - 1; i > 0; i--) {
        var j = irandom(i);
        var temp = ds_list_find_value(global.draw_pile, i);
        ds_list_replace(global.draw_pile, i, ds_list_find_value(global.draw_pile, j));
        ds_list_replace(global.draw_pile, j, temp);
    }
}

// ============================================================
// DrawCards(count)
// Pops cards from draw pile to hand. Reshuffles discard if needed.
// Returns: number of cards actually drawn
// ============================================================
function DrawCards(count) {
    var drawn = 0;
    show_debug_message("[DeckManager] DrawCards: requested " + string(count));
    
    for (var i = 0; i < count; i++) {
        // Check hand size limit
        if (ds_list_size(global.hand) >= MAX_HAND_SIZE) {
            show_debug_message("[DeckManager] DrawCards: hand is full (max " + string(MAX_HAND_SIZE) + ")");
            break;
        }
        
        // Draw pile empty — try to reshuffle from discard
        if (ds_list_empty(global.draw_pile)) {
            if (ds_list_size(global.discard_pile) > 0) {
                show_debug_message("[DeckManager] DrawCards: draw pile empty, reshuffling discard pile");
                ds_list_copy(global.draw_pile, global.discard_pile);
                ds_list_clear(global.discard_pile);
                ShuffleDrawPile();
                show_debug_message("[DeckManager] DrawCards: reshuffle complete — "
                    + string(ds_list_size(global.draw_pile)) + " cards in draw pile");
            } else {
                // BOTH piles are empty — player has no cards left to draw
                show_debug_message("[DeckManager] DrawCards: pile_empty — "
                    + "draw pile AND discard pile are both empty — "
                    + "hand has " + string(ds_list_size(global.hand)) + " cards, "
                    + "cannot draw more");
                // Emit a named event so the UI layer can show feedback
                if (global.game_phase != undefined) {
                    // Let battle controller know player is out of cards
                    show_debug_message("[DeckManager] player_out_of_cards");
                }
                break;
            }
        }
        
        // Pull top card from draw pile
        if (!ds_list_empty(global.draw_pile)) {
            var card = ds_list_find_value(global.draw_pile, ds_list_size(global.draw_pile) - 1);
            ds_list_delete(global.draw_pile, ds_list_size(global.draw_pile) - 1);
            ds_list_add(global.hand, card);
            drawn++;
            show_debug_message("[DeckManager] DrawCards: drew " + string(card[? "name"]) + " — hand now has " + string(ds_list_size(global.hand)) + " cards");
        }
    }
    
    return drawn;
}

// ============================================================
// PlayCard(cardId)
// Find card in hand, move to in-play pile
// Returns: true if successful, false otherwise
// ============================================================
function PlayCard(cardId) {
    var handSize = ds_list_size(global.hand);
    
    for (var i = 0; i < handSize; i++) {
        var card = ds_list_find_value(global.hand, i);
        if (card[? "id"] == cardId) {
            ds_list_delete(global.hand, i);
            ds_list_add(global.in_play, card);
            show_debug_message("[DeckManager] PlayCard: played " + string(card[? "name"]) + " to in-play");
            return true;
        }
    }
    
    show_debug_message("[DeckManager] PlayCard: card not found in hand: " + string(cardId));
    return false;
}

// ============================================================
// PlayCreatureToSlot(cardId, slotIndex)
// Validates creature type + empty slot, moves from hand to slot
// Returns: true if successful, -1 if no slot available, false otherwise
// ============================================================
function PlayCreatureToSlot(cardId, slotIndex) {
    var handSize = ds_list_size(global.hand);
    var card = noone;
    var handIdx = -1;
    
    // Find card in hand
    for (var i = 0; i < handSize; i++) {
        var c = ds_list_find_value(global.hand, i);
        if (c[? "id"] == cardId) {
            card = c;
            handIdx = i;
            break;
        }
    }
    
    if (card == noone) {
        show_debug_message("[DeckManager] PlayCreatureToSlot: card not found in hand: " + string(cardId));
        return false;
    }
    
    // Validate creature type (type=1 is creature based on card enums)
    if (card[? "type"] != 1) {
        show_debug_message("[DeckManager] PlayCreatureToSlot: card is not a creature: " + string(cardId));
        return false;
    }
    
    // Validate slot index
    if (slotIndex < 0 || slotIndex >= MAX_CREATURE_SLOTS) {
        show_debug_message("[DeckManager] PlayCreatureToSlot: invalid slot index: " + string(slotIndex));
        return false;
    }
    
    // Check slot is empty
    if (global.creature_slots[slotIndex] != noone) {
        show_debug_message("[DeckManager] PlayCreatureToSlot: slot already occupied: " + string(slotIndex));
        return -1; // -1 signals "slot full"
    }
    
    // Move from hand to slot
    ds_list_delete(global.hand, handIdx);
    global.creature_slots[slotIndex] = card;
    ds_list_add(global.in_play, card);
    
    show_debug_message("[DeckManager] PlayCreatureToSlot: played " + string(card[? "name"]) + " to slot " + string(slotIndex));
    return true;
}

// ============================================================
// GetCreatureInSlot(slotIndex)
// Returns: card struct or noone if slot is empty / invalid
// ============================================================
function GetCreatureInSlot(slotIndex) {
    if (slotIndex < 0 || slotIndex >= MAX_CREATURE_SLOTS) {
        return noone;
    }
    return global.creature_slots[slotIndex];
}

// ============================================================
// RemoveCreatureFromSlot(slotIndex)
// Moves creature from slot to discard pile
// ============================================================
function RemoveCreatureFromSlot(slotIndex) {
    if (slotIndex < 0 || slotIndex >= MAX_CREATURE_SLOTS) {
        show_debug_message("[DeckManager] RemoveCreatureFromSlot: invalid slot index: " + string(slotIndex));
        return;
    }
    
    var creature = global.creature_slots[slotIndex];
    if (creature == noone) {
        show_debug_message("[DeckManager] RemoveCreatureFromSlot: slot is empty: " + string(slotIndex));
        return;
    }
    
    // Remove from in_play
    var inPlaySize = ds_list_size(global.in_play);
    for (var i = 0; i < inPlaySize; i++) {
        var c = ds_list_find_value(global.in_play, i);
        if (c[? "id"] == creature[? "id"]) {
            ds_list_delete(global.in_play, i);
            break;
        }
    }
    
    // Move to discard
    ds_list_add(global.discard_pile, creature);
    global.creature_slots[slotIndex] = noone;
    
    show_debug_message("[DeckManager] RemoveCreatureFromSlot: removed creature from slot " + string(slotIndex) + " to discard");
}

// ============================================================
// GetPileCount(pileType)
// Returns: count of cards in the specified pile
// ============================================================
function GetPileCount(pileType) {
    switch (pileType) {
        case DRAW_PILE:
            return ds_list_size(global.draw_pile);
        case HAND:
            return ds_list_size(global.hand);
        case DISCARD_PILE:
            return ds_list_size(global.discard_pile);
        case IN_PLAY:
            return ds_list_size(global.in_play);
        case CREATURE_SLOTS:
            var count = 0;
            for (var i = 0; i < MAX_CREATURE_SLOTS; i++) {
                if (global.creature_slots[i] != noone) count++;
            }
            return count;
        default:
            return 0;
    }
}

// ============================================================
// Debug display functions
// ============================================================

// DisplayHand()
// Returns a string showing all cards in hand with indices
function DisplayHand() {
    var handSize = ds_list_size(global.hand);
    if (handSize == 0) return "(Empty Hand)";
    
    var output = "";
    for (var i = 0; i < handSize; i++) {
        var card = ds_list_find_value(global.hand, i);
        output += string(i + 1) + ". " + string(card[? "name"]) + " | " + string(card[? "mana_cost"]) + " Mana | " + card_type_to_string(card[? "type"]) + "\n";
    }
    return output;
}

// DisplayInPlay()
// Returns a string showing creature slots and in-play cards
function DisplayInPlay() {
    var output = "";
    
    // Creature slots
    output += "--- Creature Slots ---\n";
    for (var i = 0; i < MAX_CREATURE_SLOTS; i++) {
        var creature = global.creature_slots[i];
        if (creature != noone) {
            output += "[" + string(i + 1) + "] " + string(creature[? "name"]) + " | PWR:" + string(creature[? "power"]) + " HP:" + string(creature[? "health"]) + "\n";
        } else {
            output += "[" + string(i + 1) + "] (Empty)\n";
        }
    }
    
    // Other in-play cards
    var inPlaySize = ds_list_size(global.in_play);
    if (inPlaySize > 0) {
        output += "--- In Play ---\n";
        for (var i = 0; i < inPlaySize; i++) {
            var card = ds_list_find_value(global.in_play, i);
            output += "• " + string(card[? "name"]) + " (" + card_type_to_string(card[? "type"]) + ")\n";
        }
    }
    
    return (output == "") ? "(Nothing in play)" : output;
}

// DisplayPile(pileType)
// Returns a string listing cards in a pile
function DisplayPile(pileType) {
    var pile = noone;
    switch (pileType) {
        case DRAW_PILE:     pile = global.draw_pile;     break;
        case HAND:          pile = global.hand;          break;
        case DISCARD_PILE:  pile = global.discard_pile; break;
        case IN_PLAY:       pile = global.in_play;       break;
    }
    
    if (pile == noone || ds_list_empty(pile)) return "(Empty pile)";
    
    var output = "";
    var sz = ds_list_size(pile);
    for (var i = 0; i < sz; i++) {
        var card = ds_list_find_value(pile, i);
        output += string(i + 1) + ". " + string(card[? "name"]) + "\n";
    }
    return output;
}

// card_type_to_string(type)
// Helper to convert card type enum to readable string
function card_type_to_string(type) {
    switch (type) {
        case 0: return "Spell";
        case 1: return "Creature";
        case 2: return "Enchantment";
        case 3: return "Artifact";
        case 4: return "Instant";
        default: return "Unknown";
    }
}

// ============================================================
// Cleanup (call when game restarts)
// ============================================================
function DeckManagerCleanup() {
    ds_list_destroy(global.draw_pile);
    ds_list_destroy(global.hand);
    ds_list_destroy(global.discard_pile);
    ds_list_destroy(global.in_play);
}
