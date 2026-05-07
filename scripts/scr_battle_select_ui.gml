/// scr_battle_select_ui.gml
/// Battle-select screen UI draw helpers
/// Used by obj_battle_select_controller

/// Draw the full battle-select panel (centered 600x400)
/// difficulty: current selected difficulty index (0-3)
/// battle_mode: 'pvp' or 'ai_ai'
/// hover_start: hover index for Start Battle (0=none, 1=Start, 2=Back, 3=toggle)
function DrawBattleSelectPanel(difficulty, battle_mode, hover_start) {
    var PX = display_get_width() / 2 - 300;
    var PY = display_get_height() / 2 - 200;
    var PW = 600;
    var PH = 400;

    // Panel background
    draw_set_color(make_color_rgb(20, 20, 40));
    draw_roundrect(PX, PY, PX + PW, PY + PH, 12, false);
    draw_set_color(make_color_rgb(80, 80, 120));
    draw_roundrect(PX, PY, PX + PW, PY + PH, 12, true);

    // Title
    draw_set_halign(fa_center);
    draw_set_valign(fa_top);
    draw_set_color(c_white);
    draw_set_font(fnt_title);
    draw_text(PX + PW / 2, PY + 16, "Select Opponent");
    draw_set_font(fnt_normal);

    // Difficulty section label
    draw_set_color(make_color_rgb(180, 180, 200));
    draw_text(PX + PW / 2, PY + 60, "AI Difficulty");

    // Difficulty buttons: Apprentice, Journeyman, Expert, Grandmaster
    var diffNames = ["Apprentice", "Journeyman", "Expert", "Grandmaster"];
    var BTN_W = 130;
    var BTN_H = 50;
    var totalBtnW = 4 * BTN_W + 3 * 10; // 550
    var startX = PX + (PW - totalBtnW) / 2;
    var BTN_Y = PY + 88;

    for (var i = 0; i < 4; i++) {
        var BX = startX + i * (BTN_W + 10);
        var isSelected = (i == difficulty);
        var isHover = (hover_start == (10 + i)); // hover ids 10-13

        if (isSelected) {
            draw_set_color(make_color_rgb(100, 100, 200)); // selected = blue tint
        } else if (isHover) {
            draw_set_color(make_color_rgb(70, 70, 110));
        } else {
            draw_set_color(make_color_rgb(40, 40, 70));
        }
        draw_roundrect(BX, BTN_Y, BX + BTN_W, BTN_Y + BTN_H, 6, false);

        if (isSelected) {
            draw_set_color(make_color_rgb(200, 180, 50)); // gold border for selected
        } else {
            draw_set_color(c_gray);
        }
        draw_roundrect(BX, BTN_Y, BX + BTN_W, BTN_Y + BTN_H, 6, true);

        draw_set_color(isSelected ? make_color_rgb(255, 215, 0) : c_white);
        var txtY = BTN_Y + (BTN_H - string_height(diffNames[i])) / 2;
        draw_text(BX + BTN_W / 2, txtY, diffNames[i]);
    }

    // Battle mode toggle
    var toggleY = PY + 160;
    var toggleW = 200;
    var toggleX = PX + (PW - toggleW) / 2;

    var modeLabel = (battle_mode == 'ai_ai') ? "AI vs AI" : "Human vs AI";
    var toggleHover = (hover_start == 3);

    draw_set_color(toggleHover ? make_color_rgb(80, 80, 130) : make_color_rgb(50, 50, 90));
    draw_roundrect(toggleX, toggleY, toggleX + toggleW, toggleY + 44, 6, false);
    draw_set_color(c_gray);
    draw_roundrect(toggleX, toggleY, toggleX + toggleW, toggleY + 44, 6, true);

    draw_set_color(c_white);
    draw_text(toggleX + toggleW / 2, toggleY + 14, modeLabel + " (click to toggle)");

    // Start Battle button
    var startY = PY + 240;
    var startHover = (hover_start == 1);
    var startW = 200;
    var startX = PX + (PW - startW) / 2;

    draw_set_color(startHover ? make_color_rgb(40, 120, 40) : make_color_rgb(30, 80, 30));
    draw_roundrect(startX, startY, startX + startW, startY + 60, 8, false);
    draw_set_color(c_green);
    draw_roundrect(startX, startY, startX + startW, startY + 60, 8, true);

    draw_set_color(c_white);
    var startTxt = (battle_mode == 'ai_ai') ? "Start AI vs AI" : "Start Battle";
    draw_text(startX + startW / 2, startY + 22, startTxt);

    // Back to Menu button
    var backY = PY + 320;
    var backHover = (hover_start == 2);
    var backW = 200;
    var backX = PX + (PW - backW) / 2;

    draw_set_color(backHover ? make_color_rgb(80, 40, 40) : make_color_rgb(50, 25, 25));
    draw_roundrect(backX, backY, backX + backW, backY + 50, 6, false);
    draw_set_color(c_dkgray);
    draw_roundrect(backX, backY, backX + backW, backY + 50, 6, true);

    draw_set_color(c_white);
    draw_text(backX + backW / 2, backY + 17, "Back to Menu");

    draw_set_valign(fa_top);
    draw_set_halign(fa_left);
}

/// Get the hovered element index at (mx, my)
/// Returns: 0=none, 1=StartBattle, 2=Back, 3=toggle, 10+i=difficulty[i]
function GetBattleSelectHover(mx, my) {
    var PX = display_get_width() / 2 - 300;
    var PY = display_get_height() / 2 - 200;
    var PW = 600;
    var PH = 400;

    var diffNames = ["Apprentice", "Journeyman", "Expert", "Grandmaster"];
    var BTN_W = 130;
    var BTN_H = 50;
    var totalBtnW = 4 * BTN_W + 3 * 10;
    var startX = PX + (PW - totalBtnW) / 2;
    var BTN_Y = PY + 88;

    // Difficulty buttons (ids 10-13)
    for (var i = 0; i < 4; i++) {
        var BX = startX + i * (BTN_W + 10);
        if (point_in_rectangle(mx, my, BX, BTN_Y, BX + BTN_W, BTN_Y + BTN_H)) {
            return 10 + i;
        }
    }

    // Toggle button
    var toggleY = PY + 160;
    var toggleW = 200;
    var toggleX = PX + (PW - toggleW) / 2;
    if (point_in_rectangle(mx, my, toggleX, toggleY, toggleX + toggleW, toggleY + 44)) {
        return 3;
    }

    // Start Battle
    var startY = PY + 240;
    var startW = 200;
    var startX = PX + (PW - startW) / 2;
    if (point_in_rectangle(mx, my, startX, startY, startX + startW, startY + 60)) {
        return 1;
    }

    // Back
    var backY = PY + 320;
    var backW = 200;
    var backX = PX + (PW - backW) / 2;
    if (point_in_rectangle(mx, my, backX, backY, backX + backW, backY + 50)) {
        return 2;
    }

    return 0;
}
