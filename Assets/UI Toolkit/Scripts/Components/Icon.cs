using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Icon : VisualElement
    {
        public enum IconImage
        {
            Plus,
            Map,
            Folder,
            Trash,
        }

        public enum IconColor
        {
            White,
            Black,
            Blue50,
            Blue100,
            Blue200,
            Blue300,
            Blue700,
            Blue900,
        }

        private IconImage image = IconImage.Plus;
        [UxmlAttribute("image")]
        public IconImage Image
        {
            get => image;
            set => this.SetFieldValueAndReplaceClassName(ref image, value, "image-");
        }

        private IconColor color = IconColor.Black;
        [UxmlAttribute("color")]
        public IconColor Color
        {
            get => color;
            set => this.SetFieldValueAndReplaceClassName(ref color, value, "tint-");
        }

        public Icon()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(Icon));
            asset.CloneTree(this);
        
            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(Icon));
            styleSheets.Add(styleSheet);
        }
    }
}