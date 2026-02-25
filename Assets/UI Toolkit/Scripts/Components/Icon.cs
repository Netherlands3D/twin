using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Icon : VisualElement
    {
        private IconImage image = IconImage.Map;
        [UxmlAttribute("image")]
        public IconImage Image { get => image; set { image = value; UpdateClassList(); } }

        private ThemeColor color = ThemeColor.Blue900;
        [UxmlAttribute("color")]
        public ThemeColor Color { get => color; set { color = value; UpdateClassList(); } }

        public Icon()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            RegisterCallback<AttachToPanelEvent>(_ => UpdateClassList());
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("image-", image.ToString().ToKebabCase());
            this.ReplacePrefixedValueInClassList("tint-", color.ToString().ToKebabCase());
        }
    }
}