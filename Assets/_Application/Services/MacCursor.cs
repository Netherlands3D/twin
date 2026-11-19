#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.Runtime.InteropServices;

namespace Netherlands3D.Cursor
{
    internal static class MacCursor
    {
        [DllImport("MacOSPointerStyle")]
        private static extern void MacOSPointerStyle_SetCursor(int code);

        public static void Apply(PointerStyle.Style style)
        {
            int code = style switch
            {
                PointerStyle.Style.NONE => 15,
                PointerStyle.Style.TEXT => 1,
                PointerStyle.Style.VERTICAL_TEXT => 2,
                PointerStyle.Style.CROSSHAIR or PointerStyle.Style.CELL => 3,
                PointerStyle.Style.POINTER => 4,
                PointerStyle.Style.GRAB => 5,
                PointerStyle.Style.GRABBING => 6,
                PointerStyle.Style.COL_RESIZE or PointerStyle.Style.E_RESIZE
                    or PointerStyle.Style.W_RESIZE or PointerStyle.Style.EW_RESIZE => 7,
                PointerStyle.Style.ROW_RESIZE or PointerStyle.Style.N_RESIZE
                    or PointerStyle.Style.S_RESIZE or PointerStyle.Style.NS_RESIZE => 8,
                PointerStyle.Style.NW_RESIZE or PointerStyle.Style.SE_RESIZE
                    or PointerStyle.Style.NWSE_RESIZE => 9,
                PointerStyle.Style.NE_RESIZE or PointerStyle.Style.SW_RESIZE
                    or PointerStyle.Style.NESW_RESIZE => 10,
                PointerStyle.Style.NOT_ALLOWED or PointerStyle.Style.NO_DROP => 11,
                PointerStyle.Style.CONTEXT_MENU => 12,
                PointerStyle.Style.COPY => 13,
                PointerStyle.Style.ALIAS => 14,
                _ => 0 // arrow
            };

            MacOSPointerStyle_SetCursor(code);
        }
    }
}
#endif