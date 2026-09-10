using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D
{
    public static class PointerStyle
    {
        public class StyleRequest
        {
            public StyleRequest(object requestingObject, Style cursorStyle)
            {
                this.requestingObject = requestingObject;
                this.cursorStyle = cursorStyle;
            }

            public object requestingObject;
            public Style cursorStyle;
        }

        [DllImport("__Internal")]
        private static extern string SetCSSCursor(string cursorName = "auto");
        private static List<object> activeStyleRequestObjects = new();
        private static Dictionary<object, Style> requestedStyles = new();

        public enum Style
        {
            AUTO,
            DEFAULT,
            NONE,
            CONTEXT_MENU,
            HELP,
            POINTER,
            PROGRESS,
            WAIT,
            CELL,
            CROSSHAIR,
            TEXT,
            VERTICAL_TEXT,
            ALIAS,
            COPY,
            MOVE,
            NO_DROP,
            NOT_ALLOWED,
            GRAB,
            GRABBING,
            ALL_SCROLL,
            COL_RESIZE,
            ROW_RESIZE,
            N_RESIZE,
            NE_RESIZE,
            E_RESIZE,
            SE_RESIZE,
            S_RESIZE,
            SW_RESIZE,
            W_RESIZE,
            NW_RESIZE,
            EW_RESIZE,
            NS_RESIZE,
            NESW_RESIZE,
            NWSE_RESIZE
        }


        public static void RequestCursorChange(object requestingObject, Style style)
        {
            var requestExists = activeStyleRequestObjects.Contains(requestingObject);
            if(requestExists)
            {
                requestedStyles[requestingObject] = style;
            }
            else
            {
                activeStyleRequestObjects.Add(requestingObject);
                requestedStyles.Add(requestingObject, style);
            }

            UpdateCursor();
        }

        public static void CancelCursorChange(object unlockingObject)
        {
            activeStyleRequestObjects.Remove(unlockingObject);
            requestedStyles.Remove(unlockingObject);

            UpdateCursor();
        }

        private static void UpdateCursor()
        {
            if(activeStyleRequestObjects.Count == 0)
            {
                ChangeCursor(Style.AUTO);
                return;
            }
            
            var activeRequest = activeStyleRequestObjects[0];
            var style = requestedStyles[activeRequest];
            ChangeCursor(style);
        }

        private static void ChangeCursor(Style type)
        {
            var cursorString = "";
            switch (type)
            {
                case Style.AUTO:
                    cursorString = "auto";
                    break;
                case Style.DEFAULT:
                    cursorString = "default";
                    break;
                case Style.NONE:
                    cursorString = "none";
                    break;
                case Style.CONTEXT_MENU:
                    cursorString = "context-menu";
                    break;
                case Style.HELP:
                    cursorString = "help";
                    break;
                case Style.POINTER:
                    cursorString = "pointer";
                    break;
                case Style.PROGRESS:
                    cursorString = "progress";
                    break;
                case Style.WAIT:
                    cursorString = "wait";
                    break;
                case Style.CELL:
                    cursorString = "cell";
                    break;
                case Style.CROSSHAIR:
                    cursorString = "crosshair";
                    break;
                case Style.TEXT:
                    cursorString = "text";
                    break;
                case Style.VERTICAL_TEXT:
                    cursorString = "vertical-text";
                    break;
                case Style.ALIAS:
                    cursorString = "alias";
                    break;
                case Style.COPY:
                    cursorString = "copy";
                    break;
                case Style.MOVE:
                    cursorString = "move";
                    break;
                case Style.NO_DROP:
                    cursorString = "no-drop";
                    break;
                case Style.NOT_ALLOWED:
                    cursorString = "not-allowed";
                    break;
                case Style.GRAB:
                    cursorString = "grab";
                    break;
                case Style.GRABBING:
                    cursorString = "grabbing";
                    break;
                case Style.ALL_SCROLL:
                    cursorString = "all-scroll";
                    break;
                case Style.COL_RESIZE:
                    cursorString = "col-resize";
                    break;
                case Style.ROW_RESIZE:
                    cursorString = "row-resize";
                    break;
                case Style.N_RESIZE:
                    cursorString = "n-resize";
                    break;
                case Style.NE_RESIZE:
                    cursorString = "ne-resize";
                    break;
                case Style.E_RESIZE:
                    cursorString = "e-resize";
                    break;
                case Style.SE_RESIZE:
                    cursorString = "se-resize";
                    break;
                case Style.S_RESIZE:
                    cursorString = "s-resize";
                    break;
                case Style.SW_RESIZE:
                    cursorString = "sw-resize";
                    break;
                case Style.W_RESIZE:
                    cursorString = "w-resize";
                    break;
                case Style.NW_RESIZE:
                    cursorString = "nw-resize";
                    break;
                case Style.EW_RESIZE:
                    cursorString = "ew-resize";
                    break;
                case Style.NS_RESIZE:
                    cursorString = "ns-resize";
                    break;
                case Style.NESW_RESIZE:
                    cursorString = "nesw-resize";
                    break;
                case Style.NWSE_RESIZE:
                    cursorString = "nwse-resize";
                    break;
            }

#if !UNITY_EDITOR && UNITY_WEBGL
            SetCSSCursor(cursorString);
#else
            Debug.Log("change cursor to " + cursorString);
#endif
        }
    }
}