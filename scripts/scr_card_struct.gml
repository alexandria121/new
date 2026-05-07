/// CardAbility struct constructor — maps from a ds_map to a struct
/// Mirrors MagicalDeckbuilder.Cards.CardAbility C# class fields exactly
function CardAbility(_map) {
    return {
        name: _map[? "name"] ?? "",
        description: _map[? "description"] ?? "",
        manaCost: _map[? "manaCost"] ?? 0,
        effectType: _map[? "effectType"] ?? 0,
        effectValue: _map[? "effectValue"] ?? 0,
        target: _map[? "target"] ?? 0,
        isTemporary: _map[? "isTemporary"] ?? false,
        duration: _map[? "duration"] ?? 0,
        isPassive: _map[? "isPassive"] ?? false,
        requiresTarget: _map[? "requiresTarget"] ?? false
    };
}

/// CardEffect struct constructor — maps from a ds_map to a struct
/// Mirrors MagicalDeckbuilder.Cards.CardEffect C# class fields
function CardEffect(_map) {
    return {
        name: _map[? "name"] ?? "",
        description: _map[? "description"] ?? "",
        type: _map[? "type"] ?? 0,
        value: _map[? "value"] ?? 0,
        target: _map[? "target"] ?? 0,
        isTemporary: _map[? "isTemporary"] ?? false
    };
}

/// Card struct constructor — maps from a ds_map to a struct
/// Mirrors MagicalDeckbuilder.Cards.Card C# class fields exactly
/// Also handles nested Abilities (array of CardAbility maps) and Effects (array of CardEffect maps)
function Card(_map) {
    var abilities = [];
    var abilitiesData = _map[? "abilities"];
    if (abilitiesData != undefined && is_array(abilitiesData)) {
        for (var i = 0; i < array_length(abilitiesData); i++) {
            abilities[i] = CardAbility(abilitiesData[i]);
        }
    }
    
    var effects = [];
    var effectsData = _map[? "effects"];
    if (effectsData != undefined && is_array(effectsData)) {
        for (var j = 0; j < array_length(effectsData); j++) {
            effects[j] = CardEffect(effectsData[j]);
        }
    }
    
    return {
        id: _map[? "id"] ?? "",
        templateId: _map[? "templateId"] ?? 0,
        name: _map[? "name"] ?? "Unknown",
        description: _map[? "description"] ?? "",
        type: _map[? "type"] ?? 0,
        element: _map[? "element"] ?? 0,
        manaCost: _map[? "manaCost"] ?? 0,
        power: _map[? "power"] ?? 0,
        health: _map[? "health"] ?? 0,
        effects: effects,
        abilities: abilities,
        isLegendary: _map[? "isLegendary"] ?? false,
        rarity: _map[? "rarity"] ?? 1,
        isCombinable: _map[? "isCombinable"] ?? true,
        isComboOnly: _map[? "isComboOnly"] ?? false,
        temperatureGradient: _map[? "temperatureGradient"] ?? ""
    };
}

/// Deep-clone a card struct, generating a new Id (GUID-style string)
/// Mirrors MagicalDeckbuilder.Cards.Card.Clone() behaviour
function CardClone(_card) {
    var newAbilities = [];
    for (var i = 0; i < array_length(_card.abilities); i++) {
        newAbilities[i] = {
            name: _card.abilities[i].name,
            description: _card.abilities[i].description,
            manaCost: _card.abilities[i].manaCost,
            effectType: _card.abilities[i].effectType,
            effectValue: _card.abilities[i].effectValue,
            target: _card.abilities[i].target,
            isTemporary: _card.abilities[i].isTemporary,
            duration: _card.abilities[i].duration,
            isPassive: _card.abilities[i].isPassive,
            requiresTarget: _card.abilities[i].requiresTarget
        };
    }
    
    var newEffects = [];
    for (var k = 0; k < array_length(_card.effects); k++) {
        newEffects[k] = {
            name: _card.effects[k].name,
            description: _card.effects[k].description,
            type: _card.effects[k].type,
            value: _card.effects[k].value,
            target: _card.effects[k].target,
            isTemporary: _card.effects[k].isTemporary
        };
    }
    
    return {
        id: date_current_datetime() + "_" + string(random(999999)),
        templateId: _card.templateId,
        name: _card.name,
        description: _card.description,
        type: _card.type,
        element: _card.element,
        manaCost: _card.manaCost,
        power: _card.power,
        health: _card.health,
        effects: newEffects,
        abilities: newAbilities,
        isLegendary: _card.isLegendary,
        rarity: _card.rarity,
        isCombinable: _card.isCombinable,
        isComboOnly: _card.isComboOnly,
        temperatureGradient: _card.temperatureGradient
    };
}

/// Human-readable string for a card struct
/// Format: "{Name} ({ManaCost}M) [{Type}] P:{Power} H:{Health}"
function CardToString(_card) {
    var typeNames = ["Spell", "Creature", "Artifact", "Enchantment", "Weapon", "Armor", "Event", "Blank"];
    var typeStr = _card.type < array_length(typeNames) ? typeNames[_card.type] : "Unknown";
    return _card.name + " (" + string(_card.manaCost) + "M) [" + typeStr + "] P:" + string(_card.power) + " H:" + string(_card.health);
}

/// Returns the display colour for an element type (for UI rendering)
/// Mirrors any existing element colour conventions in the codebase
function GetCardElementColor(_element) {
    switch (_element) {
        case ElementType.Radioactivity: return make_color_rgb(0, 255, 0);    // toxic green
        case ElementType.Flesh:         return make_color_rgb(220, 20, 60);   // crimson
        case ElementType.Toxin:          return make_color_rgb(200, 100, 0);    // sickly amber
        case ElementType.Fungus:         return make_color_rgb(144, 238, 144); // pale green
        case ElementType.Thermodynamics: return make_color_rgb(255, 100, 0);   // flame orange
        case ElementType.Time:           return make_color_rgb(100, 100, 255);  // time blue
        case ElementType.Food:           return make_color_rgb(255, 180, 100);  // warm tan
        case ElementType.Eldritch:       return make_color_rgb(128, 0, 128);    // eldritch purple
        default:                        return c_white;
    }
}

/// Sort an array of card structs by mana cost (ascending)
function SortCardsByMana(_cards) {
    var n = array_length(_cards);
    for (var i = 1; i < n; i++) {
        var key = _cards[i];
        var j = i - 1;
        while (j >= 0 && _cards[j].manaCost > key.manaCost) {
            _cards[j + 1] = _cards[j];
            j--;
        }
        _cards[j + 1] = key;
    }
    return _cards;
}