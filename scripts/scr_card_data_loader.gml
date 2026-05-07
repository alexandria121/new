/// scr_card_data_loader.gml
/// Loads CardDatabase.json and combos.json into global.ds_maps.
/// Call load_all_card_data() once at game start.

global.base_cards = ds_map_create();
global.combo_registry = ds_map_create();
global.cards_loaded = false;

/// load_all_card_data()
/// Returns: void
/// Loads base cards and combo recipes from JSON files.
/// On success sets global.cards_loaded = true.
/// On failure logs json_load_error and leaves cards_loaded as false.
function load_all_card_data() {
    // ── Load base cards ──────────────────────────────────────────────────────────
    var card_json_path = "datafiles/CardDatabase.json";
    if (!file_exists(card_json_path)) {
        show_debug_message("json_load_error: CardDatabase.json not found at " + card_json_path);
        return;
    }

    var card_raw = file_text_to_string(card_json_path);
    var card_data = json_decode(card_raw);
    if (is_undefined(card_data) || !ds_map_exists(card_data, "cards")) {
        show_debug_message("json_load_error: CardDatabase.json failed to parse or missing 'cards' key");
        if (!is_undefined(card_data)) ds_map_destroy(card_data);
        return;
    }

    var card_list = card_data[? "cards"];
    var card_count = ds_list_size(card_list);
    for (var i = 0; i < card_count; i++) {
        var entry = ds_list_find_value(card_list, i);
        var card_id = is_struct(entry) ? struct_get(entry, "id") : (ds_map_exists(entry, "id") ? entry[? "id"] : undefined);
        if (!is_undefined(card_id)) {
            ds_map_add(global.base_cards, card_id, entry);
        }
    }
    ds_map_destroy(card_data);

    // ── Load combo recipes ─────────────────────────────────────────────────────
    var combo_json_path = "datafiles/combos.json";
    if (!file_exists(combo_json_path)) {
        show_debug_message("json_load_error: combos.json not found at " + combo_json_path);
        return;
    }

    var combo_raw = file_text_to_string(combo_json_path);
    var combo_data = json_decode(combo_raw);
    if (is_undefined(combo_data) || !ds_map_exists(combo_data, "combos")) {
        show_debug_message("json_load_error: combos.json failed to parse or missing 'combos' key");
        if (!is_undefined(combo_data)) ds_map_destroy(combo_data);
        return;
    }

    // combos.json uses "combos" array, not "combo_recipes"
    var combo_list = combo_data[? "combos"];
    var combo_count = ds_list_size(combo_list);
    for (var j = 0; j < combo_count; j++) {
        var recipe = ds_list_find_value(combo_list, j);

        // Support both struct (GMS 2023+) and ds_map (legacy) formats
        var id1, id2;
        if (is_struct(recipe)) {
            id1 = struct_get(recipe, "card1Id");
            id2 = struct_get(recipe, "card2Id");
        } else {
            id1 = ds_map_exists(recipe, "card1Id") ? recipe[? "card1Id"] : undefined;
            id2 = ds_map_exists(recipe, "card2Id") ? recipe[? "card2Id"] : undefined;
        }

        if (!is_undefined(id1) && !is_undefined(id2)) {
            var template_id = 10000 + min(id1, id2) * 1000 + max(id1, id2);
            ds_map_add(global.combo_registry, template_id, recipe);
        }
    }
    ds_map_destroy(combo_data);

    global.cards_loaded = true;
    show_debug_message("load_all_card_data: loaded " + string(card_count) + " base cards and " + string(combo_count) + " combo recipes");
}
