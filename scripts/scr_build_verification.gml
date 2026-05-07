// ============================================================
// scr_build_verification.gml
// Windows .exe export procedure and smoke-test checklist
// Created by GSD auto-mode for M001/S09/T01
// ============================================================

// --------------------------------------------------------
// EXPORT PROCEDURE
// --------------------------------------------------------
// Follow these steps in GameMaker Studio 2 to produce
// MagicalDeckbuilder.exe for Windows:
//
// 1. Open MagicalDeckbuilder.yyp in GMS2 IDE.
//
// 2. Verify the Room Order (Resource Tree > Right-click game >
//    "Set Inclusion Order") matches:
//      room_menu         (0)  — Main menu
//      room_battle_select(1)  — Battle mode selection
//      room_gallery      (2)  — Card gallery view
//      room_deck_builder (3)  — Deck builder
//      room_battle       (4)  — Main battle arena
//
// 3. Verify Script Order in project.yy includes:
//      scripts/scr_card_data_loader.gml  (loads cards.json)
//      scripts/scr_game_state.gml         (game state globals)
//      scripts/scr_deck_manager.gml        (draw/discard piles)
//      scripts/scr_opponent_ai.gml         (AI decision engine)
//    Additional scripts found in scripts/ directory
//    should be added to the resource tree if referenced
//    by objects.
//
// 4. Mark Included Files (for .exe bundling):
//      In GMS2 Resource Tree, right-click "Included Files" >
//      "Add Existing Files" and add:
//        datafiles/cards.json    (card database — 265+ entries)
//        datafiles/combos.json  (combo recipes — 265+ entries)
//      Without these, the game will crash at startup with
//      "Failed to load card data".
//
// 5. Build: File > Create Executable >
//    Platform: Windows | Output: Standalone
//    Architecture: x64 (or x86 if targeting 32-bit)
//    Click "Produce Executable" and choose output path.
//
// 6. Launch the .exe outside the IDE to verify.
//
// --------------------------------------------------------
// SMOKE-TEST CHECKLIST
// --------------------------------------------------------
// Run these checks within 10 seconds of .exe launch.
// All items must pass for the build to be considered verified.
//
// [ ] 1. STARTUP: .exe launches without crash or error dialog
// [ ] 2. MENU: Main menu room (room_menu) is shown first
// [ ] 3. MENU NAV: "Start Battle" or battle button navigates
//         to room_battle_select
// [ ] 4. NAV — GALLERY: Gallery button navigates to room_gallery
// [ ] 5. NAV — DECK BUILDER: Deck Builder button navigates to
//         room_deck_builder
// [ ] 6. BATTLE SELECT: battle_select room shows AI difficulty
//         options and a "Fight" or "Start" button
// [ ] 7. BATTLE INIT: Tapping Fight starts room_battle and
//         initialises global.player_deck from cards.json
// [ ] 8. DRAW: At battle start, player hand shows cards
//         drawn from global.player_deck (check GMS2 console
//         for "[DeckManager] DrawCards: drew X" messages)
// [ ] 9. AI TURN: After player's first turn, AI opponent
//         takes a turn without crashing (check console for
//         "[ai_decision] type=..." messages)
// [ ] 10. WIN/LOSE: When a player's health reaches 0, the
//          win/lose screen appears with correct result text
// [ ] 11. CLEAN EXIT: Closing the game window shuts down
//          cleanly — no freeze or zombie process
//
// --------------------------------------------------------
// KNOWN FAILURE POINTS
// --------------------------------------------------------
// If the build fails at export time:
//   - Verify all .gml scripts end without fatal parse errors
//   - Check the GMS2 Output console for the first error message
//   - Common: ds_map_find_value on undefined map → add null check
//   - Common: ds_list_find_value out of bounds → use ds_list_size guard
//
// If .exe crashes on startup:
//   - Check cards.json and combos.json are marked as Included Files
//   - Run GMS2 IDE and open the same project — if it crashes
//     in the IDE it will crash in the exe too
//
// If menu buttons don't work:
//   - obj_menu_controller handles clickable buttons via
//     mouse_check_button_pressed(mb_left)
//   - Verify room_menu has been created and its Create event
//     calls menu_setup() or equivalent
//
// If AI crashes during battle:
//   - Ensure scr_opponent_ai.gml is in the script order
//   - If still failing, add a guard in the battle controller
//     before calling opponent_ai_create():
//     if (script_exists(scr_opponent_ai)) { /* safe to call */ }
//
// --------------------------------------------------------
// DEBUG OVERLAY (optional in-game diagnostic)
// --------------------------------------------------------
// Add this to obj_battle_controller Draw GUI event for
// an in-game debug overlay:
/ *
    // Uncomment in IDE to see current game state:
    draw_set_color(c_white);
    draw_text(10, 10,
        "Phase: "   + string(global.game_phase   ?? "n/a") +
        " | Turn: " + string(global.turn_count   ?? 0) +
        " | Deck: " + string(DeckManagerGetPileCount(DRAW_PILE)) +
        " | Hand: " + string(DeckManagerGetPileCount(HAND)) +
        " | Discard: " + string(DeckManagerGetPileCount(DISCARD_PILE))
    );
* /

show_debug_message("[BuildVerification] scr_build_verification.gml loaded — export steps and checklist documented");
