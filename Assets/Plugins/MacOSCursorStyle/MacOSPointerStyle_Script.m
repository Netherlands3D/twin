#import <Cocoa/Cocoa.h>

// Codes shared with the C# side
enum {
    CURSOR_ARROW = 0, CURSOR_IBEAM, CURSOR_IBEAM_VERTICAL, CURSOR_CROSSHAIR,
    CURSOR_POINTING_HAND, CURSOR_OPEN_HAND, CURSOR_CLOSED_HAND,
    CURSOR_RESIZE_LR, CURSOR_RESIZE_UD,
    CURSOR_RESIZE_NWSE, CURSOR_RESIZE_NESW,
    CURSOR_NOT_ALLOWED, CURSOR_CONTEXT_MENU, CURSOR_COPY, CURSOR_ALIAS,
    CURSOR_HIDDEN
};

static BOOL g_hidden = NO;

static NSCursor *ResolveCursor(int code)
{
    switch (code)
    {
        case CURSOR_IBEAM:          return [NSCursor IBeamCursor];
        case CURSOR_IBEAM_VERTICAL: return [NSCursor IBeamCursorForVerticalLayout];
        case CURSOR_CROSSHAIR:      return [NSCursor crosshairCursor];
        case CURSOR_POINTING_HAND:  return [NSCursor pointingHandCursor];
        case CURSOR_OPEN_HAND:      return [NSCursor openHandCursor];
        case CURSOR_CLOSED_HAND:    return [NSCursor closedHandCursor];
        case CURSOR_RESIZE_LR:      return [NSCursor resizeLeftRightCursor];
        case CURSOR_RESIZE_UD:      return [NSCursor resizeUpDownCursor];
        case CURSOR_NOT_ALLOWED:    return [NSCursor operationNotAllowedCursor];
        case CURSOR_CONTEXT_MENU:   return [NSCursor contextualMenuCursor];
        case CURSOR_COPY:           return [NSCursor dragCopyCursor];
        case CURSOR_ALIAS:          return [NSCursor dragLinkCursor];

        // Diagonal resize cursors only have public API on macOS 15+
        case CURSOR_RESIZE_NWSE:
            if (@available(macOS 15.0, *))
                return [NSCursor frameResizeCursorFromPosition:NSCursorFrameResizePositionTopLeft
                                                   inDirections:NSCursorFrameResizeDirectionsAll];
            return [NSCursor arrowCursor];
        case CURSOR_RESIZE_NESW:
            if (@available(macOS 15.0, *))
                return [NSCursor frameResizeCursorFromPosition:NSCursorFrameResizePositionTopRight
                                                   inDirections:NSCursorFrameResizeDirectionsAll];
            return [NSCursor arrowCursor];

        default:                    return [NSCursor arrowCursor];
    }
}

void MacOSPointerStyle_SetCursor(int code)
{
    if (code == CURSOR_HIDDEN)
    {
        if (!g_hidden) { [NSCursor hide]; g_hidden = YES; }
        return;
    }

    if (g_hidden) { [NSCursor unhide]; g_hidden = NO; }
    [ResolveCursor(code) set];
}