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
            Auto,
            Default,
            None,
            ContextMenu,
            Help,
            Pointer,
            Progress,
            Wait,
            Cell,
            Crosshair,
            Text,
            VerticalText,
            Alias,
            Copy,
            Move,
            NoDrop,
            NotAllowed,
            Grab,
            Grabbing,
            AllScroll,
            ColResize,
            RowResize,
            NResize,
            NeResize,
            EResize,
            SeResize,
            SResize,
            SwResize,
            WResize,
            NwResize,
            EwResize,
            NsResize,
            NeswResize,
            NwseResize
        }
        
        private PointerStyle styleOnHover = PointerStyle.Pointer;
        public static PointerStyle pointerType = PointerStyle.Auto;
        
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
            ChangeCursor(PointerStyle.Auto);
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