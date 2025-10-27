using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.JavascriptConnection
{
    public class ChangePointerStyleHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [DllImport("__Internal")]
        private static extern string SetCSSCursor(string cursorName = "auto");

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

        [SerializeField]
        private Style styleOnHover = Style.POINTER;
        public static Style cursorType = Style.AUTO;

        public Style StyleOnHover { get => styleOnHover; set => styleOnHover = value; }

        public static void ChangeCursor(Style type)
        {
            cursorType = type;

            var cursorString = "";
            switch (cursorType)
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
#endif
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ChangeCursor(StyleOnHover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Always change back cursor to CSS default 'auto'
            ChangeCursor(Style.AUTO);
        }

        public void OnDisable()
        {
            ChangeCursor(Style.AUTO);
        }
    }
}
