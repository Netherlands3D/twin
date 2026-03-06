using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MaskLayerRowElement : VisualElement
    {
        public Toggle MaskActiveToggle => this.Q<Toggle>("MaskActiveToggle");
        public Label LayerNameLabel => this.Q<Label>("LayerNameLabel");
        
        public MaskLayerRowElement()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }

        public MaskLayerRowElement(string name, bool isActive) : this()
        {
            MaskActiveToggle.value = isActive;
            LayerNameLabel.text = name;
        }
    }
}