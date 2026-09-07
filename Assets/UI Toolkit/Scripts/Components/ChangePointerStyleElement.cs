using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ChangePointerStyleElement : VisualElement
    {
        [DllImport("__Internal")]
        private static extern string SetCSSCursor(string cursorName = "auto");
        
        private PointerStyle.Style styleOnHover = PointerStyle.Style.POINTER;
        
        [UxmlAttribute("pointer-style-hover")]
        public PointerStyle.Style StyleOnHover { get => styleOnHover; set => styleOnHover = value; }
        
        public ChangePointerStyleElement()
        {
            this.AddComponentStylesheet("Components");

            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        private void OnPointerOver(PointerOverEvent evt)
        {
            PointerStyle.RequestCursorChange(this, styleOnHover);
        }

        private void OnPointerOut(PointerOutEvent evt)
        {
            PointerStyle.CancelCursorChange(this);
        }
    }
}