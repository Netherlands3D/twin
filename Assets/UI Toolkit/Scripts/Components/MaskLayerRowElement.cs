using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MaskLayerRowElement : VisualElement
    {
        public Toggle MaskActiveToggle => this.Q<Toggle>("MaskActiveToggle"); //todo: this is now wrapped in a visual element for layout, should this be a component?
        public Label LayerNameLabel => this.Q<Label>("LayerNameLabel"); //todo: this is now wrapped in a visual element for layout, should this be a component?

        public string LayerName
        {
            get => LayerNameLabel.text;
            set => LayerNameLabel.text = value;
        }
        
        public bool ToggleIsOn
        {
            get => MaskActiveToggle.value;
            set => MaskActiveToggle.value = value;
        }
        

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