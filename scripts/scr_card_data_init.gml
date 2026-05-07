/// scr_card_data_init.gml
/// Card and combo lookup functions.
/// These are populated by scr_card_data_loader.gml at game start.
/// Call load_all_card_data() before using any of these.

/// GetCard(card_id)
/// Returns: ds_map or struct for the base card with given ID.
/// Returns: undefined if card not found.
function GetCard(card_id) {
    if (!global.cards_loaded) {
        show_debug_message("GetCard called before cards loaded");
        return undefined;
    }
    if (ds_map_exists(global.base_cards, card_id)) {
        return global.base_cards[? card_id];
    }
    return undefined;
}

/// GetCombo(card1_id, card2_id)
/// Returns: ds_map or struct for the combo recipe combining the two cards.
/// Returns: undefined if no combo exists for this pair.
/// Key is 10000 + min(id1,id2)*1000 + max(id1,id2).
function GetCombo(card1_id, card2_id) {
    if (!global.cards_loaded) {
        show_debug_message("GetCombo called before cards loaded");
        return undefined;
    }
    var key = 10000 + min(card1_id, card2_id) * 1000 + max(card1_id, card2_id);
    if (ds_map_exists(global.combo_registry, key)) {
        return global.combo_registry[? key];
    }
    return undefined;
}

/// GetCardName(card_id)
/// Returns: string card name, or "Unknown Card" if not found.
function GetCardName(card_id) {
    var card = GetCard(card_id);
    if (is_undefined(card)) return "Unknown Card";
    if (is_struct(card)) {
        return struct_get(card, "name");
    }
    return card[? "name"];
}

/// GetCardCount()
/// Returns: number of base cards loaded.
function GetCardCount() {
    if (!global.cards_loaded) return 0;
    return ds_map_size(global.base_cards);
}

/// GetComboCount()
/// Returns: number of combo recipes loaded.
function GetComboCount() {
    if (!global.cards_loaded) return 0;
    return ds_map_size(global.combo_registry);
}
