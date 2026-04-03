using System.Runtime.InteropServices;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ChangePointerStyleElement : VisualElement
    {
        [DllImport("__Internal")]
        private static extern string SetCSSCursor(string cursorName = "auto");

        public enum PointerStyle
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
        
        private PointerStyle styleOnHover = PointerStyle.POINTER;
        public static PointerStyle pointerType = PointerStyle.AUTO;
        
        [UxmlAttribute("pointer-style-hover")]
        public PointerStyle StyleOnHover { get => styleOnHover; set => styleOnHover = value; }
        
        public ChangePointerStyleElement()
        {
            // this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            ChangeCursor(StyleOnHover);
        }

        private void OnPointerOut(PointerOutEvent evt)
        {
            ChangeCursor(PointerStyle.AUTO);
        }
        
        public static void ChangeCursor(PointerStyle type)
        {
            pointerType = type;

            var cursorString = "";
            switch (pointerType)
            {
                case PointerStyle.AUTO:
                    cursorString = "auto";
                    break;
                case PointerStyle.DEFAULT:
                    cursorString = "default";
                    break;
                case PointerStyle.NONE:
                    cursorString = "none";
                    break;
                case PointerStyle.CONTEXT_MENU:
                    cursorString = "context-menu";
                    break;
                case PointerStyle.HELP:
                    cursorString = "help";
                    break;
                case PointerStyle.POINTER:
                    cursorString = "pointer";
                    break;
                case PointerStyle.PROGRESS:
                    cursorString = "progress";
                    break;
                case PointerStyle.WAIT:
                    cursorString = "wait";
                    break;
                case PointerStyle.CELL:
                    cursorString = "cell";
                    break;
                case PointerStyle.CROSSHAIR:
                    cursorString = "crosshair";
                    break;
                case PointerStyle.TEXT:
                    cursorString = "text";
                    break;
                case PointerStyle.VERTICAL_TEXT:
                    cursorString = "vertical-text";
                    break;
                case PointerStyle.ALIAS:
                    cursorString = "alias";
                    break;
                case PointerStyle.COPY:
                    cursorString = "copy";
                    break;
                case PointerStyle.MOVE:
                    cursorString = "move";
                    break;
                case PointerStyle.NO_DROP:
                    cursorString = "no-drop";
                    break;
                case PointerStyle.NOT_ALLOWED:
                    cursorString = "not-allowed";
                    break;
                case PointerStyle.GRAB:
                    cursorString = "grab";
                    break;
                case PointerStyle.GRABBING:
                    cursorString = "grabbing";
                    break;
                case PointerStyle.ALL_SCROLL:
                    cursorString = "all-scroll";
                    break;
                case PointerStyle.COL_RESIZE:
                    cursorString = "col-resize";
                    break;
                case PointerStyle.ROW_RESIZE:
                    cursorString = "row-resize";
                    break;
                case PointerStyle.N_RESIZE:
                    cursorString = "n-resize";
                    break;
                case PointerStyle.NE_RESIZE:
                    cursorString = "ne-resize";
                    break;
                case PointerStyle.E_RESIZE:
                    cursorString = "e-resize";
                    break;
                case PointerStyle.SE_RESIZE:
                    cursorString = "se-resize";
                    break;
                case PointerStyle.S_RESIZE:
                    cursorString = "s-resize";
                    break;
                case PointerStyle.SW_RESIZE:
                    cursorString = "sw-resize";
                    break;
                case PointerStyle.W_RESIZE:
                    cursorString = "w-resize";
                    break;
                case PointerStyle.NW_RESIZE:
                    cursorString = "nw-resize";
                    break;
                case PointerStyle.EW_RESIZE:
                    cursorString = "ew-resize";
                    break;
                case PointerStyle.NS_RESIZE:
                    cursorString = "ns-resize";
                    break;
                case PointerStyle.NESW_RESIZE:
                    cursorString = "nesw-resize";
                    break;
                case PointerStyle.NWSE_RESIZE:
                    cursorString = "nwse-resize";
                    break;
            }

#if !UNITY_EDITOR && UNITY_WEBGL
            SetCSSCursor(cursorString);
#endif
        }
    }
}