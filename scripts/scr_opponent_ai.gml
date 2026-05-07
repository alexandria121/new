/// @brief Opponent AI for Magical Battle cards - GML Implementation
/// @details Mirrors the C# OpponentAI class with enums, structs, and decision logic
/// @requires GMS 2.3+ (uses constructor syntax and enum functions)

// ============================================================
// ENUMS
// ============================================================

/// @brief AI difficulty levels - mirrors C# DifficultyLevel enum
function AIDifficultyLevel() constructor {
    // Static constants - use these instead of numeric values
    static Apprentice    = 0;
    static Journeyman   = 1;
    static Expert       = 2;
    static Grandmaster   = 3;
    
    // For iteration/debugging
    static get_names = function() {
        return ["Apprentice", "Journeyman", "Expert", "Grandmaster"];
    }
    
    static to_string = function(value) {
        switch (value) {
            case Apprentice:   return "Apprentice";
            case Journeyman:   return "Journeyman";
            case Expert:       return "Expert";
            case Grandmaster:   return "Grandmaster";
            default:           return "Unknown(" + string(value) + ")";
        }
    }
}

// Static instance for easy access
global.ai_difficulty = new AIDifficultyLevel();

/// @brief AI decision types - mirrors C# AIDecisionType enum
function AIDecisionType() constructor {
    static PlayCreature  = 0;
    static PlaySpell     = 1;
    static PlayArtifact  = 2;
    static PlayWeapon    = 3;
    static Attack        = 4;
    static UseAbility    = 5;
    static EndTurn       = 6;
    static Pass          = 7;
    
    static get_names = function() {
        return ["PlayCreature", "PlaySpell", "PlayArtifact", "PlayWeapon", "Attack", "UseAbility", "EndTurn", "Pass"];
    }
    
    static to_string = function(value) {
        switch (value) {
            case PlayCreature:  return "PlayCreature";
            case PlaySpell:      return "PlaySpell";
            case PlayArtifact:   return "PlayArtifact";
            case PlayWeapon:     return "PlayWeapon";
            case Attack:         return "Attack";
            case UseAbility:     return "UseAbility";
            case EndTurn:         return "EndTurn";
            case Pass:           return "Pass";
            default:             return "Unknown(" + string(value) + ")";
        }
    }
}

// Static instance for easy access
global.ai_decision_type = new AIDecisionType();

// ============================================================
// AIDECISION STRUCT
// ============================================================

/// @brief Represents a single AI decision - mirrors C# OpponentAI.AIDecision struct
/// @param cardId_        (string) Card identifier
/// @param slotIndex_     (real)   Creature slot index
/// @param targetCard_    (struct) Target card instance
/// @param decisionType_  (real)   AIDecisionType value
/// @param abilityIndex_  (real)   Ability index for UseAbility decisions
/// @param abilityTarget_ (struct) Target for ability use
function AIDecision(_cardId_=undefined, _slotIndex_=undefined, _targetCard_=undefined, 
                    _decisionType_=global.ai_decision_type.Pass, _abilityIndex_=undefined, 
                    _abilityTarget_=undefined) constructor {
    // Core decision fields
    cardId      = _cardId_;
    slotIndex    = _slotIndex_;
    targetCard   = _targetCard_;
    decisionType = _decisionType_;
    
    // UseAbility specific fields
    abilityIndex  = _abilityIndex_;
    abilityTarget = _abilityTarget_;
    
    // Factory method for creating a pass decision
    static pass = function() {
        return new AIDecision(undefined, undefined, undefined, global.ai_decision_type.Pass, undefined, undefined);
    }
    
    // Factory method for creating a play creature decision
    static play_creature = function(_cardId, _slotIndex) {
        return new AIDecision(_cardId, _slotIndex, undefined, global.ai_decision_type.PlayCreature, undefined, undefined);
    }
    
    // Factory method for creating a play spell decision
    static play_spell = function(_cardId, _target=undefined) {
        return new AIDecision(_cardId, undefined, _target, global.ai_decision_type.PlaySpell, undefined, undefined);
    }
    
    // Factory method for creating a use ability decision
    static use_ability = function(_cardId, _slotIndex, _abilityIdx, _target=undefined) {
        return new AIDecision(_cardId, _slotIndex, _target, global.ai_decision_type.UseAbility, _abilityIdx, _target);
    }
    
    // Check if this is a pass decision
    static is_pass = function() {
        return decisionType == global.ai_decision_type.Pass;
    }
    
    // Debug string representation
    static to_string = function() {
        var result = "AIDecision: ";
        result += global.ai_decision_type.to_string(decisionType);
        if (cardId != undefined) result += ", cardId=" + string(cardId);
        if (slotIndex != undefined) result += ", slot=" + string(slotIndex);
        if (abilityIndex != undefined) result += ", ability=" + string(abilityIndex);
        return result;
    }
}

// ============================================================
// OPPONENT AI CLASS
// ============================================================

/// @brief Main Opponent AI class - mirrors C# OpponentAI
/// @param difficulty_ (real) AIDifficultyLevel value
function OpponentAI(_difficulty_) constructor {
    // Private fields
    difficulty = _difficulty_;
    random = new SystemRandom(); // Assume we have a basic random utility
    
    // Public property
    get_difficulty = function() { return difficulty; }
    
    // ============================================================
    // MAIN ENTRY POINT - mirrors C# DecideAction()
    // ============================================================
    
    /// @brief Main decision function - determines what action the AI should take
    /// @param deck           (struct) DeckManager instance
    /// @param currentMana    (real)  Available mana
    /// @param playerSlots     (array) Array of CreatureSlot structs (player's creatures)
    /// @param opponentSlots   (array) Optional: AI's own creature slots
    /// @returns AIDecision struct
    decide_action = function(_deck, _currentMana, _playerSlots, _opponentSlots=undefined) {
        // First check if any creature can use an ability (abilities first priority)
        var abilityDecision = try_choose_ability_decision(_deck, _currentMana, _playerSlots, _opponentSlots);
        if (!abilityDecision.is_pass()) {
            return abilityDecision;
        }
        
        // Route to difficulty-specific logic
        switch (difficulty) {
            case global.ai_difficulty.Apprentice:
                return apprentice_logic(_deck, _currentMana, _playerSlots);
            case global.ai_difficulty.Journeyman:
                return journeyman_logic(_deck, _currentMana, _playerSlots);
            case global.ai_difficulty.Expert:
                return expert_logic(_deck, _currentMana, _playerSlots);
            case global.ai_difficulty.Grandmaster:
                return grandmaster_logic(_deck, _currentMana, _playerSlots);
            default:
                return apprentice_logic(_deck, _currentMana, _playerSlots);
        }
    }
    
    // ============================================================
    // ABILITY DECISION LOGIC - mirrors C# TryChooseAbilityDecision()
    // ============================================================
    
    /// @brief Try to find a usable creature ability
    /// @details Returns pass decision if no abilities can be used
    try_choose_ability_decision = function(_deck, _currentMana, _playerSlots, _opponentSlots=undefined) {
        if (_opponentSlots == undefined) {
            return AIDecision.pass();
        }
        
        // Iterate through opponent's creatures (AI's creatures = opponentSlots here)
        for (var i = 0; i < array_length(_opponentSlots); i++) {
            var slot = _opponentSlots[i];
            if (slot == undefined || slot.creature == undefined) continue;
            
            var creature = slot.creature;
            
            // Skip if no abilities
            if (creature.abilities == undefined || array_length(creature.abilities) == 0) continue;
            
            // Check each ability
            for (var a = 0; a < array_length(creature.abilities); a++) {
                var ability = creature.abilities[a];
                
                // Skip passive abilities
                if (ability.is_passive == true) continue;
                
                // Check mana cost
                if (ability.mana_cost > _currentMana) continue;
                
                // Find target if needed
                var target = undefined;
                if (ability.requires_target == true) {
                    // Get available targets (player's creatures)
                    var availableTargets = [];
                    for (var t = 0; t < array_length(_playerSlots); t++) {
                        if (_playerSlots[t] != undefined && _playerSlots[t].creature != undefined) {
                            array_push(availableTargets, _playerSlots[t].creature);
                        }
                    }
                    
                    if (array_length(availableTargets) > 0) {
                        target = availableTargets[random.next(array_length(availableTargets))];
                    } else {
                        continue; // No valid targets, skip this ability
                    }
                }
                
                // We found a valid ability - return UseAbility decision
                return AIDecision.use_ability(creature.id, i, a, target);
            }
        }
        
        return AIDecision.pass();
    }
    
    // ============================================================
    // DIFFICULTY-SPECIFIC LOGIC
    // ============================================================
    
    /// @brief Apprentice-level AI - plays random affordable cards
    apprentice_logic = function(_deck, _currentMana, _playerSlots) {
        // Get playable cards (cost <= mana)
        var playableCards = get_playable_cards(_deck, _currentMana);
        
        if (array_length(playableCards) == 0) {
            return AIDecision.pass();
        }
        
        // Random selection
        var randomCard = playableCards[random.next(array_length(playableCards))];
        
        // Handle creature placement
        if (randomCard.type == global.card_type.Creature) {
            var emptySlot = find_empty_slot(_deck.creatureSlots);
            if (emptySlot >= 0) {
                return AIDecision.play_creature(randomCard.id, emptySlot);
            }
        }
        
        // Determine decision type for card type
        var decisionType = get_decision_type_for_card(randomCard);
        
        return new AIDecision(randomCard.id, undefined, undefined, decisionType, undefined, undefined);
    }
    
    /// @brief Journeyman-level AI - prioritizes high power cards
    journeyman_logic = function(_deck, _currentMana, _playerSlots) {
        // Get playable cards sorted by power
        var playableCards = get_playable_cards_sorted_by_power(_deck, _currentMana);
        
        if (array_length(playableCards) == 0) {
            return AIDecision.pass();
        }
        
        // Pick best card
        var bestCard = playableCards[0];
        
        // Handle creature placement
        if (bestCard.type == global.card_type.Creature) {
            var emptySlot = find_empty_slot(_deck.creatureSlots);
            if (emptySlot >= 0) {
                return AIDecision.play_creature(bestCard.id, emptySlot);
            }
            return AIDecision.pass();
        }
        
        // Handle spell targeting
        if (bestCard.type == global.card_type.Spell) {
            var hasTargets = has_any_creatures(_playerSlots);
            if (hasTargets) {
                var target = select_random_creature(_playerSlots);
                return new AIDecision(bestCard.id, undefined, target, global.ai_decision_type.PlaySpell, undefined, undefined);
            }
        }
        
        return new AIDecision(bestCard.id, undefined, undefined, get_decision_type_for_card(bestCard), undefined, undefined);
    }
    
    /// @brief Expert-level AI - considers board position and value
    expert_logic = function(_deck, _currentMana, _playerSlots) {
        // Get playable cards sorted by value calculation
        var playableCards = get_playable_cards_sorted_by_value(_deck, _currentMana, _playerSlots);
        
        if (array_length(playableCards) == 0) {
            return AIDecision.pass();
        }
        
        var bestCard = playableCards[0];
        
        // Handle creature placement with best slot
        if (bestCard.type == global.card_type.Creature) {
            var slot = find_best_slot_for_creature(_deck, bestCard, _playerSlots);
            if (slot >= 0) {
                return AIDecision.play_creature(bestCard.id, slot);
            }
            return AIDecision.pass();
        }
        
        // Handle spell targeting
        if (bestCard.type == global.card_type.Spell) {
            var target = find_best_spell_target(bestCard, _playerSlots, _deck);
            return new AIDecision(bestCard.id, undefined, target, global.ai_decision_type.PlaySpell, undefined, undefined);
        }
        
        return new AIDecision(bestCard.id, undefined, undefined, get_decision_type_for_card(bestCard), undefined, undefined);
    }
    
    /// @brief Grandmaster-level AI - optimal threat assessment and removal
    grandmaster_logic = function(_deck, _currentMana, _playerSlots) {
        // Get all playable cards
        var playableCards = get_playable_cards(_deck, _currentMana);
        
        if (array_length(playableCards) == 0) {
            return AIDecision.pass();
        }
        
        // Get sorted threats (highest power first)
        var threats = get_creatures_sorted_by_power(_playerSlots);
        
        // Get empty slot counts
        var myEmptySlots = count_empty_slots(_deck.creatureSlots);
        
        // Priority 1: Play high-value creatures or when board is clear
        var sortedByValue = get_playable_cards_sorted_by_value(_deck, _currentMana, _playerSlots);
        
        for (var i = 0; i < array_length(sortedByValue); i++) {
            var card = sortedByValue[i];
            
            // Creature logic
            if (card.type == global.card_type.Creature && myEmptySlots > 0) {
                if (card.power >= 4 || array_length(threats) == 0) {
                    var slot = find_best_slot_for_creature(_deck, card, _playerSlots);
                    if (slot >= 0) {
                        return AIDecision.play_creature(card.id, slot);
                    }
                }
            }
            
            // Spell logic - prioritize killing threats
            if (card.type == global.card_type.Spell && array_length(threats) > 0) {
                var killable = find_killable_threat(card, threats);
                if (killable != undefined) {
                    return new AIDecision(card.id, undefined, killable, global.ai_decision_type.PlaySpell, undefined, undefined);
                }
            }
        }
        
        // Fallback: Play cheap creatures
        var cheapCreatures = get_cheap_creatures(playableCards, 2);
        if (array_length(cheapCreatures) > 0 && myEmptySlots > 0) {
            var best = cheapCreatures[0];
            var slot = find_empty_slot(_deck.creatureSlots);
            return AIDecision.play_creature(best.id, slot);
        }
        
        return AIDecision.pass();
    }
    
    // ============================================================
    // HELPER FUNCTIONS
    // ============================================================
    
    get_playable_cards = function(_deck, _currentMana) {
        var result = [];
        var hand = _deck.hand;
        for (var i = 0; i < array_length(hand); i++) {
            var card = hand[i];
            if (card.mana_cost <= _currentMana) {
                array_push(result, card);
            }
        }
        return result;
    }
    
    get_playable_cards_sorted_by_power = function(_deck, _currentMana) {
        var cards = get_playable_cards(_deck, _currentMana);
        // Sort descending by power
        array_sort(cards, function(a, b) { return b.power - a.power; });
        return cards;
    }
    
    get_playable_cards_sorted_by_value = function(_deck, _currentMana, _playerSlots) {
        var cards = get_playable_cards(_deck, _currentMana);
        // Sort by GetCardValue calculation
        array_sort(cards, function(a, b) { 
            return calculate_card_value(b, _currentMana, _playerSlots) - calculate_card_value(a, _currentMana, _playerSlots);
        });
        return cards;
    }
    
    calculate_card_value = function(_card, _currentMana, _playerSlots) {
        var baseValue = _card.power + (_card.health / 2.0);
        
        if (_card.type == global.card_type.Creature) {
            if (_currentMana >= _card.mana_cost + 3) {
                baseValue += 3;
            }
        } else if (_card.type == global.card_type.Spell) {
            var threats = get_creatures(_playerSlots);
            if (array_length(threats) > 0) {
                var killCount = 0;
                for (var i = 0; i < array_length(threats); i++) {
                    if (threats[i].health <= _card.power) killCount++;
                }
                baseValue += killCount * 5;
            }
            baseValue += 2; // Bonus for spell flexibility
        }
        
        return baseValue;
    }
    
    get_decision_type_for_card = function(_card) {
        switch (_card.type) {
            case global.card_type.Creature:  return global.ai_decision_type.PlayCreature;
            case global.card_type.Weapon:   return global.ai_decision_type.PlayWeapon;
            case global.card_type.Armor:    return global.ai_decision_type.PlayArtifact;
            case global.card_type.Artifact:  return global.ai_decision_type.PlayArtifact;
            case global.card_type.Event:     return global.ai_decision_type.PlaySpell;
            default:                         return global.ai_decision_type.PlaySpell;
        }
    }
    
    find_empty_slot = function(_slots) {
        for (var i = 0; i < array_length(_slots); i++) {
            if (_slots[i].is_empty()) {
                return i;
            }
        }
        return -1;
    }
    
    count_empty_slots = function(_slots) {
        var count = 0;
        for (var i = 0; i < array_length(_slots); i++) {
            if (_slots[i].is_empty()) count++;
        }
        return count;
    }
    
    get_creatures = function(_slots) {
        var result = [];
        for (var i = 0; i < array_length(_slots); i++) {
            if (_slots[i] != undefined && _slots[i].creature != undefined) {
                array_push(result, _slots[i].creature);
            }
        }
        return result;
    }
    
    get_creatures_sorted_by_power = function(_slots) {
        var creatures = get_creatures(_slots);
        array_sort(creatures, function(a, b) { return b.power - a.power; });
        return creatures;
    }
    
    has_any_creatures = function(_slots) {
        for (var i = 0; i < array_length(_slots); i++) {
            if (_slots[i] != undefined && _slots[i].creature != undefined) {
                return true;
            }
        }
        return false;
    }
    
    select_random_creature = function(_slots) {
        var creatures = get_creatures(_slots);
        if (array_length(creatures) > 0) {
            return creatures[random.next(array_length(creatures))];
        }
        return undefined;
    }
    
    find_best_slot_for_creature = function(_deck, _creature, _playerSlots) {
        var emptySlots = [];
        for (var i = 0; i < array_length(_deck.creatureSlots); i++) {
            if (_deck.creatureSlots[i].is_empty()) {
                array_push(emptySlots, i);
            }
        }
        
        if (array_length(emptySlots) == 0) return -1;
        
        // Get threats
        var threats = [];
        for (var i = 0; i < array_length(_playerSlots); i++) {
            if (_playerSlots[i] != undefined && _playerSlots[i].creature != undefined) {
                array_push(threats, { slot: _playerSlots[i], index: i });
            }
        }
        
        if (array_length(threats) == 0) {
            return emptySlots[0]; // Return first empty slot if no threats
        }
        
        // Match against threats we can kill
        for (var i = 0; i < array_length(threats); i++) {
            if (_creature.power >= threats[i].slot.creature.health) {
                if (threats[i].index < array_length(emptySlots)) {
                    return emptySlots[threats[i].index];
                }
            }
        }
        
        // Return first empty slot
        return emptySlots[0];
    }
    
    find_best_spell_target = function(_spell, _playerSlots, _deck) {
        var targets = get_creatures(_playerSlots);
        if (array_length(targets) == 0) return undefined;
        
        if (_spell.power > 0) {
            // Try to kill something
            for (var i = 0; i < array_length(targets); i++) {
                if (targets[i].health <= _spell.power) {
                    return targets[i];
                }
            }
            // No killable targets, hit highest power
            array_sort(targets, function(a, b) { return b.power - a.power; });
            return targets[0];
        }
        
        return targets[0];
    }
    
    find_killable_threat = function(_card, _threats) {
        for (var i = 0; i < array_length(_threats); i++) {
            if (_threats[i].health <= _card.power) {
                return _threats[i];
            }
        }
        return undefined;
    }
    
    get_cheap_creatures = function(_cards, _maxCost) {
        var result = [];
        for (var i = 0; i < array_length(_cards); i++) {
            if (_cards[i].type == global.card_type.Creature && _cards[i].mana_cost <= _maxCost) {
                array_push(result, _cards[i]);
            }
        }
        return result;
    }
}

// ============================================================
// FACTORY FUNCTIONS
// ============================================================

/// @brief Create an OpponentAI instance for a given difficulty
/// @param difficulty (real) AIDifficultyLevel value
/// @returns OpponentAI instance
function opponent_ai_create(_difficulty) {
    return new OpponentAI(_difficulty);
}

/// @brief Get all card type values for AI logic
/// @note Assumes global.card_type exists with: Creature, Weapon, Armor, Artifact, Spell, Event
function get_card_type_enum() {
    if (!variable_global_exists("card_type")) {
        // Define if not exists - mirrors C# CardType enum
        global.card_type = {
            Creature: 0,
            Spell: 1,
            Weapon: 2,
            Armor: 3,
            Artifact: 4,
            Event: 5
        };
    }
    return global.card_type;
}

// ============================================================
// LOGGING HELPER
// ============================================================

/// @brief Log AI decision for debugging
/// @param decision (AIDecision) The decision struct
/// @param source   (string)     Source function name
function ai_log_decision(_decision, _source) {
    if (global.debug_mode) {
        var msg = "[AI Decision] " + _source + ": " + _decision.to_string();
        show_debug_message(msg);
    }
}

// ============================================================
// INITIALIZATION
// ============================================================

// Initialize card type enum on load
get_card_type_enum();

// ============================================================
// OBSERVABILITY - ai_decision event logging
// ============================================================

/// @brief Emit ai_decision event for observability
/// @param decision (AIDecision) Decision that was made
/// @param context  (string)     Additional context (e.g., difficulty name)
function ai_emit_decision_event(_decision, _context="") {
    // Log the decision type and target for observability
    var decisionTypeName = global.ai_decision_type.to_string(_decision.decisionType);
    var targetInfo = "";
    if (_decision.slotIndex != undefined) {
        targetInfo = " slot=" + string(_decision.slotIndex);
    }
    if (_decision.cardId != undefined) {
        targetInfo += " card=" + string(_decision.cardId);
    }
    if (_decision.abilityIndex != undefined) {
        targetInfo += " ability=" + string(_decision.abilityIndex);
    }
    
    show_debug_message("[ai_decision] type=" + decisionTypeName + targetInfo + " context=" + _context);
}

// ============================================================
// CREATE DECK FOR DIFFICULTY
// ============================================================

/// @brief Creates a deck of card IDs for a given AI difficulty
/// @param difficulty (real) AIDifficultyLevel value (0-3)
/// @returns ds_list of card ID strings (caller owns the list)
/// @note Mirrors C# OpponentAI.CreateDeckForDifficulty():
///   - Apprentice:  random 30 cards from CardPool, mana cost <= 3
///   - Journeyman: higher avg power, balanced mana curve, some removal
///   - Expert:     higher power-per-mana, synergy-focused
///   - Grandmaster: high power, strong synergies, targeted removal
function CreateDeckForDifficulty(_difficulty) {
    var _deck = ds_list_create();
    var _rng = new SystemRandom();
    
    // global.base_cards is keyed by card Id string (populated by load_all_card_data)
    if (ds_map_empty(global.base_cards)) {
        show_debug_message("[CreateDeckForDifficulty] ERROR: global.base_cards is empty — call load_all_card_data() first");
        return _deck;
    }
    
    // ── Collect unique card entry templates ─────────────────────────
    // ds_map keys are card Id strings; values are card entry ds_maps
    var _seen = ds_map_create();
    var _pool  = ds_list_create();
    var _key   = ds_map_find_first(global.base_cards);
    for (var _n = ds_map_size(global.base_cards); _n > 0; _n--) {
        if (!ds_map_exists(_seen, _key)) {
            ds_map_add(_seen, _key, true);
            ds_list_add(_pool, global.base_cards[? _key]);  // value = card entry ds_map
        }
        _key = ds_map_find_next(global.base_cards, _key);
    }
    ds_map_destroy(_seen);
    
    if (ds_list_empty(_pool)) {
        show_debug_message("[CreateDeckForDifficulty] ERROR: no card entries in global.base_cards");
        ds_list_destroy(_pool);
        return _deck;
    }
    
    // ── Filter pool by difficulty ────────────────────────────────
    var _filtered = ds_list_create();
    var _count = ds_list_size(_pool);
    
    for (var _i = 0; _i < _count; _i++) {
        var _entry = ds_list_find_value(_pool, _i);
        
        var _mana, _pow, _type;
        if (is_struct(_entry)) {
            _mana = struct_get(_entry, "manaCost") ?? 0;
            _pow  = struct_get(_entry, "power") ?? 0;
            _type = struct_get(_entry, "type") ?? 0;
        } else {
            _mana = _entry[? "manaCost"] ?? 0;
            _pow  = _entry[? "power"] ?? 0;
            _type = _entry[? "type"] ?? 0;
        }
        
        var _ok = false;
        switch (_difficulty) {
            case global.ai_difficulty.Apprentice:
                // Forgiving pool: low-cost cards only
                _ok = (_mana <= 3);  break;
            case global.ai_difficulty.Journeyman:
                // Moderate curve with decent power — some removal spells included
                _ok = (_mana <= 5) && ((_type == 0) || (_pow >= 2));  break;
            case global.ai_difficulty.Expert:
                // Higher power-per-mana, synergy-friendly
                _ok = (_pow >= 2) || (_mana <= 6);  break;
            case global.ai_difficulty.Grandmaster:
                // Highest power, strong synergies, artifact support
                _ok = (_pow >= 3) || (_mana <= 7) || (_type == 4);  break;
            default: _ok = (_mana <= 3);  break;
        }
        
        if (_ok) ds_list_add(_filtered, _entry);
    }
    ds_list_destroy(_pool);
    
    // Fallback: if filter emptied the pool, use everything
    if (ds_list_empty(_filtered)) {
        show_debug_message("[CreateDeckForDifficulty] WARNING: filter returned nothing — using full pool");
        _filtered = ds_list_create();
        var _kf = ds_map_find_first(global.base_cards);
        for (var _nf = ds_map_size(global.base_cards); _nf > 0; _nf--) {
            ds_list_add(_filtered, global.base_cards[? _kf]);
            _kf = ds_map_find_next(global.base_cards, _kf);
        }
    }
    
    // ── Deck size per difficulty ────────────────────────────────
    var _size = 0;
    switch (_difficulty) {
        case global.ai_difficulty.Apprentice:   _size = 8  + _rng.next(5); break;  // 8–12
        case global.ai_difficulty.Journeyman:  _size = 10 + _rng.next(6); break;  // 10–15
        case global.ai_difficulty.Expert:      _size = 12 + _rng.next(7); break;  // 12–18
        case global.ai_difficulty.Grandmaster: _size = 15 + _rng.next(8); break;  // 15–22
        default:                               _size = 10 + _rng.next(11); break; // 10–20
    }
    
    // ── Sample cards from filtered pool ─────────────────────────
    var _fSz = ds_list_size(_filtered);
    if (_fSz == 0) {
        show_debug_message("[CreateDeckForDifficulty] ERROR: pool empty");
        ds_list_destroy(_filtered);
        return _deck;
    }
    
    for (var _k = 0; _k < _size; _k++) {
        var _src = ds_list_find_value(_filtered, _rng.next(_fSz));
        // Wrap raw entry in Card struct, then CardClone for unique Id
        // Mirrors C# CardFactory → Card.Clone()
        var _card = Card(_src);
        var _clone = CardClone(_card);
        ds_list_add(_deck, _clone);
    }
    ds_list_destroy(_filtered);
    
    // ── Observability ───────────────────────────────────────────
    var _name = global.ai_difficulty.to_string(_difficulty);
    show_debug_message("[ai_deck_created] difficulty=" + _name
                     + " deck_size=" + string(ds_list_size(_deck)));
    
    return _deck;
}