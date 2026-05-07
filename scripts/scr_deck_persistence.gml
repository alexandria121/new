// ============================================================
// scr_deck_persistence.gml
// Deck save/load via JSON text file in the game save directory
// ============================================================

#macro DECK_SAVE_FILE "deck_save.json"

// --------------------------------------------------------
// SaveDeckToFile(deckList)
// Serializes a ds_list of card structs to a JSON file.
// Returns: true on success, false on failure
// --------------------------------------------------------
function SaveDeckToFile(deckList) {
    var path = DECK_SAVE_FILE;
    var saveStruct = {
        version: 1,
        cardIds: []
    };
    
    var count = ds_list_size(deckList);
    var ids = [];
    for (var i = 0; i < count; i++) {
        var card = ds_list_find_value(deckList, i);
        var cardId = is_struct(card) ? struct_get(card, "id") : (ds_map_exists(card, "id") ? card[? "id"] : "unknown");
        ids[i] = cardId;
    }
    saveStruct.cardIds = ids;
    
    var jsonStr = json_stringify(saveStruct);
    var file = file_text_open_write(path);
    if (file == -1) {
        show_debug_message("[DeckPersistence] SaveDeckToFile: failed to open " + path + " for writing");
        return false;
    }
    file_text_write_string(file, jsonStr);
    file_text_close(file);
    show_debug_message("[DeckPersistence] SaveDeckToFile: saved " + string(count) + " cards to " + path);
    return true;
}

// --------------------------------------------------------
// LoadDeckFromFile()
// Deserializes a JSON file back into global.player_deck ds_list.
// Returns: true on success, false on failure
// --------------------------------------------------------
function LoadDeckFromFile() {
    var path = DECK_SAVE_FILE;
    if (!file_exists(path)) {
        show_debug_message("[DeckPersistence] LoadDeckFromFile: " + path + " not found");
        return false;
    }
    
    var raw = file_text_to_string(path);
    var parsed = json_decode(raw);
    if (is_undefined(parsed)) {
        show_debug_message("[DeckPersistence] LoadDeckFromFile: json_decode failed for " + path);
        return false;
    }
    
    // Support both struct (GMS 2023+) and ds_map (legacy)
    var idsArray;
    if (is_struct(parsed)) {
        idsArray = struct_get(parsed, "cardIds");
    } else {
        idsArray = parsed[? "cardIds"];
    }
    
    if (!is_array(idsArray)) {
        show_debug_message("[DeckPersistence] LoadDeckFromFile: 'cardIds' not found or not array");
        if (is_struct(parsed)) ds_map_destroy(parsed);
        else ds_map_destroy(parsed);
        return false;
    }
    
    // Populate global.player_deck from card IDs
    ds_list_clear(global.player_deck);
    for (var i = 0; i < array_length(idsArray); i++) {
        var cardId = idsArray[i];
        var cardData = is_struct(global.base_cards) ? struct_get(global.base_cards, cardId) : global.base_cards[? cardId];
        if (cardData != undefined) {
            ds_list_add(global.player_deck, cardData);
        } else {
            show_debug_message("[DeckPersistence] LoadDeckFromFile: card not found in base_cards: " + string(cardId));
        }
    }
    
    if (is_struct(parsed)) ds_map_destroy(parsed);
    else ds_map_destroy(parsed);
    
    show_debug_message("[DeckPersistence] LoadDeckFromFile: loaded " + string(ds_list_size(global.player_deck)) + " cards from " + path);
    return true;
}

// --------------------------------------------------------
// InitDefaultDeck()
// Creates a default 22-card deck from global.base_cards.
// Populates global.player_deck with one copy of each card.
// --------------------------------------------------------
function InitDefaultDeck() {
    if (ds_exists(global.player_deck, ds_type_list)) {
        ds_list_destroy(global.player_deck);
    }
    global.player_deck = ds_list_create();
    
    if (is_struct(global.base_cards)) {
        var keys = struct_get_names(global.base_cards);
        for (var i = 0; i < array_length(keys); i++) {
            var card = struct_get(global.base_cards, keys[i]);
            ds_list_add(global.player_deck, card);
        }
    } else {
        var keys = ds_map_keys_to_array(global.base_cards);
        for (var i = 0; i < array_length(keys); i++) {
            var card = global.base_cards[? keys[i]];
            ds_list_add(global.player_deck, card);
        }
    }
    
    show_debug_message("[DeckPersistence] InitDefaultDeck: created default deck with " + string(ds_list_size(global.player_deck)) + " cards");
}