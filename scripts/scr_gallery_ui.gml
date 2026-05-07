/// scr_gallery_ui.gml
/// Helper functions for gallery UI rendering

/// Draw a single gallery card at the given grid position
/// card: Card struct instance
/// x, y: top-left corner of the card slot
/// isHovered: bool, changes border highlight
function DrawGalleryCard(card, x, y, isHovered) {
    var CARD_W = 118;
    var CARD_H = 158;

    // Card background
    draw_set_color(c_black);
    draw_rectangle(x, y, x + CARD_W, y + CARD_H, false);

    // Element color border
    var elemColor = GetCardElementColor(card.element);
    if (isHovered) {
        elemColor = merge_color(elemColor, c_white, 0.3);
    }
    draw_set_color(elemColor);
    draw_rectangle(x, y, x + CARD_W, y + CARD_H, true);

    // Element strip on left
    draw_set_color(elemColor);
    draw_rectangle(x, y, x + 6, y + CARD_H, false);

    // Mana cost circle
    draw_set_color(make_color_rgb(30, 30, 80));
    draw_circle(x + CARD_W - 12, y + 12, 11, false);
    draw_set_color(c_white);
    draw_text(x + CARD_W - 16, y + 4, string(card.manaCost));

    // Card name (centered)
    draw_set_color(c_white);
    draw_set_halign(fa_center);
    draw_text(x + CARD_W / 2, y + 26, card.name);
    draw_set_halign(fa_left);

    // Stats
    draw_set_color(make_color_rgb(255, 100, 100));
    draw_text(x + 10, y + CARD_H - 28, "ATK:" + string(card.power));
    draw_set_color(make_color_rgb(100, 255, 100));
    draw_text(x + 10, y + CARD_H - 14, "DEF:" + string(card.health));
}

/// Returns the draw color for a given element string
function GetCardElementColor(element) {
    switch (element) {
        case "Fire":  return make_color_rgb(255, 100, 50);
        case "Water": return make_color_rgb(50, 150, 255);
        case "Earth": return make_color_rgb(180, 120, 60);
        case "Air":   return make_color_rgb(180, 255, 255);
        default:      return c_white;
    }
}

/// Draw a gallery filter button
/// x, y: center of the button
/// label: button text
/// isSelected: currently selected filter
/// isHover: mouse is over button
function DrawGalleryFilterButton(x, y, label, isSelected, isHover) {
    var btnW = 80;
    var btnH = 30;

    if (isSelected) {
        draw_set_color(make_color_rgb(255, 200, 50));
    } else if (isHover) {
        draw_set_color(c_dkgray);
    } else {
        draw_set_color(make_color_rgb(50, 50, 70));
    }
    draw_rectangle(x - btnW / 2, y - btnH / 2, x + btnW / 2, y + btnH / 2, false);

    draw_set_color(isSelected ? c_black : c_white);
    draw_set_halign(fa_center);
    draw_set_valign(fa_middle);
    draw_text(x, y, label);
    draw_set_halign(fa_left);
    draw_set_valign(fa_top);
}

/// Draw the gallery back button
/// Returns true if point (mx, my) is inside the button
function DrawGalleryBackButton(btnX, btnY, btnW, btnH, mx, my) {
    var isHover = (mx >= btnX && mx <= btnX + btnW && my >= btnY && my <= btnY + btnH);

    if (isHover) {
        draw_set_color(c_red);
    } else {
        draw_set_color(make_color_rgb(120, 30, 30));
    }
    draw_rectangle(btnX, btnY, btnX + btnW, btnY + btnH, false);

    draw_set_color(c_white);
    draw_set_halign(fa_center);
    draw_set_valign(fa_middle);
    draw_text(btnX + btnW / 2, btnY + btnH / 2, "Back to Menu");
    draw_set_halign(fa_left);
    draw_set_valign(fa_top);

    return isHover;
}
