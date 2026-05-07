/// scr_battle_ui.gml
/// Battle UI state struct, main draw entry point, hit-testing, and click handling
/// Requires: scr_ui_draw.gml, scr_card_struct.gml, scr_card_enums.gml

/// BattleUI constructor — stores layout constants for the battle screen
function BattleUI() {
    return {
        CARD_W:   80,
        CARD_H:   110,
        SLOT_W:    90,
        SLOT_H:    120,
        
        // Vertical positions
        BOARD_Y:     40,      // top of enemy board slots
        ENEMY_BOARD_Y: 40,
        PLAYER_BOARD_Y: 170,   // middle of screen
        HAND_Y:       460,    // near bottom
        HAND_START_X:  100,
        
        // Log area
        LOG_X:     740,
        LOG_Y:     40,
        LOG_W:     300,
        LOG_H:     440,
        
        // Button
        BTN_ENDTURN_X:    740,
        BTN_ENDTURN_Y:    490,
        BTN_ENDTURN_W:    300,
        BTN_ENDTURN_H:    44,
        
        // Health / mana bars
        PLAYER_HEALTH_X:  10,
        PLAYER_HEALTH_Y:  310,
        ENEMY_HEALTH_X:   10,
        ENEMY_HEALTH_Y:   10,
        
        // Hover state (set externally by mouse position each draw step)
        hoveredIndex: -1,
        hoveredType: ""   // 'hand', 'board', ''
    };
}

/// Main draw function — renders the complete battle UI
/// game: obj_battle_controller or a game-state struct with ds_lists
function BattleUI_Draw(game) {
    var ui = BattleUI();
    
    // Enemy board (face-down slots at top)
    if (variable_instance_exists(game, "enemyBoard")) {
        DrawEnemyBoard(game.enemyBoard, 220, ui.ENEMY_BOARD_Y);
    } else {
        DrawEnemyBoard(ds_list_create(), 220, ui.ENEMY_BOARD_Y);
    }
    
    // Enemy health bar
    if (variable_instance_exists(game, "enemyHealth") && variable_instance_exists(game, "enemyMaxHealth")) {
        DrawHealthBar(game.enemyHealth, game.enemyMaxHealth, ui.ENEMY_HEALTH_X, ui.ENEMY_HEALTH_Y, "Enemy HP", false);
    }
    
    // Player board (face-up)
    if (variable_instance_exists(game, "playerBoard")) {
        DrawPlayerBoard(game.playerBoard, 220, ui.PLAYER_BOARD_Y);
    } else {
        DrawPlayerBoard(ds_list_create(), 220, ui.PLAYER_BOARD_Y);
    }
    
    // Player health bar
    if (variable_instance_exists(game, "playerHealth") && variable_instance_exists(game, "playerMaxHealth")) {
        DrawHealthBar(game.playerHealth, game.playerMaxHealth, ui.PLAYER_HEALTH_X, ui.PLAYER_HEALTH_Y, "Your HP", true);
    }
    
    // Mana bar
    if (variable_instance_exists(game, "playerMana") && variable_instance_exists(game, "playerMaxMana")) {
        DrawManaBar(game.playerMana, game.playerMaxMana, 10, ui.PLAYER_HEALTH_Y + 24);
    }
    
    // Player hand
    if (variable_instance_exists(game, "playerHand")) {
        DrawHand(game.playerHand, ui.HAND_START_X, ui.HAND_Y, 10);
    }
    
    // Game log
    if (variable_instance_exists(game, "eventLog")) {
        DrawGameLog(game.eventLog, ui.LOG_X, ui.LOG_Y, ui.LOG_W, ui.LOG_H);
    }
    
    // End Turn button
    var hover = (ui.hoveredType == "button");
    DrawButton("End Turn", ui.BTN_ENDTURN_X, ui.BTN_ENDTURN_Y, ui.BTN_ENDTURN_W, ui.BTN_ENDTURN_H, hover);
}

/// Hit-test: returns a struct describing what's under the mouse
/// mx, my: mouse coordinates
/// game: battle controller instance
function BattleUI_HitTest(mx, my, game) {
    var ui = BattleUI();
    
    // Hand cards
    if (variable_instance_exists(game, "playerHand")) {
        var count = min(ds_list_size(game.playerHand), 10);
        for (var i = 0; i < count; i++) {
            var cx = ui.HAND_START_X + i * (ui.CARD_W + 4);
            var cy = ui.HAND_Y;
            if (point_in_rectangle(mx, my, cx, cy, cx + ui.CARD_W, cy + ui.CARD_H)) {
                return { type: "hand", index: i, card: game.playerHand[| i] };
            }
        }
    }
    
    // Board slots (player)
    if (variable_instance_exists(game, "playerBoard")) {
        for (var j = 0; j < 3; j++) {
            var sx = 220 + j * (ui.SLOT_W + 4);
            var sy = ui.PLAYER_BOARD_Y;
            if (point_in_rectangle(mx, my, sx, sy, sx + ui.SLOT_W, sy + ui.SLOT_H)) {
                var slotCard = (j < ds_list_size(game.playerBoard)) ? game.playerBoard[| j] : noone;
                return { type: "board", index: j, card: slotCard };
            }
        }
    }
    
    // End Turn button
    if (point_in_rectangle(mx, my, ui.BTN_ENDTURN_X, ui.BTN_ENDTURN_Y,
                           ui.BTN_ENDTURN_X + ui.BTN_ENDTURN_W,
                           ui.BTN_ENDTURN_Y + ui.BTN_ENDTURN_H)) {
        return { type: "button", index: -1, card: noone };
    }
    
    return { type: "", index: -1, card: noone };
}

/// Handle a mouse click on the battle UI
/// Calls PlayCreatureToSlot / EndTurn on the game controller
/// type: 'hand' | 'board' | 'button'
/// index: slot/card index (unused for button)
function BattleUI_HandleClick(game, type, index) {
    if (type == "hand") {
        if (variable_instance_exists(game, "PlayCreatureToSlot")) {
            game.PlayCreatureToSlot(index);
        }
    } else if (type == "button") {
        if (variable_instance_exists(game, "EndTurn")) {
            game.EndTurn();
        }
    }
}
