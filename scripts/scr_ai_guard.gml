// ============================================================
// scr_ai_guard.gml
// AI call guard — wraps OpponentAI.decide_action() with crash safety
// Created by GSD auto-mode for M001/S09/T02
// ============================================================
//
// PURPOSE
//   OpponentAI.decide_action() is the main entry point for AI
//   decisions. If global.base_cards is not yet populated
//   (card data not loaded) or if any structural field
//   is undefined, GML throws no exception — it silently returns
//   undefined — which then causes downstream crashes when the
//   battle controller tries to read properties of undefined.
//
//   scr_ai_guard wraps every AI call site with pre-call
//   guards and a safe fallback, so:
//     - AI crashes do NOT end the game
//     - Failures are logged to the GMS2 console for diagnosis
//     - The battle continues even if AI produces no decision
//
// USAGE — replace direct OpponentAI calls with:
//     var decision = AI_TakeTurn(deck, mana, playerSlots, opponentSlots);
//   instead of:
//     var ai = opponent_ai_create(difficulty);
//     var decision = ai.decide_action(deck, mana, playerSlots, opponentSlots);
//
// ============================================================
// PREREQUISITES
// ============================================================
// Requires these to be loaded (call load_all_card_data() first):
//   - global.base_cards        (ds_map, keyed by card ID)
//   - global.combo_registry     (ds_map, combo recipes)
//   - global.ai_difficulty     (AIDifficultyLevel struct)
//   - global.ai_decision_type   (AIDecisionType struct)
// ============================================================

// --------------------------------------------------------
// AI_TakeTurn(deck, currentMana, playerSlots, opponentSlots)
// --------------------------------------------------------
// Primary safe wrapper for OpponentAI.decide_action().
// Returns an AIDecision (always — never undefined or null).
// If any error occurs, logs the failure and returns AIDecision.pass().
//
// deck:           ds_list of Card structs representing AI hand
// currentMana:    real — mana available to the AI this turn
// playerSlots:    array of creature slot structs (player's creatures)
// opponentSlots: array of creature slot structs (AI's creatures)
//
// Returns: AIDecision struct (never undefined)
function AI_TakeTurn(_deck, _currentMana, _playerSlots, _opponentSlots) {
    // ── Pre-call validation ────────────────────────────────
    if (!ds_exists(_deck)) {
        show_debug_message("[AI_GUARD] ERROR: _deck is not a valid ds_list — returning pass()");
        return AIDecision.pass();
    }

    if (ds_list_empty(_deck)) {
        show_debug_message("[AI_GUARD] deck is empty — returning pass()");
        return AIDecision.pass();
    }

    if (is_undefined(_playerSlots) || !is_array(_playerSlots)) {
        show_debug_message("[AI_GUARD] WARNING: _playerSlots is invalid — AI may have no valid targets");
        // Not fatal — continue with empty targets
        _playerSlots = [];
    }

    if (is_undefined(_opponentSlots) || !is_array(_opponentSlots)) {
        show_debug_message("[AI_GUARD] WARNING: _opponentSlots is invalid — AI abilities may not resolve");
        _opponentSlots = [];
    }

    // ── Check card data is loaded ────────────────────────
    if (!ds_exists(global.base_cards) || ds_map_empty(global.base_cards)) {
        show_debug_message("[AI_GUARD] ERROR: global.base_cards not loaded — call load_all_card_data() first");
        show_debug_message("[AI_GUARD] AI cannot make decisions without card definitions");
        return AIDecision.pass();
    }

    // ── Create AI instance with difficulty guard ───────────
    var _ai = undefined;
    if (script_exists(scr_opponent_ai)) {
        _ai = opponent_ai_create(global.ai_difficulty.Journeyman); // safe default
    } else {
        show_debug_message("[AI_GUARD] ERROR: scr_opponent_ai not found — AI unavailable");
        return AIDecision.pass();
    }

    // ── Main AI call with structural guards ───────────────
    var _decision = undefined;

    try {
        // Verify ai instance is valid
        if (is_undefined(_ai) || !is_struct(_ai)) {
            show_debug_message("[AI_GUARD] ERROR: opponent_ai_create returned invalid AI instance");
            return AIDecision.pass();
        }

        // Call decide_action on the AI instance
        _decision = _ai.decide_action(_deck, _currentMana, _playerSlots, _opponentSlots);

        // Validate returned decision structure
        if (is_undefined(_decision)) {
            show_debug_message("[AI_GUARD] WARNING: decide_action returned undefined — returning pass()");
            return AIDecision.pass();
        }

        if (!is_struct(_decision)) {
            show_debug_message("[AI_GUARD] WARNING: decide_action returned non-struct (type=" + string(typeof(_decision)) + ") — returning pass()");
            return AIDecision.pass();
        }

        // Valid decision — emit observability event and return
        ai_emit_decision_event(_decision,
            global.ai_difficulty.to_string(global.ai_difficulty.Journeyman));
        return _decision;

    } catch (_e) {
        // In GML there is no true try/catch, but we guard the
        // most-likely failure points above. Any exception at this
        // point would be a structural field access on undefined.
        // We treat undefined AI output as a pass decision.
        show_debug_message("[AI_GUARD] EXCEPTION in decide_action: " + string(_e));
        show_debug_message("[AI_GUARD] AI decision failed — game continues, returning pass()");
        return AIDecision.pass();
    }
}

// --------------------------------------------------------
// AI_EvaluateBoard(playerSlots, opponentSlots)
// --------------------------------------------------------
// Returns a real score: positive = AI advantage, negative = player advantage.
// Safe wrapper around any board-evaluation logic.
// Returns 0 if evaluation fails.
function AI_EvaluateBoard(_playerSlots, _opponentSlots) {
    if (!is_array(_playerSlots) || !is_array(_opponentSlots)) {
        show_debug_message("[AI_GUARD] EvaluateBoard: invalid slot arrays — returning 0");
        return 0;
    }

    var aiScore = 0;
    var playerScore = 0;

    // Score AI creatures
    for (var i = 0; i < array_length(_opponentSlots); i++) {
        var slot = _opponentSlots[i];
        if (!is_undefined(slot) && is_struct(slot)) {
            var creature = slot.creature;
            if (!is_undefined(creature) && is_struct(creature)) {
                aiScore += (creature.power ?? 0) + (creature.health ?? 0);
            }
        }
    }

    // Score player creatures
    for (var i = 0; i < array_length(_playerSlots); i++) {
        var slot = _playerSlots[i];
        if (!is_undefined(slot) && is_struct(slot)) {
            var creature = slot.creature;
            if (!is_undefined(creature) && is_struct(creature)) {
                playerScore += (creature.power ?? 0) + (creature.health ?? 0);
            }
        }
    }

    var netScore = aiScore - playerScore;
    show_debug_message("[AI_GUARD] EvaluateBoard: AI=" + string(aiScore)
        + " Player=" + string(playerScore) + " Net=" + string(netScore));
    return netScore;
}

// --------------------------------------------------------
// OBSERVABILITY — failure traces
// --------------------------------------------------------
// Every guard failure path emits a show_debug_message at WARN level.
// Search the GMS2 console (Window > Show Console) for:
//   [AI_GUARD] ERROR  — fatal guard triggered (AI skipped)
//   [AI_GUARD] WARNING — non-fatal guard triggered (AI may be impaired)
//   [ai_decision]       — successful AI decision emitted
//
// Example failure signatures in the console:
//   [AI_GUARD] ERROR: global.base_cards not loaded
//     → Fix: call load_all_card_data() before starting battle
//   [AI_GUARD] deck is empty
//     → Fix: ensure AI deck was populated from cards.json
//   [AI_GUARD] WARNING: decide_action returned undefined
//     → Bug in OpponentAI.decide_action — check that deck/playerSlots/opponentSlots are valid
//   [AI_GUARD] EXCEPTION in decide_action
//     → Structural field access failed — check card structs have required fields

show_debug_message("[AI_GUARD] scr_ai_guard.gml loaded — AI calls are now guarded");
