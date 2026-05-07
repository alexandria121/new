/// scr_ui_draw.gml
/// GUI drawing functions for battle UI rendering
/// Requires: scr_card_struct.gml, scr_card_enums.gml (already in scope)

/// Draw a single card at position (x, y = top-left corner)
/// card: Card struct instance
/// selected: bool, draws a highlight ring if true
function DrawCard(card, x, y, selected) {
    var CARD_W = 80;
    var CARD_H = 110;
    
    // Background (dark, slightly rounded rect approximation)
    draw_set_color(c_black);
    draw_rectangle(x, y, x + CARD_W, y + CARD_H, false);
    
    // Card border (element colour strip on left edge)
    var elemColor = GetCardElementColor(card.element);
    draw_set_color(elemColor);
    draw_rectangle(x, y, x + 6, y + CARD_H, false);
    
    // Selected highlight
    if (selected) {
        draw_set_color(make_color_rgb(255, 215, 0)); // gold
        draw_rectangle(x - 2, y - 2, x + CARD_W + 2, y + CARD_H + 2, false);
    }
    
    // Mana cost circle (top-right)
    var manaColor = c_blue;
    draw_set_color(manaColor);
    draw_circle(x + CARD_W - 10, y + 10, 10, false);
    draw_set_color(c_white);
    draw_text(x + CARD_W - 14, y + 4, string(card.manaCost));
    
    // Card name (centred, truncated if needed)
    draw_set_color(c_white);
    draw_text(x + 4, y + 18, card.name);
    
    // Power (bottom-left corner)
    draw_set_color(make_color_rgb(255, 80, 80));
    draw_text(x + 4, y + CARD_H - 14, string(card.power));
    
    // Health (bottom-right corner)
    draw_set_color(make_color_rgb(80, 255, 80));
    draw_text(x + CARD_W - 14, y + CARD_H - 14, string(card.health));
}

/// Draw a face-down card back at position
function DrawCardBack(x, y) {
    var CARD_W = 80;
    var CARD_H = 110;
    draw_set_color(make_color_rgb(30, 30, 60));
    draw_rectangle(x, y, x + CARD_W, y + CARD_H, false);
    // Diagonal pattern to indicate face-down
    draw_set_color(make_color_rgb(60, 60, 100));
    for (var i = 0; i < 4; i++) {
        draw_line(x + i * 20, y, x, y + i * 20);
        draw_line(x + CARD_W - i * 20, y + CARD_H, x + CARD_W, y + CARD_H - i * 20);
    }
}

/// Draw all cards in a hand horizontally
/// handList: ds_list of Card structs
/// startX, y: top-left of first card
/// maxVisible: cap — draws up to this many cards
function DrawHand(handList, startX, y, maxVisible) {
    var count = min(ds_list_size(handList), maxVisible);
    var CARD_W = 80;
    var CARD_H = 110;
    for (var i = 0; i < count; i++) {
        DrawCard(handList[| i], startX + i * (CARD_W + 4), y, false);
    }
}

/// Draw a single board slot rectangle
/// If card is provided, draws card face-up; otherwise shows empty slot
function DrawBoardSlot(slotIndex, x, y, card, isHighlighted) {
    var SLOT_W = 90;
    var SLOT_H = 120;
    
    // Slot background
    if (isHighlighted) {
        draw_set_color(make_color_rgb(100, 100, 180));
    } else {
        draw_set_color(make_color_rgb(40, 40, 60));
    }
    draw_rectangle(x, y, x + SLOT_W, y + SLOT_H, false);
    draw_set_color(c_gray);
    draw_rectangle(x, y, x + SLOT_W, y + SLOT_H, true);
    
    if (card != undefined && card != noone) {
        var cardOffsetX = x + (SLOT_W - 80) / 2;
        var cardOffsetY = y + (SLOT_H - 110) / 2;
        DrawCard(card, cardOffsetX, cardOffsetY, isHighlighted);
    }
}

/// Draw the player's 3 creature slots
/// boardList: ds_list (max 3 cards)
function DrawPlayerBoard(boardList, startX, y) {
    var SLOT_W = 90;
    for (var i = 0; i < 3; i++) {
        var card = (i < ds_list_size(boardList)) ? boardList[| i] : noone;
        DrawBoardSlot(i, startX + i * (SLOT_W + 4), y, card, false);
    }
}

/// Draw the enemy's 3 creature slots face-down
function DrawEnemyBoard(boardList, startX, y) {
    var SLOT_W = 90;
    for (var i = 0; i < 3; i++) {
        var x = startX + i * (SLOT_W + 4);
        draw_set_color(make_color_rgb(40, 40, 60));
        draw_rectangle(x, y, x + SLOT_W, y + 120, false);
        draw_set_color(c_gray);
        draw_rectangle(x, y, x + SLOT_W, y + 120, true);
        if (i < ds_list_size(boardList)) {
            DrawCardBack(x + 5, y + 5);
        }
    }
}

/// Draw a health bar with label and value text
/// isPlayer: colour direction (green = player health bar goes left-to-right)
function DrawHealthBar(current, max, x, y, label, isPlayer) {
    var BAR_W = 200;
    var BAR_H = 16;
    var ratio = (max > 0) ? clamp(current / max, 0, 1) : 0;
    
    // Label
    draw_set_color(c_white);
    draw_text(x, y, label + ": " + string(current) + "/" + string(max));
    
    // Background
    draw_set_color(make_color_rgb(60, 20, 20));
    draw_rectangle(x, y + 14, x + BAR_W, y + 14 + BAR_H, false);
    
    // Fill
    if (isPlayer) {
        draw_set_color(make_color_rgb(80, 220, 80)); // green for player
    } else {
        draw_set_color(make_color_rgb(220, 80, 80)); // red for enemy
    }
    var fillW = round(BAR_W * ratio);
    draw_rectangle(x, y + 14, x + fillW, y + 14 + BAR_H, false);
}

/// Draw a mana bar
function DrawManaBar(current, max, x, y) {
    var BAR_W = 160;
    var BAR_H = 10;
    var ratio = (max > 0) ? clamp(current / max, 0, 1) : 0;
    
    draw_set_color(c_white);
    draw_text(x, y, "Mana: " + string(current) + "/" + string(max));
    
    draw_set_color(make_color_rgb(20, 20, 60));
    draw_rectangle(x, y + 12, x + BAR_W, y + 12 + BAR_H, false);
    
    draw_set_color(make_color_rgb(60, 60, 220));
    var fillW = round(BAR_W * ratio);
    draw_rectangle(x, y + 12, x + fillW, y + 12 + BAR_H, false);
}

/// Draw a scrolling game log showing the last N messages
/// logList: ds_list of strings
/// x, y: top-left of log area
/// width, height: dimensions of the log region
function DrawGameLog(logList, x, y, width, height) {
    // Background
    draw_set_color(make_color_rgb(20, 20, 30));
    draw_rectangle(x, y, x + width, y + height, false);
    draw_set_color(c_dkgray);
    draw_rectangle(x, y, x + width, y + height, true);
    
    // Content — last 20 messages
    var startIndex = max(0, ds_list_size(logList) - 20);
    draw_set_color(c_ltgray);
    var lineH = 14;
    for (var i = startIndex; i < ds_list_size(logList); i++) {
        var msgY = y + 6 + (i - startIndex) * lineH;
        if (msgY < y + height - lineH) {
            draw_text(x + 4, msgY, logList[| i]);
        }
    }
}

/// Draw a clickable button
/// Returns true if (mx, my) is inside bounds
/// hover: bool — changes button appearance
function DrawButton(label, x, y, width, height, hover) {
    if (hover) {
        draw_set_color(make_color_rgb(80, 120, 200));
    } else {
        draw_set_color(make_color_rgb(50, 50, 80));
    }
    draw_rectangle(x, y, x + width, y + height, false);
    draw_set_color(c_white);
    draw_rectangle(x, y, x + width, y + height, true);
    var textW = string_width(label);
    var textH = string_height(label);
    draw_text(x + (width - textW) / 2, y + (height - textH) / 2, label);
}
