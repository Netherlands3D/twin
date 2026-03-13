using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HideObjectListViewItem : VisualElement
    {
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");
       
        public IconImage Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }
        
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("bagidtext");
      
        private string id;
        public string ID
        {
            get => id;
            set
            {
                id = value;
                Label.text = id;
            }
        }
        
        public HideObjectListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            Image = IconImage.Object;
        }
    }
}