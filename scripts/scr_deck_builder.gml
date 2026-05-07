// ============================================================
// scr_deck_builder.gml
// Deck Builder UI: two-panel layout with available cards,
// current deck list, filter bar, and action buttons.
// ============================================================

#macro DB_LEFT_X     20
#macro DB_LEFT_W    600
#macro DB_RIGHT_X   640
#macro DB_RIGHT_W   620
#macro DB_CARD_W     80
#macro DB_CARD_H    110
#macro DB_CARD_GAP    4
#macro DB_TOP_H      50   // Filter bar + header height
#macro DB_BOTTOM_H  50   // Back button height
#macro DB_ROW_H     (DB_CARD_H + DB_CARD_GAP)

#define DB_SCROLL_BTN_H  30
#define DB_SCROLL_BTN_W  30

// ============================================================
// GetDeckBuilderHover(mx, my)
// Returns interaction region:
//   -1 = nothing
//   0-999 = left panel card index (global.deck_list_scroll + card row)
//   1000-1999 = right panel card index
//   100 = scroll up btn, 101 = scroll down btn
//   200-208 = element filter buttons
//   300 = Save, 301 = Load, 302 = Back
// ============================================================
function GetDeckBuilderHover(mx, my) {
    // ── Scroll buttons ──────────────────────────────────
    // Scroll up: top-left corner above left panel
    if (point_in_rectangle(mx, my, DB_LEFT_X, DB_TOP_H, DB_LEFT_X + DB_SCROLL_BTN_W, DB_TOP_H + DB_SCROLL_BTN_H)) {
        return 100;
    }
    // Scroll down: below left panel
    if (point_in_rectangle(mx, my, DB_LEFT_X, room_height - DB_BOTTOM_H - DB_SCROLL_BTN_H, DB_LEFT_X + DB_SCROLL_BTN_W, room_height - DB_BOTTOM_H)) {
        return 101;
    }

    // ── Element filter bar ───────────────────────────────
    var filterY = DB_TOP_H;
    var filterH = 36;
    var elemStartX = DB_LEFT_X;
    var elemBtnW = 70;
    var elemGap = 4;
    for (var i = 0; i < 9; i++) {
        if (point_in_rectangle(mx, my, elemStartX, filterY, elemStartX + elemBtnW, filterY + filterH)) {
            return 200 + i;  // 200 = All, 201 = Element 0, ... 208 = Element 7
        }
        elemStartX += elemBtnW + elemGap;
    }

    // ── Left panel: available cards ────────────────────
    var leftTop = DB_TOP_H + 40;
    var visibleRows = floor((room_height - DB_BOTTOM_H - leftTop) / DB_ROW_H);
    var visibleCols = 7;  // cards per row (600px / 80px)
    var scroll = global.deck_list_scroll;

    for (var row = 0; row < visibleRows; row++) {
        for (var col = 0; col < visibleCols; col++) {
            var cardIdx = scroll + row;
            if (cardIdx >= ds_list_size(global.player_deck)) continue;
            var cx = DB_LEFT_X + 10 + col * (DB_CARD_W + DB_CARD_GAP);
            var cy = leftTop + row * DB_ROW_H;
            if (point_in_rectangle(mx, my, cx, cy, cx + DB_CARD_W, cy + DB_CARD_H)) {
                return 1000 + cardIdx;  // click on left panel card to add to deck
            }
        }
    }

    // ── Right panel: current deck cards ─────────────────
    var deckTop = DB_TOP_H + 40;
    var rightCols = 7;
    for (var row = 0; row < 10; row++) {   // max 10 rows visible
        for (var col = 0; col < rightCols; col++) {
            var cardIdx = col + row * rightCols;
            if (cardIdx >= ds_list_size(global.player_deck)) continue;
            var cx = DB_RIGHT_X + 10 + col * (DB_CARD_W + DB_CARD_GAP);
            var cy = deckTop + row * DB_ROW_H;
            if (point_in_rectangle(mx, my, cx, cy, cx + DB_CARD_W, cy + DB_CARD_H)) {
                return 2000 + cardIdx;  // click on right panel card to remove from deck
            }
        }
    }

    // ── Buttons ────────────────────────────────────────
    var btnY = room_height - DB_BOTTOM_H;
    var btnH = 36;
    var btnW = 120;
    var btnGap = 16;
    var btnX = DB_RIGHT_X + 10;

    // Save Deck button
    if (point_in_rectangle(mx, my, btnX, btnY, btnX + btnW, btnY + btnH)) return 300;
    btnX += btnW + btnGap;

    // Load Deck button
    if (point_in_rectangle(mx, my, btnX, btnY, btnX + btnW, btnY + btnH)) return 301;
    btnX += btnW + btnGap;

    // Back to Menu button
    if (point_in_rectangle(mx, my, btnX, btnY, btnX + btnW + 20, btnY + btnH)) return 302;

    return -1;
}

// ============================================================
// DrawDeckBuilderPanel()
// Full UI render: header, left panel (all cards), right panel
// (current deck), filter bar, and action buttons.
// ============================================================
function DrawDeckBuilderPanel() {
    var W = 1280;
    var H = 720;

    // ── Background ───────────────────────────────────────
    draw_set_color(make_color_rgb(15, 15, 25));
    draw_rectangle(0, 0, W, H, false);

    // ── Title bar ────────────────────────────────────────
    draw_set_color(make_color_rgb(30, 30, 50));
    draw_rectangle(0, 0, W, DB_TOP_H, false);
    draw_set_color(c_white);
    draw_text(W / 2 - 80, 14, "DECK BUILDER");

    // ── Left panel header ────────────────────────────────
    draw_set_color(make_color_rgb(40, 40, 65));
    draw_rectangle(DB_LEFT_X, DB_TOP_H, DB_LEFT_X + DB_LEFT_W, DB_TOP_H + 36, false);
    draw_set_color(c_white);
    draw_text(DB_LEFT_X + 8, DB_TOP_H + 10, "Available Cards");

    // ── Right panel header ──────────────────────────────
    draw_set_color(make_color_rgb(40, 40, 65));
    draw_rectangle(DB_RIGHT_X, DB_TOP_H, DB_RIGHT_X + DB_RIGHT_W, DB_TOP_H + 36, false);
    draw_set_color(c_white);
    var deckCount = ds_list_size(global.player_deck);
    draw_text(DB_RIGHT_X + 8, DB_TOP_H + 10, "Your Deck  (" + string(deckCount) + " cards)");

    // ── Element filter bar ───────────────────────────────
    DrawDeckFilterBar(DB_TOP_H + 36);

    // ── Left panel: scrollable available-cards list ───────
    var listTop = DB_TOP_H + 76;
    var listBottom = H - DB_BOTTOM_H;

    DrawDeckScrollButtons(DB_LEFT_X, listTop, listBottom);

    var visibleRows = floor((listBottom - listTop) / DB_ROW_H);
    var visibleCols = 7;
    var scroll = global.deck_list_scroll;
    var drawn = 0;

    for (var row = 0; row < visibleRows && drawn < visibleRows; row++) {
        for (var col = 0; col < visibleCols; col++) {
            var cardIdx = scroll * visibleCols + row * visibleCols + col;
            if (cardIdx >= ds_list_size(global.player_deck)) continue;
            var card = global.player_deck[| cardIdx];
            var cx = DB_LEFT_X + 10 + col * (DB_CARD_W + DB_CARD_GAP);
            var cy = listTop + drawn * DB_ROW_H;
            DrawDeckCard(card, cx, cy, false, cardIdx);
        }
        drawn++;
    }

    // ── Right panel: current deck cards ──────────────────
    var deckTop = DB_TOP_H + 76;
    var deckLeft = DB_RIGHT_X + 10;
    var rightCols = 7;

    for (var row = 0; row < 20; row++) {  // up to 20 rows
        for (var col = 0; col < rightCols; col++) {
            var cardIdx = col + row * rightCols;
            if (cardIdx >= ds_list_size(global.player_deck)) continue;
            var card = global.player_deck[| cardIdx];
            var cx = deckLeft + col * (DB_CARD_W + DB_CARD_GAP);
            var cy = deckTop + row * DB_ROW_H;
            DrawDeckCard(card, cx, cy, true, cardIdx);
        }
    }

    // ── Divider line ────────────────────────────────────
    draw_set_color(make_color_rgb(80, 80, 120));
    draw_line(DB_LEFT_X + DB_LEFT_W + 10, DB_TOP_H, DB_LEFT_X + DB_LEFT_W + 10, H - DB_BOTTOM_H);

    // ── Bottom action buttons ────────────────────────────
    DrawDeckActionButtons(H - DB_BOTTOM_H);
}

// ============================================================
// DrawDeckFilterBar(y)
// Element filter buttons bar at the top of the left panel
// ============================================================
function DrawDeckFilterBar(y) {
    var btnW = 70;
    var btnH = 30;
    var gap = 4;
    var x = DB_LEFT_X;
    var elements = ["All", "Radio", "Flesh", "Toxin", "Fungus", "Thermo", "Time", "Food", "Eldritch"];

    for (var i = 0; i < 9; i++) {
        var selected = (global.deck_filter_element == i - 1);
        if (selected) {
            draw_set_color(make_color_rgb(80, 120, 220));
        } else {
            draw_set_color(make_color_rgb(45, 45, 70));
        }
        draw_rectangle(x, y, x + btnW, y + btnH, false);
        draw_set_color(c_white);
        draw_text(x + 4, y + 8, elements[i]);
        x += btnW + gap;
    }
}

// ============================================================
// DrawDeckScrollButtons(x, top, bottom)
// Scroll up/down buttons on the left edge of card list
// ============================================================
function DrawDeckScrollButtons(x, top, bottom) {
    // Scroll up button
    var upY = top;
    var upH = 30;
    draw_set_color(make_color_rgb(50, 50, 80));
    draw_rectangle(x, upY, x + DB_SCROLL_BTN_W, upY + upH, false);
    draw_set_color(c_white);
    draw_text(x + 10, upY + 8, "^");

    // Scroll down button
    var dnY = bottom - 30;
    draw_set_color(make_color_rgb(50, 50, 80));
    draw_rectangle(x, dnY, x + DB_SCROLL_BTN_W, dnY + 30, false);
    draw_set_color(c_white);
    draw_text(x + 10, dnY + 8, "v");
}

// ============================================================
// DrawDeckCard(card, x, y, showRemove, cardIndex)
// Draws a single card in the deck builder.
// If showRemove=true, draws a red X button overlay.
// ============================================================
function DrawDeckCard(card, x, y, showRemove, cardIndex) {
    var isHovered = (global.deck_builder_hover == cardIndex);

    // Card background
    draw_set_color(make_color_rgb(25, 25, 45));
    draw_rectangle(x, y, x + DB_CARD_W, y + DB_CARD_H, false);

    // Element color strip (left edge)
    var elem = is_struct(card) ? struct_get(card, "element") : card[? "element"];
    var elemColor = GetCardElementColor(elem);
    draw_set_color(elemColor);
    draw_rectangle(x, y, x + 5, y + DB_CARD_H, false);

    // Hover highlight
    if (isHovered) {
        draw_set_color(make_color_rgb(100, 140, 255));
        draw_rectangle(x - 2, y - 2, x + DB_CARD_W + 2, y + DB_CARD_H + 2, false);
    }

    // Card name
    var name = is_struct(card) ? struct_get(card, "name") : card[? "name"];
    draw_set_color(c_white);
    draw_text(x + 8, y + 18, string(name));

    // Mana cost (top-right)
    var manaCost = is_struct(card) ? struct_get(card, "manaCost") : card[? "manaCost"];
    draw_set_color(c_blue);
    draw_circle(x + DB_CARD_W - 10, y + 10, 9, false);
    draw_set_color(c_white);
    draw_text(x + DB_CARD_W - 14, y + 2, string(manaCost));

    // Power (bottom-left)
    var power = is_struct(card) ? struct_get(card, "power") : card[? "power"];
    draw_set_color(make_color_rgb(255, 80, 80));
    draw_text(x + 4, y + DB_CARD_H - 14, "P:" + string(power));

    // Health (bottom-right)
    var health = is_struct(card) ? struct_get(card, "health") : card[? "health"];
    draw_set_color(make_color_rgb(80, 255, 80));
    draw_text(x + DB_CARD_W - 20, y + DB_CARD_H - 14, "H:" + string(health));

    // Remove button (X) on right panel cards
    if (showRemove) {
        var rx = x + DB_CARD_W - 16;
        var ry = y + 4;
        draw_set_color(c_red);
        draw_circle(rx, ry, 8, false);
        draw_set_color(c_white);
        draw_text(rx - 4, ry - 5, "X");

        // Click to remove card from deck
        if (isHovered && mouse_check_button_pressed(mb_left)) {
            ds_list_delete(global.player_deck, cardIndex);
            show_debug_message("[DeckBuilder] Removed card index " + string(cardIndex));
        }
    } else {
        // Left panel: click to add to deck
        if (isHovered && mouse_check_button_pressed(mb_left)) {
            var cardCopy = is_struct(card) ? card : card;
            ds_list_add(global.player_deck, card);
            show_debug_message("[DeckBuilder] Added " + string(name) + " to deck");
        }
    }
}

// ============================================================
// DrawDeckActionButtons(y)
// Save, Load, and Back to Menu buttons at bottom-right
// ============================================================
function DrawDeckActionButtons(y) {
    var btnH = 36;
    var btnW = 120;
    var gap = 16;
    var x = DB_RIGHT_X + 10;

    // Save Deck button
    DrawDeckButton("Save Deck", x, y, btnW, btnH, (global.deck_builder_hover == 300));
    x += btnW + gap;

    // Load Deck button
    DrawDeckButton("Load Deck", x, y, btnW, btnH, (global.deck_builder_hover == 301));
    x += btnW + gap;

    // Back to Menu button
    DrawDeckButton("Back to Menu", x, y, btnW + 20, btnH, (global.deck_builder_hover == 302));
}

// ============================================================
// DrawDeckButton(label, x, y, w, h, hover)
// Simple styled button drawer
// ============================================================
function DrawDeckButton(label, x, y, w, h, hover) {
    if (hover) {
        draw_set_color(make_color_rgb(70, 110, 200));
    } else {
        draw_set_color(make_color_rgb(45, 45, 75));
    }
    draw_rectangle(x, y, x + w, y + h, false);
    draw_set_color(c_white);
    draw_rectangle(x, y, x + w, y + h, true);
    var tw = string_width(label);
    var th = string_height(label);
    draw_text(x + (w - tw) / 2, y + (h - th) / 2, label);
}