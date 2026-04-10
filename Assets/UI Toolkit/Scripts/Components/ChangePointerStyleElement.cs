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
            var cursorString = type.ToString().ToKebabCase();
            
#if !UNITY_EDITOR && UNITY_WEBGL
            SetCSSCursor(cursorString);
#endif
        }
    }
}