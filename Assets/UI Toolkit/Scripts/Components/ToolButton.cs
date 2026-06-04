using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolButton : VisualElement 
    {
        public Button Button => button;
        
        private ToolType toolType;
        private Button button;
        
        [UxmlAttribute("tooltype")]
        public ToolType ToolType
        {
            get => toolType;
            set => toolType = value;
        }
        
        [UxmlAttribute("icon")]
        public IconImage Image
        {
            get => button.Image;
            set => button.Image = value;
        }

        public ToolButton() 
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            button = this.Q<Button>();
        }
    }
}
