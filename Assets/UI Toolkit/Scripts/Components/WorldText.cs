using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldText : VisualElement
    {
        private EditableNameField nameField;
        
        public WorldText()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            nameField = this.Q<EditableNameField>();
        }
    }
}
