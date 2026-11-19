#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Netherlands3D
{
    internal static class WindowsCursor
    {
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        // Standard cursor IDs (IDC_*)
        private const int IDC_ARROW = 32512;
        private const int IDC_IBEAM = 32513;
        private const int IDC_WAIT = 32514;
        private const int IDC_CROSS = 32515;
        private const int IDC_SIZENWSE = 32642;
        private const int IDC_SIZENESW = 32643;
        private const int IDC_SIZEWE = 32644;
        private const int IDC_SIZENS = 32645;
        private const int IDC_SIZEALL = 32646;
        private const int IDC_NO = 32648;
        private const int IDC_HAND = 32649;
        private const int IDC_APPSTARTING = 32650;
        private const int IDC_HELP = 32651;

        public static void Apply(PointerStyle.Style style)
        {
            int id = style switch
            {
                PointerStyle.Style.POINTER => IDC_HAND,
                PointerStyle.Style.TEXT or PointerStyle.Style.VERTICAL_TEXT => IDC_IBEAM,
                PointerStyle.Style.WAIT => IDC_WAIT,
                PointerStyle.Style.PROGRESS => IDC_APPSTARTING,
                PointerStyle.Style.CROSSHAIR or PointerStyle.Style.CELL => IDC_CROSS,
                PointerStyle.Style.HELP => IDC_HELP,
                PointerStyle.Style.NOT_ALLOWED or PointerStyle.Style.NO_DROP => IDC_NO,
                PointerStyle.Style.MOVE or PointerStyle.Style.ALL_SCROLL
                    or PointerStyle.Style.GRAB or PointerStyle.Style.GRABBING => IDC_SIZEALL,
                PointerStyle.Style.COL_RESIZE or PointerStyle.Style.E_RESIZE
                    or PointerStyle.Style.W_RESIZE or PointerStyle.Style.EW_RESIZE => IDC_SIZEWE,
                PointerStyle.Style.ROW_RESIZE or PointerStyle.Style.N_RESIZE
                    or PointerStyle.Style.S_RESIZE or PointerStyle.Style.NS_RESIZE => IDC_SIZENS,
                PointerStyle.Style.NE_RESIZE or PointerStyle.Style.SW_RESIZE
                    or PointerStyle.Style.NESW_RESIZE => IDC_SIZENESW,
                PointerStyle.Style.NW_RESIZE or PointerStyle.Style.SE_RESIZE
                    or PointerStyle.Style.NWSE_RESIZE => IDC_SIZENWSE,
                _ => IDC_ARROW
            };

            SetCursor(LoadCursor(IntPtr.Zero, id));
        }
    }
}
#endif