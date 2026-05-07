// ============================================================
// scr_mobile_check.gml
// Mobile export readiness — platform detection, touch input
// shim, and UI scale helpers for iOS/Android export.
// Created by GSD auto-mode for M001/S09/T03
// ============================================================
//
// PURPOSE
//   Provides a single source of truth for mobile platform
//   detection so UI scripts can adjust touch target sizes,
//   drag thresholds, font scaling, and interaction styles
//   without duplicating os_type checks across the codebase.
//
// USAGE
//   if (IsMobile())            { /* touch-safe layout */ }
//   var scale = GetMobileScale(); // 1.0–1.5 multiplier
//   var touchOK = TouchTargetOk(width, height); // min 48px
//
// LICENSE NOTE
//   Full native iOS/Android builds require GameMaker Studio 2
//   Mobile module and appropriate YoYo Games licenses.
//   HTML5 export is available without additional licenses
//   and also uses touch input (handled by this script).
// ============================================================

// --------------------------------------------------------
// IsMobile()
// Returns: true if running on iOS or Android
//          (also true for HTML5 export on mobile browsers)
//
// Use this to switch UI scale, drag thresholds, or input
// style without touching individual UI scripts.
// --------------------------------------------------------
function IsMobile() {
    return (os_type == os_ios)
        || (os_type == os_android)
        || (os_type == os_windowsphone);  // legacy but included for completeness
}

// --------------------------------------------------------
// IsTouchDevice()
// Returns: true if device has touch input available
//          (mobile + HTML5 on touch devices)
//
// Use for UI elements that must work with both mouse and touch.
// --------------------------------------------------------
function IsTouchDevice() {
    // device_input_type is only available in some GMS2 versions;
    // fallback to IsMobile() for broad compatibility.
    // GMS2 2023+ supports device_input_type constant:
    //   0 = unknown, 1 = mouse, 2 = touch, 3 = pen
    if (variable_global_exists("device_input_type")) {
        return (global.device_input_type == 2);  // touch
    }
    // Fallback: assume mobile platforms are touch-capable
    return IsMobile();
}

// --------------------------------------------------------
// GetMobileScale()
// Returns: float multiplier for UI scaling on mobile.
//
// Logic:
//   - Landscape portrait-short side reference: 720px
//   - Scale up when the actual short side exceeds 720
//   - Caps at 1.5× to avoid oversized UI on tablets
//
// Call this in Draw events before drawing any UI element
// that has hardcoded pixel sizes (card dims, button dims).
// --------------------------------------------------------
function GetMobileScale() {
    var shortSide = min(display_get_width(), display_get_height());
    if (shortSide <= 0) return 1.0;
    var raw = shortSide / 720.0;
    return min(clamp(raw, 1.0, 1.5), 1.5);  // max 1.5× tablet scale
}

// --------------------------------------------------------
// GetDPIRank()
// Returns: 0=low, 1=medium, 2=high (for font/image scaling)
// --------------------------------------------------------
function GetDPIRank() {
    var dpi = display_get_dpi();
    if (dpi >= 320) return 2;  // high-DPI tablet / flagship phone
    if (dpi >= 200) return 1;  // standard phone
    return 0;                  // low-DPI / small screen
}

// --------------------------------------------------------
// TouchTargetOk(width, height)
// Returns: true if (width × height) is at least 48×48 px
//          (WCAG 2.1 Level A touch target minimum).
//
// Use to validate button/card sizes before drawing or
// to log a warning if a UI element is too small for touch.
// --------------------------------------------------------
function TouchTargetOk(width, height) {
    var minTarget = 48;
    return (width >= minTarget) && (height >= minTarget);
}

// --------------------------------------------------------
// LogMobileInfo()
// Emits platform information to GMS2 console for debugging.
// Call once in obj_global_system Create event.
// --------------------------------------------------------
function LogMobileInfo() {
    show_debug_message("[MobileCheck] os_type = " + string(os_type)
        + " | IsMobile = " + string(IsMobile())
        + " | IsTouchDevice = " + string(IsTouchDevice())
        + " | scale = " + string(GetMobileScale())
        + " | DPI rank = " + string(GetDPIRank())
        + " | display = " + string(display_get_width())
        + "x" + string(display_get_height())
        + " | dpi = " + string(display_get_dpi()));
}

// --------------------------------------------------------
// OBSERVABILITY — GMS2 console event signatures
// --------------------------------------------------------
// [MobileCheck] os_type = N | IsMobile = bool | IsTouchDevice = bool | scale = X | DPI rank = N | display = WxH | dpi = D
//   → Emitted by LogMobileInfo() for failure diagnosis

show_debug_message("[MobileCheck] scr_mobile_check.gml loaded");