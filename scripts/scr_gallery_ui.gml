/// scr_gallery_ui.gml
/// Drawing helpers for the card gallery UI
/// Used by obj_gallery_controller in room_gallery

/// Draw a single element filter button
/// cx, cy: center of button; label: element name; selected: highlighted
function DrawGalleryFilterButton(cx, cy, label, selected, hover) {
    var bw = 80;
    var bh = 30;
    var bx = cx - bw / 2;
    var by = cy - bh / 2;

    if (selected) {
        draw_set_color(make_color_rgb(60, 100, 180));
    } else if (hover) {
        draw_set_color(make_color_rgb(40, 40, 70));
    } else {
        draw_set_color(make_color_rgb(25, 25, 45));
    }
    draw_roundrect(bx, by, bx + bw, by + bh, 6, false);

    if (selected) {
        draw_set_color(c_yellow);
    } else if (hover) {
        draw_set_color(c_white);
    } else {
        draw_set_color(c_ltgray);
    }
    draw_roundrect(bx, by, bx + bw, by + bh, 6, true);

    draw_set_color(selected ? c_black : c_white);
    draw_set_halign(fa_center);
    draw_text(cx, cy - 4, label);
    draw_set_halign(fa_left);
}

/// Draw a single gallery card cell (compact, grid-friendly)
/// card: simple wrapper struct with name/element/manaCost/power/health
/// x, y: top-left of cell; isHover: highlight on mouse-over
function DrawGalleryCard(card, x, y, isHover) {
    var cw = 118;
    var ch = 158;

    // Background
    draw_set_color(isHover ? make_color_rgb(35, 35, 55) : make_color_rgb(25, 25, 40));
    draw_rectangle(x, y, x + cw, y + ch, false);

    // Element strip (top edge)
    var elemColor = GetGalleryElementColor(card.element);
    draw_set_color(elemColor);
    draw_rectangle(x, y, x + cw, y + 6, false);

    // Border
    draw_set_color(isHover ? c_white : make_color_rgb(60, 60, 90));
    draw_rectangle(x, y, x + cw, y + ch, true);

    // Mana cost
    draw_set_color(c_blue);
    draw_circle(x + cw - 14, y + 14, 11, false);
    draw_set_color(c_white);
    draw_text(x + cw - 18, y + 5, string(card.manaCost));

    // Card name
    draw_set_color(c_white);
    draw_text(x + 4, y + 18, card.name);

    // Type badge (short)
    draw_set_color(make_color_rgb(80, 80, 120));
    draw_rectangle(x + 4, y + 34, x + cw - 4, y + 50, false);
    draw_set_color(c_ltgray);
    draw_text(x + 6, y + 36, "Creature");

    // Power
    draw_set_color(make_color_rgb(255, 80, 80));
    draw_text(x + 4, y + ch - 20, "P:" + string(card.power));

    // Health
    draw_set_color(make_color_rgb(80, 255, 80));
    draw_text(x + cw - 30, y + ch - 20, "H:" + string(card.health));
}

/// Returns a colour for an element name string (matching gallery filter labels)
function GetGalleryElementColor(elemName) {
    switch (elemName) {
        case "Fire":     return make_color_rgb(255, 100, 0);
        case "Water":    return make_color_rgb(50, 150, 255);
        case "Earth":    return make_color_rgb(160, 120, 60);
        case "Air":      return make_color_rgb(200, 230, 255);
        case "Neutral": return make_color_rgb(150, 150, 150);
        default:         return make_color_rgb(150, 150, 150);
    }
}

/// Draw the gallery back button
/// mx, my: current mouse position (for hover highlight)
function DrawGalleryBackButton(bx, by, bw, bh, mx, my) {
    var hover = (mx >= bx && mx <= bx + bw && my >= by && my <= by + bh);
    if (hover) {
        draw_set_color(make_color_rgb(60, 60, 100));
    } else {
        draw_set_color(make_color_rgb(30, 30, 55));
    }
    draw_roundrect(bx, by, bx + bw, by + bh, 8, false);
    draw_set_color(c_white);
    draw_roundrect(bx, by, bx + bw, by + bh, 8, true);

    var label = "Back to Menu";
    var tw = string_width(label);
    var th = string_height(label);
    draw_text(bx + (bw - tw) / 2, by + (bh - th) / 2, label);
}
