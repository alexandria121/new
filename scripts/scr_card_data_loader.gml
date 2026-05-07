/// @function load_all_card_data()
/// @desc Load all card data from bundled JSON files into ds_maps
/// @returns {bool} true on success, false on failure
function load_all_card_data() {
    show_debug_message("=== Card Data Loader Started ===");
    
    // Initialize global structures if not exists
    if (!variable_global_exists("base_cards") || !ds_exists(global.base_cards, ds_type_map)) {
        global.base_cards = ds_map_create();
    }
    if (!variable_global_exists("combo_registry") || !ds_exists(global.combo_registry, ds_type_map)) {
        global.combo_registry = ds_map_create();
    }
    
    // Load CardDatabase.json (base cards)
    var card_file = "datafiles/CardDatabase.json";
    if (!file_exists(card_file)) {
        show_debug_message("[ERROR] CardDatabase.json not found: " + card_file);
        event_user(0); // json_load_error event
        return false;
    }
    
    var card_json = "";
    var f = file_text_open_read(card_file);
    if (f == -1) {
        show_debug_message("[ERROR] Failed to open CardDatabase.json for reading");
        event_user(0);
        return false;
    }
    
    // Read entire file
    while (!file_text_eof(f)) {
        card_json += file_text_read_string(f);
        file_text_readln(f);
    }
    file_text_close(f);
    
    var card_data = json_decode(card_json);
    if (card_data == -1 || !ds_exists(card_data, ds_type_map)) {
        show_debug_message("[ERROR] Failed to parse CardDatabase.json - invalid JSON");
        if (card_data != -1) ds_map_destroy(card_data);
        event_user(0);
        return false;
    }
    
    // Parse base cards array
    var cards_array = ds_map_find_value(card_data, "cards");
    if (!is_array(cards_array)) {
        show_debug_message("[ERROR] CardDatabase.json missing 'cards' array");
        ds_map_destroy(card_data);
        event_user(0);
        return false;
    }
    
    var card_count = array_length(cards_array);
    show_debug_message("Loading " + string(card_count) + " base cards...");
    
    for (var i = 0; i < card_count; i++) {
        var card = cards_array[i];
        var card_id = card[? "id"];
        
        // Create a ds_map for each card
        var card_map = ds_map_create();
        ds_map_add(card_map, "id", card_id);
        ds_map_add(card_map, "name", card[? "name"]);
        ds_map_add(card_map, "element", card[? "element"]);
        ds_map_add(card_map, "type", card[? "type"]);
        ds_map_add(card_map, "manaCost", card[? "manaCost"]);
        ds_map_add(card_map, "health", card[? "health"]);
        ds_map_add(card_map, "power", card[? "power"]);
        ds_map_add(card_map, "rarity", card[? "rarity"]);
        ds_map_add(card_map, "isQuestCard", card[? "isQuestCard"] ?? false);
        
        ds_map_add_map(global.base_cards, card_id, card_map);
    }
    
    ds_map_destroy(card_data);
    show_debug_message("Base cards loaded: " + string(ds_map_size(global.base_cards)));
    
    // Load combos.json
    var combo_file = "datafiles/combos.json";
    if (!file_exists(combo_file)) {
        show_debug_message("[WARNING] combos.json not found, skipping combo loading");
        // Still return true, combo loading is optional
    } else {
        var combo_json = "";
        var cf = file_text_open_read(combo_file);
        if (cf == -1) {
            show_debug_message("[WARNING] Failed to open combos.json for reading");
        } else {
            while (!file_text_eof(cf)) {
                combo_json += file_text_read_string(cf);
                file_text_readln(cf);
            }
            file_text_close(cf);
            
            var combo_data = json_decode(combo_json);
            if (combo_data == -1 || !ds_exists(combo_data, ds_type_map)) {
                show_debug_message("[WARNING] Failed to parse combos.json - invalid JSON");
                if (combo_data != -1) ds_map_destroy(combo_data);
            } else {
                var combos_array = ds_map_find_value(combo_data, "combos");
                if (is_array(combos_array)) {
                    var combo_count = array_length(combos_array);
                    show_debug_message("Loading " + string(combo_count) + " combo recipes...");
                    
                    for (var j = 0; j < combo_count; j++) {
                        var combo = combos_array[j];
                        var card1_id = combo[? "card1Id"];
                        var card2_id = combo[? "card2Id"];
                        
                        // Recipe key formula: 10000 + min*1000 + max
                        var min_id = min(card1_id, card2_id);
                        var max_id = max(card1_id, card2_id);
                        var recipe_key = 10000 + (min_id * 1000) + max_id;
                        
                        var combo_map = ds_map_create();
                        ds_map_add(combo_map, "resultName", combo[? "resultName"]);
                        ds_map_add(combo_map, "element", combo[? "element"]);
                        ds_map_add(combo_map, "card1Id", card1_id);
                        ds_map_add(combo_map, "card2Id", card2_id);
                        ds_map_add(combo_map, "recipeKey", recipe_key);
                        
                        // Abilities array
                        var abilities = combo[? "abilities"];
                        if (is_array(abilities)) {
                            var abilities_list = ds_list_create();
                            for (var k = 0; k < array_length(abilities); k++) {
                                var ability = abilities[k];
                                var ability_map = ds_map_create();
                                ds_map_add(ability_map, "name", ability[? "name"]);
                                ds_map_add(ability_map, "effect", ability[? "effect"]);
                                ds_map_add(ability_map, "value", ability[? "value"]);
                                ds_map_add(ability_map, "manaCost", ability[? "manaCost"]);
                                ds_map_add(ability_map, "target", ability[? "target"]);
                                ds_map_add(ability_map, "description", ability[? "description"] ?? "");
                                ds_list_add(abilities_list, ability_map);
                            }
                            ds_map_add_list(combo_map, "abilities", abilities_list);
                        }
                        
                        ds_map_add_map(global.combo_registry, recipe_key, combo_map);
                    }
                    show_debug_message("Combo recipes loaded: " + string(ds_map_size(global.combo_registry)));
                }
                ds_map_destroy(combo_data);
            }
        }
    }
    
    global.cards_loaded = true;
    show_debug_message("=== Card Data Load Complete ===");
    show_debug_message("Stats: " + string(ds_map_size(global.base_cards)) + " base cards, " + string(ds_map_size(global.combo_registry)) + " combos");
    
    return true;
}

// User event 0 = json_load_error event
function user_event0() {
    show_debug_message("[EVENT] json_load_error triggered - card data load failure logged");
    // This event can be hooked by other objects to handle errors
    if (variable_global_exists("on_card_load_error")) {
        script_execute(global.on_card_load_error);
    }
}