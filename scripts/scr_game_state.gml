// ============================================================
// scr_game_state.gml
// Game loop, turn system, and phase management
// Slice S06 — Game loop and turn system
// ============================================================

// --------------------------------------------------------
// Phase enum — mirrors GamePhase in the shared library
// --------------------------------------------------------
enum Phase {
    DRAW   = 0,
    MAIN   = 1,
    COMBAT = 2,
    END_TURN = 3
}

// --------------------------------------------------------
// GameState struct — all mutable game data lives here
// --------------------------------------------------------
function GameState() constructor {
    // Phase tracking
    currentPhase = Phase.DRAW;
    
    // Mana
    currentMana = 0;
    maxMana     = 0;
    
    // Turn tracking
    turnCount    = 0;
    currentPlayer = 0;  // 0 = player, 1 = AI
    
    // Health (both characters start at 30)
    playerHealth = 30;
    aiHealth    = 30;
    
    // Board reference (set externally by scr_board_state or similar)
    playerBoard = ds_list_create();
    aiBoard     = ds_list_create();
    
    // Hand and deck (managed by scr_deck_manager)
    playerHand = ds_list_create();
    aiHand     = ds_list_create();
    
    // Game log
    gameLog = ds_list_create();
}

// --------------------------------------------------------
// Global singleton — persisted across all rooms
// --------------------------------------------------------
global.game = new GameState();

// --------------------------------------------------------
// Logging helper
// --------------------------------------------------------
function _gs_log(msg) {
    ds_list_add(global.game.gameLog, msg);
    show_debug_message("[GAME] " + msg);
    event_log("game_event", msg);  // GMS native broadcast
}

// --------------------------------------------------------
// T01: InitializeGame()
// Sets all state, draws initial hands (4 cards each),
// logs the game_started event.
// --------------------------------------------------------
function InitializeGame() {
    var g = global.game;
    
    g.currentPhase  = Phase.DRAW;
    g.currentMana   = 1;
    g.maxMana        = 1;
    g.turnCount       = 1;
    g.currentPlayer  = 0;
    g.playerHealth    = 30;
    g.aiHealth        = 30;
    
    // Clear any stale board/hand data
    ds_list_clear(g.playerBoard);
    ds_list_clear(g.aiBoard);
    ds_list_clear(g.playerHand);
    ds_list_clear(g.aiHand);
    ds_list_clear(g.gameLog);
    
    // Draw starting hands (4 cards each)
    var pDeck = global.playerDeck;
    var aDeck = global.aiDeck;
    
    if (ds_exists(pDeck, ds_type_list)) {
        for (var i = 0; i < 4; i++) {
            var card = DeckDrawCard(pDeck);
            if (!is_undefined(card) && card != noone) {
                ds_list_add(g.playerHand, card);
            }
        }
    }
    
    if (ds_exists(aDeck, ds_type_list)) {
        for (var i = 0; i < 4; i++) {
            var card = DeckDrawCard(aDeck);
            if (!is_undefined(card) && card != noone) {
                ds_list_add(g.aiHand, card);
            }
        }
    }
    
    _gs_log("Game started! Player health: " + string(g.playerHealth)
          + " | AI health: " + string(g.aiHealth)
          + " | Turn " + string(g.turnCount));
    
    event_log("game_started",
              "playerHealth=" + string(g.playerHealth)
            + ",aiHealth="   + string(g.aiHealth)
            + ",turnCount="  + string(g.turnCount));
}

// ============================================================
// T02: Turn management and phase progression
// ============================================================

// --------------------------------------------------------
// StartTurn()
// currentPlayer gains 1 mana (capped at 10), draws 1 card,
// sets phase to MAIN, logs the phase_change event.
// --------------------------------------------------------
function StartTurn() {
    var g = global.game;
    
    // Gain mana
    g.maxMana = min(g.maxMana + 1, 10);
    g.currentMana = g.maxMana;
    
    // Draw a card
    if (g.currentPlayer == 0) {
        var deck = global.playerDeck;
        var card  = DeckDrawCard(deck);
        if (!is_undefined(card) && card != noone) {
            ds_list_add(g.playerHand, card);
            _gs_log("Player drew a card.");
        }
    } else {
        var deck = global.aiDeck;
        var card  = DeckDrawCard(deck);
        if (!is_undefined(card) && card != noone) {
            ds_list_add(g.aiHand, card);
            _gs_log("AI drew a card.");
        }
    }
    
    g.currentPhase = Phase.MAIN;
    _gs_log("Turn " + string(g.turnCount) + ": "
          + (g.currentPlayer == 0 ? "Player" : "AI")
          + "'s turn begins. Mana: " + string(g.currentMana) + "/" + string(g.maxMana));
    
    event_log("phase_change", "phase=MAIN,player=" + string(g.currentPlayer)
            + ",mana=" + string(g.currentMana));
}

// --------------------------------------------------------
// PlayCreature(cardId, slotIndex)
// Validates mana cost and slot availability, summons the
// creature via deck.PlayCreatureToSlot, deducts mana.
// --------------------------------------------------------
function PlayCreature(cardId, slotIndex) {
    var g = global.game;
    
    // Resolve the card from the hand
    var hand = (g.currentPlayer == 0) ? g.playerHand : g.aiHand;
    var card  = noone;
    for (var i = ds_list_size(hand) - 1; i >= 0; i--) {
        var c = ds_list_find_value(hand, i);
        if (!is_undefined(c) && c != noone && c.cardId == cardId) {
            card = c;
            break;
        }
    }
    
    if (card == noone) {
        _gs_log("PlayCreature: card " + string(cardId) + " not found in hand.");
        return false;
    }
    
    // Mana check
    if (g.currentMana < card.manaCost) {
        _gs_log("Not enough mana! Need " + string(card.manaCost)
              + ", have " + string(g.currentMana));
        return false;
    }
    
    // Board slot check (0–2 for each side)
    var board = (g.currentPlayer == 0) ? g.playerBoard : g.aiBoard;
    if (slotIndex < 0 || slotIndex >= ds_list_size(board)) {
        _gs_log("Invalid slot index: " + string(slotIndex));
        return false;
    }
    
    // Slot must be empty
    if (ds_list_find_value(board, slotIndex) != noone) {
        _gs_log("Slot " + string(slotIndex) + " is already occupied.");
        return false;
    }
    
    // Place on board
    ds_list_set(board, slotIndex, card);
    ds_list_delete(hand, i);
    g.currentMana -= card.manaCost;
    
    _gs_log((g.currentPlayer == 0 ? "Player" : "AI")
          + " played " + card.name + " to slot " + string(slotIndex)
          + " (cost " + string(card.manaCost) + " mana).");
    
    event_log("phase_change", "phase=MAIN,action=PlayCreature,slot="
            + string(slotIndex) + ",cardId=" + string(cardId));
    
    return true;
}

// --------------------------------------------------------
// PlaySpell(cardId, targetId)
// Validates mana, applies spell effect (damage/heal),
// deducts mana.
// --------------------------------------------------------
function PlaySpell(cardId, targetId) {
    var g = global.game;
    
    var hand = (g.currentPlayer == 0) ? g.playerHand : g.aiHand;
    var card  = noone;
    for (var i = 0; i < ds_list_size(hand); i++) {
        var c = ds_list_find_value(hand, i);
        if (!is_undefined(c) && c != noone && c.cardId == cardId) {
            card = c;
            break;
        }
    }
    
    if (card == noone) {
        _gs_log("PlaySpell: card " + string(cardId) + " not in hand.");
        return false;
    }
    
    if (g.currentMana < card.manaCost) {
        _gs_log("Not enough mana for spell!");
        return false;
    }
    
    g.currentMana -= card.manaCost;
    
    // Apply spell effects (damage to target, or heal)
    if (card.effectType == EffectType.DAMAGE || card.effectType == EffectType.BURN) {
        var amount = card.effectValue;
        DealDamageToPlayer(targetId, amount);
        _gs_log((g.currentPlayer == 0 ? "Player" : "AI")
              + " cast " + card.name + " for " + string(amount) + " damage!");
    } else if (card.effectType == EffectType.HEAL) {
        var amount = card.effectValue;
        if (targetId == 0) {
            g.playerHealth = min(g.playerHealth + amount, 30);
        } else {
            g.aiHealth = min(g.aiHealth + amount, 30);
        }
        _gs_log((g.currentPlayer == 0 ? "Player" : "AI")
              + " healed " + string(amount) + " HP.");
    }
    
    // Remove spell from hand after casting
    for (var i = 0; i < ds_list_size(hand); i++) {
        if (ds_list_find_value(hand, i) == card) {
            ds_list_delete(hand, i);
            break;
        }
    }
    
    return true;
}

// --------------------------------------------------------
// UseAbility(cardId, abilityIndex, targetId)
// Validates ability mana cost, applies the effect.
// --------------------------------------------------------
function UseAbility(cardId, abilityIndex, targetId) {
    var g = global.game;
    
    var hand = (g.currentPlayer == 0) ? g.playerHand : g.aiHand;
    var card  = noone;
    for (var i = 0; i < ds_list_size(hand); i++) {
        var c = ds_list_find_value(hand, i);
        if (!is_undefined(c) && c != noone && c.cardId == cardId) {
            card = c;
            break;
        }
    }
    
    if (card == noone) {
        _gs_log("UseAbility: card " + string(cardId) + " not in hand.");
        return false;
    }
    
    // abilityManaCost is per-ability cost array field (if present)
    var manaCost = 0;
    if (variable_struct_exists(card, "abilityManaCost")) {
        manaCost = card.abilityManaCost[abilityIndex];
    }
    
    if (g.currentMana < manaCost) {
        _gs_log("Not enough mana for ability! Need " + string(manaCost));
        return false;
    }
    
    g.currentMana -= manaCost;
    var eff = card.effectValue;
    DealDamageToPlayer(targetId, eff);
    
    _gs_log((g.currentPlayer == 0 ? "Player" : "AI")
          + " used ability " + string(abilityIndex) + " on target "
          + string(targetId) + " for " + string(eff) + " damage!");
    
    event_log("phase_change", "phase=MAIN,action=UseAbility,cardId="
            + string(cardId) + ",abilityIndex=" + string(abilityIndex));
    
    return true;
}

// --------------------------------------------------------
// CombatPhase()
// Each creature in slot N attacks the opposing creature in
// slot N, or directly hits the opposing player if that
// slot is empty.
// --------------------------------------------------------
function CombatPhase() {
    var g = global.game;
    
    _gs_log("=== COMBAT PHASE ===");
    event_log("phase_change", "phase=COMBAT,player=" + string(g.currentPlayer));
    
    var myBoard   = (g.currentPlayer == 0) ? g.playerBoard : g.aiBoard;
    var oppBoard  = (g.currentPlayer == 0) ? g.aiBoard     : g.playerBoard;
    var oppPlayer = (g.currentPlayer == 0) ? 1 : 0;
    
    for (var i = 0; i < ds_list_size(myBoard); i++) {
        var creature = ds_list_find_value(myBoard, i);
        if (is_undefined(creature) || creature == noone) continue;
        
        var opposing = ds_list_find_value(oppBoard, i);
        if (!is_undefined(opposing) && opposing != noone) {
            // Creature vs creature
            var dmg = creature.attack;
            DealDamageToPlayer(oppPlayer, dmg);
            _gs_log(creature.name + " attacks opposing creature for "
                  + string(dmg) + " damage!");
        } else {
            // No opposing creature — attack player directly
            var dmg = creature.attack;
            DealDamageToPlayer(oppPlayer, dmg);
            _gs_log(creature.name + " attacks " + (oppPlayer == 0 ? "Player" : "AI")
                  + " directly for " + string(dmg) + " damage!");
        }
    }
    
    _gs_log("=== END COMBAT ===");
}

// --------------------------------------------------------
// EndTurn()
// Triggers combat, switches currentPlayer (0↔1), increments
// turn count if AI just finished, then calls StartTurn().
// --------------------------------------------------------
function EndTurn() {
    var g = global.game;
    
    g.currentPhase = Phase.END_TURN;
    _gs_log((g.currentPlayer == 0 ? "Player" : "AI") + " ends their turn.");
    event_log("turn_end", "player=" + string(g.currentPlayer)
            + ",turn=" + string(g.turnCount));
    
    // Combat resolution before switching
    CombatPhase();
    
    // Check win condition
    if (CheckWinCondition()) return;
    
    // Switch active player
    if (g.currentPlayer == 0) {
        g.currentPlayer = 1;
    } else {
        g.currentPlayer = 0;
        g.turnCount++;
    }
    
    // Start the new player's turn
    StartTurn();
}

// --------------------------------------------------------
// DealDamageToPlayer(player, amount)
// Reduces the specified player's health. Logs damage_dealt.
// --------------------------------------------------------
function DealDamageToPlayer(player, amount) {
    var g = global.game;
    
    if (player == 0) {
        g.playerHealth = max(0, g.playerHealth - amount);
        _gs_log("Player takes " + string(amount) + " damage! ("
              + string(g.playerHealth) + " HP remaining)");
    } else {
        g.aiHealth = max(0, g.aiHealth - amount);
        _gs_log("AI takes " + string(amount) + " damage! ("
              + string(g.aiHealth) + " HP remaining)");
    }
    
    event_log("damage_dealt",
              "target=" + string(player)
            + ",amount=" + string(amount)
            + ",playerHealth=" + string(g.playerHealth)
            + ",aiHealth="    + string(g.aiHealth));
}

// ============================================================
// T03: Win condition and game log
// ============================================================

// --------------------------------------------------------
// CheckWinCondition()
// Returns true if the game is over, prints the result.
// --------------------------------------------------------
function CheckWinCondition() {
    var g = global.game;
    
    if (g.playerHealth <= 0) {
        _gs_log("═══ DEFEAT ═══ Player has fallen!");
        event_log("game_over", "result=defeat");
        GameOver(false);
        return true;
    }
    if (g.aiHealth <= 0) {
        _gs_log("═══ VICTORY ═══ AI has been defeated!");
        event_log("game_over", "result=victory");
        GameOver(true);
        return true;
    }
    return false;
}

// --------------------------------------------------------
// AddToGameLog(msg)
// Appends a message to the game log (called by other scripts).
// --------------------------------------------------------
function AddToGameLog(msg) {
    _gs_log(msg);
}

// --------------------------------------------------------
// GetGameLogText()
// Returns the full game log as a string (newlines separated).
// --------------------------------------------------------
function GetGameLogText() {
    var g   = global.game;
    var out = "";
    for (var i = 0; i < ds_list_size(g.gameLog); i++) {
        out += ds_list_find_value(g.gameLog, i) + "\n";
    }
    return out;
}

// --------------------------------------------------------
// GameOver(playerWon)
// Called by CheckWinCondition() when a player reaches 0 HP.
// Sets global flags that trigger the VICTORY/DEFEAT overlay in obj_battle_controller.
// --------------------------------------------------------
function GameOver(playerWon) {
    global.battle_complete = true;
    global.winner = playerWon ? "Player" : "AI";
    _gs_log("═══ " + (playerWon ? "VICTORY" : "DEFEAT") + " ═══");
    event_log("game_over", "winner=" + global.winner);
}

// --------------------------------------------------------
// IsGameOver()
// Returns true if either player has 0 health.
// --------------------------------------------------------
function IsGameOver() {
    var g = global.game;
    return (g.playerHealth <= 0 || g.aiHealth <= 0);
}
