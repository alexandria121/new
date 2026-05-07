/// scr_card_combiner.gml
/// Card combining functions for Magical Sparkdeck.
/// Ported from C# MagicalDeckbuilder.Combining/CardCombiner.cs and CardFactory.cs
/// 
/// Usage: call load_all_card_data() at game start first (defined in scr_card_data_loader.gml)
/// then use GetRecipeKey, IsCombinable, TryCombine, GetComboResult as needed.

/// GetRecipeKey(cardA, cardB)
/// Computes the deterministic recipe key for any two cards.
/// Mirrors C# CombinationLookup.cs: 10000 + minId*1000 + maxId
/// The order of cardA/cardB does not matter (min/max normalises it).
function GetRecipeKey(cardA, cardB) {
    var idA = is_struct(cardA) ? struct_get(cardA, "id") : cardA.id;
    var idB = is_struct(cardB) ? struct_get(cardB, "id") : cardB.id;
    var minId = min(idA, idB);
    var maxId = max(idA, idB);
    return 10000 + minId * 1000 + maxId;
}

/// IsCombinable(cardA, cardB)
/// Returns true if a combo recipe exists for these two cards.
function IsCombinable(cardA, cardB) {
    if (!ds_map_exists(global.combo_registry, GetRecipeKey(cardA, cardB))) {
        show_debug_message("combo_failed: "
            + string(is_struct(cardA) ? struct_get(cardA, "id") : cardA.id)
            + " + "
            + string(is_struct(cardB) ? struct_get(cardB, "id") : cardB.id));
        return false;
    }
    return true;
}

/// TryCombine(cardA, cardB)
/// Returns a Card struct for the combo result, or null if not combinable.
function TryCombine(cardA, cardB) {
    if (!IsCombinable(cardA, cardB)) return null;
    var key = GetRecipeKey(cardA, cardB);
    var comboData = ds_map_find_value(global.combo_registry, key);
    var resultId = is_struct(comboData) ? struct_get(comboData, "resultId") : comboData[? "resultId"];
    return GetCard(resultId);
}

/// GetComboResult(cardA, cardB)
/// Looks up the combo recipe and builds a full combo Card struct with element inheritance.
/// Mirrors C# CardCombiner.Combine() + CardFactory ability-inheritance logic:
///   - element falls back to ingredient cards if not stored in combo_data
///   - isCombo = true; comboResult = "{cardA.name} + {cardB.name}"
///   - abilities are inherited from combo_data.abilities (or computed if absent)
/// Logs combo_created with combo name and ingredient cards.
function GetComboResult(cardA, cardB) {
    if (!IsCombinable(cardA, cardB)) {
        return null;
    }

    var key = GetRecipeKey(cardA, cardB);
    var comboData = ds_map_find_value(global.combo_registry, key);

    // Resolve names from ingredient cards (safe for both struct and legacy ds_map)
    var nameA = is_struct(cardA) ? struct_get(cardA, "name") : cardA.name;
    var nameB = is_struct(cardB) ? struct_get(cardB, "name") : cardB.name;
    var elemA  = is_struct(cardA) ? struct_get(cardA, "element") : cardA.element;
    var elemB  = is_struct(cardB) ? struct_get(cardB, "element") : cardB.element;

    // Read combo_data fields — support both struct (GMS 2023+) and ds_map (legacy)
    var comboName;
    if (is_struct(comboData)) {
        comboName = struct_get(comboData, "resultName");
    } else {
        comboName = ds_map_exists(comboData, "resultName") ? comboData[? "resultName"] : "Unknown Combo";
    }

    var resultId;
    if (is_struct(comboData)) {
        resultId = struct_get(comboData, "resultId");
    } else {
        resultId = ds_map_exists(comboData, "resultId") ? comboData[? "resultId"] : 0;
    }

    var resultElement;
    if (is_struct(comboData) && struct_exists(comboData, "element")) {
        resultElement = struct_get(comboData, "element");
    } else if (!is_struct(comboData) && ds_map_exists(comboData, "element")) {
        resultElement = comboData[? "element"];
    } else {
        // Fall back to element of the first ingredient card
        resultElement = elemA;
    }

    // Resolve element string -> ElementType enum code
    // resultElement may be a string or already a number; normalise to enum code
    var elementCode = resultElement;
    if (is_string(resultElement)) {
        switch (resultElement) {
            case "Radioactivity":  elementCode = 0; break;
            case "Flesh":         elementCode = 1; break;
            case "Toxin":         elementCode = 2; break;
            case "Fungus":         elementCode = 3; break;
            case "Thermodynamics":elementCode = 4; break;
            case "Food":          elementCode = 5; break;
            case "Eldritch":      elementCode = 6; break;
            case "Arcane":       elementCode = 7; break;
            default:             elementCode = 0; break;
        }
    }

    // Build abilities array from combo_data.abilities if present
    var abilities = [];
    var abilitiesData;
    if (is_struct(comboData)) {
        abilitiesData = struct_exists(comboData, "abilities")
            ? struct_get(comboData, "abilities")
            : undefined;
    } else {
        abilitiesData = ds_map_exists(comboData, "abilities")
            ? comboData[? "abilities"]
            : undefined;
    }

    if (abilitiesData != undefined && is_array(abilitiesData)) {
        for (var i = 0; i < array_length(abilitiesData); i++) {
            abilities[i] = CardAbility(abilitiesData[i]);
        }
    }

    // Log combo_created event
    show_debug_message("combo_created: " + comboName + " = " + nameA + " + " + nameB);

    // Assemble the combo Card struct
    // id is the resultId (template id), templateId mirrors it, name is comboName,
    // element is the resolved element code, isCombo=true, comboResult records ingredients.
    return {
        id:          resultId,
        templateId:  resultId,
        name:        comboName,
        description: nameA + " + " + nameB,
        type:        0,                  // CardType.Creature
        element:     elementCode,
        manaCost:     0,
        power:       0,
        health:      0,
        isCombo:     true,
        comboResult: nameA + " + " + nameB,
        abilities:   abilities,
        effects:     []
    };
}

/// VerifyAllCombos()
/// Iterates global.combo_registry (loaded from combos.json by load_all_card_data)
/// and confirms every entry has required fields (resultId, resultName).
/// Logs combo_count with actual registry size vs. expected 265 entries.
/// Returns the number of entries with missing or malformed recipe data (0 = all OK).
function VerifyAllCombos() {
    if (!ds_exists(global.combo_registry)) {
        show_debug_message("combo_count: ERROR - global.combo_registry does not exist");
        return -1;
    }

    var size = ds_map_size(global.combo_registry);
    show_debug_message("combo_count: " + string(size) + " combos registered (expected 265+)");

    var malformed = 0;
    var firstFive = [];
    var idx = 0;

    var key = ds_map_find_first(global.combo_registry);
    for (var i = 0; i < size && !is_undefined(key); i++) {
        var data = ds_map_find_value(global.combo_registry, key);

        // Check required fields
        var hasResultId   = is_struct(data) ? struct_exists(data, "resultId")   : ds_map_exists(data, "resultId");
        var hasResultName = is_struct(data) ? struct_exists(data, "resultName") : ds_map_exists(data, "resultName");

        if (!hasResultId || !hasResultName) {
            malformed++;
            show_debug_message("combo_failed: malformed entry at key " + string(key));
        }

        // Capture first 5 entries for debug output
        if (idx < 5) {
            var name = is_struct(data) ? struct_get(data, "resultName") : data[? "resultName"];
            if (is_undefined(name)) name = "(no name)";
            firstFive[idx] = string(key) + " => " + string(name);
            idx++;
        }

        key = ds_map_find_next(global.combo_registry, key);
    }

    show_debug_message("combo_registry first 5 entries:");
    for (var j = 0; j < array_length(firstFive); j++) {
        show_debug_message("  " + firstFive[j]);
    }

    if (malformed > 0) {
        show_debug_message("combo_count: " + string(malformed) + " malformed entries found");
    } else {
        show_debug_message("combo_count: all " + string(size) + " entries valid, 0 missing recipes");
    }

    return malformed;
}
