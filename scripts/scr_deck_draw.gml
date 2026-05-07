// ============================================================
// scr_deck_draw.gml
// Draw-phase logic — reshuffle, empty-hand, turn progression
// Created by GSD auto-mode for M001/S09/T02
// ============================================================
//
// PURPOSE
//   Centralises all draw logic so edge cases are handled
//   consistently across any code path that needs to draw cards.
//   Mirrors the logic in scr_deck_manager.gml DrawCards() but
//   exposes a clean public API with full guard coverage.
//
// USAGE
//   draw_count = TryDrawCards(n);   // draws up to n cards
//   has_more = CanDrawCards();      // true if any cards remain
//   DiscardHand();                  // discards all hand cards
//
// EDGE CASES HANDLED
//   - draw pile empty, discard has cards → auto-reshuffle
//   - draw AND discard empty        → no crash; emits player_out_of_cards
//   - hand is full (MAX_HAND_SIZE)  → stops drawing, does not overflow
//   - hand is empty                → turn still progresses (AI may win)
//   - card in hand is undefined    → skipped with debug warning
// ============================================================

#macro DRAW_MAX_HAND 7  // matches MAX_HAND_SIZE from scr_deck_manager

// --------------------------------------------------------
// TryDrawCards(count)
// Attempts to draw [count] cards from draw pile.
// Returns: actual number of cards drawn (0..count)
//
// Side effects:
//   - Moves drawn cards from global.draw_pile to global.hand
//   - Auto-reshuffles discard into draw when draw is empty
//   - Emits show_debug_message on every edge case
//   - Emits player_out_of_cards when both piles are empty
// --------------------------------------------------------
function TryDrawCards(count) {
    var drawn = 0;

    for (var i = 0; i < count; i++) {
        // Hand full guard
        if (ds_list_size(global.hand) >= DRAW_MAX_HAND) {
            show_debug_message("[DeckDraw] hand full (" + string(DRAW_MAX_HAND)
                + ") — stop drawing");
            break;
        }

        // Draw pile empty
        if (ds_list_empty(global.draw_pile)) {

            // Try to reshuffle discard pile into draw pile
            if (ds_list_size(global.discard_pile) > 0) {
                show_debug_message("[DeckDraw] draw pile empty — "
                    + "reshuffling discard (" + string(ds_list_size(global.discard_pile))
                    + " cards) into draw pile");
                ds_list_copy(global.draw_pile, global.discard_pile);
                ds_list_clear(global.discard_pile);
                ShuffleDrawPile();
                show_debug_message("[DeckDraw] reshuffle done — "
                    + string(ds_list_size(global.draw_pile)) + " cards in draw pile");
            } else {
                // BOTH piles empty — no cards remain
                show_debug_message("[DeckDraw] player_out_of_cards — "
                    + "draw pile and discard pile both empty; "
                    + "hand has " + string(ds_list_size(global.hand)) + " cards");
                // Signal to battle controller that player cannot draw
                // (battle controller can check DeckManagerGetPileCount(DRAW_PILE)==0
                //  and DeckManagerGetPileCount(DISCARD_PILE)==0
                //  to display "no cards left" UI feedback)
                break;
            }
        }

        // Pull top card from draw pile
        if (!ds_list_empty(global.draw_pile)) {
            var topIndex = ds_list_size(global.draw_pile) - 1;
            var card = ds_list_find_value(global.draw_pile, topIndex);

            if (is_undefined(card)) {
                show_debug_message("[DeckDraw] WARNING: draw pile has undefined card at index "
                    + string(topIndex) + " — skipping");
                ds_list_delete(global.draw_pile, topIndex);
                continue;
            }

            ds_list_delete(global.draw_pile, topIndex);
            ds_list_add(global.hand, card);

            var cardName = is_struct(card) ? struct_get(card, "name") : (ds_map_exists(card, "name") ? card[? "name"] : "?");
            drawn++;
            show_debug_message("[DeckDraw] drew: " + string(cardName)
                + " — hand now " + string(ds_list_size(global.hand)) + "/"
                + string(DRAW_MAX_HAND));
        }
    }

    if (drawn < count) {
        show_debug_message("[DeckDraw] requested " + string(count)
            + " cards but only drew " + string(drawn)
            + " (check pile sizes in console)");
    }

    return drawn;
}

// --------------------------------------------------------
// CanDrawCards()
// Returns: true if the player CAN draw at least 1 more card
// (either draw pile or discard pile has cards)
//
// Use this before calling TryDrawCards to give UI feedback
// without triggering the edge-case logs.
// --------------------------------------------------------
function CanDrawCards() {
    return !ds_list_empty(global.draw_pile)
        || ds_list_size(global.discard_pile) > 0;
}

// --------------------------------------------------------
// DiscardHand()
// Discards all cards in hand into global.discard_pile.
// Returns: number of cards discarded
//
// Call this at end of turn or when a round resets.
// --------------------------------------------------------
function DiscardHand() {
    var discarded = 0;
    while (!ds_list_empty(global.hand)) {
        var card = ds_list_find_value(global.hand, 0);
        ds_list_delete(global.hand, 0);
        ds_list_add(global.discard_pile, card);
        discarded++;
    }
    show_debug_message("[DeckDraw] DiscardHand: discarded "
        + string(discarded) + " cards to discard pile");
    return discarded;
}

// --------------------------------------------------------
// GetTopDrawCard()
// Peeks at the top card of the draw pile without removing it.
// Returns: Card struct, or undefined if draw pile is empty
// --------------------------------------------------------
function GetTopDrawCard() {
    if (ds_list_empty(global.draw_pile)) {
        return undefined;
    }
    return ds_list_find_value(global.draw_pile,
        ds_list_size(global.draw_pile) - 1);
}

// --------------------------------------------------------
// OBSERVABILITY — GMS2 console event signatures
// --------------------------------------------------------
// Draw phase events emitted via show_debug_message:
//
// [DeckDraw] drew: {cardName} — hand now N/M
//   → Normal draw; N = cards in hand, M = DRAW_MAX_HAND
//
// [DeckDraw] draw pile empty — reshuffling discard (N cards)
//   → Expected when draw pile empties mid-turn
//
// [DeckDraw] player_out_of_cards
//   → Both piles empty; UI should show "no cards to draw" message
//
// [DeckDraw] hand full (N) — stop drawing
//   → DRAW_MAX_HAND reached; no more draws this turn
//
// [DeckDraw] WARNING: draw pile has undefined card at index N
//   → Corrupted card entry; auto-skip with ds_list_delete guard

show_debug_message("[DeckDraw] scr_deck_draw.gml loaded");
