using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class AssetLibraryListViewItem : VisualElement
    {
        private Icon icon;
        private Label label;

        public string LabelText
        {
            get => label.text;
            set => label.text = value;
        }

        public IconImage Image
        {
            get => icon.Image;
            set => icon.Image = value;
        }     
        
        public AssetLibraryListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            icon = this.Q<Icon>();
            label = this.Q<Label>();
            Image = IconImage.Object;
        }
    }
}