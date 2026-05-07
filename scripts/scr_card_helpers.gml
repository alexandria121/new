/// Card utility helper functions
/// Companion to scr_card_struct.gml — provides pure utility functions on Card structs
/// All functions operate on plain struct instances returned by Card() or CardClone()

/// Returns whether two cards are functionally equal (same id)
/// Mirrors MagicalDeckbuilder.Cards.Card.Equals()
function CardEquals(_cardA, _cardB) {
    if (!is_struct(_cardA) || !is_struct(_cardB)) return false;
    return _cardA.id == _cardB.id;
}

/// Human-readable string for a card struct
/// Format: "{Name} ({Element}) {ManaCost}M {Power}P/{Health}H"
function CardToString(_card) {
    var elemNames = ["Radioactivity", "Flesh", "Toxin", "Fungus", "Thermodynamics", "Time", "Food", "Eldritch", "Unknown"];
    var elem = is_struct(_card) ? _card.element : 0;
    var elemStr = elem < array_length(elemNames) ? elemNames[elem] : "Unknown";
    var name = is_struct(_card) ? _card.name : "???";
    var mc   = is_struct(_card) ? _card.manaCost : 0;
    var pwr  = is_struct(_card) ? _card.power : 0;
    var hlt  = is_struct(_card) ? _card.health : 0;
    return name + " (" + elemStr + ") " + string(mc) + "M " + string(pwr) + "P/" + string(hlt) + "H";
}

/// Returns the display colour for an element type (for UI rendering)
/// Mirrors element colour conventions used elsewhere in the project
function GetCardDisplayColor(_element) {
    switch (_element) {
        case 0: return make_color_rgb(0, 255, 0);     // Radioactivity — toxic green
        case 1: return make_color_rgb(220, 20, 60);    // Flesh — crimson
        case 2: return make_color_rgb(200, 100, 0);     // Toxin — sickly amber
        case 3: return make_color_rgb(144, 238, 144);  // Fungus — pale green
        case 4: return make_color_rgb(255, 100, 0);      // Thermodynamics — flame orange
        case 5: return make_color_rgb(100, 100, 255);   // Time — time blue
        case 6: return make_color_rgb(255, 180, 100);    // Food — warm tan
        case 7: return make_color_rgb(128, 0, 128);     // Eldritch — eldritch purple
        default: return c_white;
    }
}

/// Deep-copy a card struct with a fresh id so the clone is independent
/// Array fields (abilities, effects) are copied element-by-element so the
/// clone does not share references with the original.
/// Mirrors MagicalDeckbuilder.Cards.Card.Clone() behaviour
function CardClone(_card) {
    var newAbilities = [];
    for (var i = 0; i < array_length(_card.abilities); i++) {
        newAbilities[i] = {
            name:          _card.abilities[i].name,
            description:   _card.abilities[i].description,
            manaCost:      _card.abilities[i].manaCost,
            effectType:    _card.abilities[i].effectType,
            effectValue:   _card.abilities[i].effectValue,
            target:        _card.abilities[i].target,
            isTemporary:   _card.abilities[i].isTemporary,
            duration:      _card.abilities[i].duration,
            isPassive:     _card.abilities[i].isPassive,
            requiresTarget:_card.abilities[i].requiresTarget
        };
    }
    
    var newEffects = [];
    for (var k = 0; k < array_length(_card.effects); k++) {
        newEffects[k] = {
            name:        _card.effects[k].name,
            description:_card.effects[k].description,
            type:        _card.effects[k].type,
            value:       _card.effects[k].value,
            target:      _card.effects[k].target,
            isTemporary: _card.effects[k].isTemporary
        };
    }
    
    return {
        id:                 date_current_datetime() + "_" + string(random(999999)),
        templateId:         _card.templateId,
        name:                _card.name,
        description:         _card.description,
        type:                _card.type,
        element:             _card.element,
        manaCost:            _card.manaCost,
        power:               _card.power,
        health:              _card.health,
        effects:             newEffects,
        abilities:           newAbilities,
        isLegendary:         _card.isLegendary,
        rarity:              _card.rarity,
        isCombinable:        _card.isCombinable,
        isComboOnly:          _card.isComboOnly,
        temperatureGradient: _card.temperatureGradient
    };
}

/// Sorts an array of card structs by mana cost ascending (insertion sort)
/// Modifies the array in place and also returns it for convenience
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