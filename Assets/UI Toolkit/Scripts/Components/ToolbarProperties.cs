using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarProperties : VisualElement
    {
        // TODO: clean-up OpenLibrary and AddToLibrary after removing buttons 
        // also clean-up corresponding behaviours in PropertiesPanelBehaviour.cs
        public Toggle Information => this.Q<Toggle>("Information");
        public Toggle Properties => this.Q<Toggle>("Properties");
        public Toggle Styles => this.Q<Toggle>("Styles");

        public EventCallback<ChangeEvent<bool>> OnOpenInformationToggled { get; set; }
        public EventCallback<ChangeEvent<bool>> OnPropertiesToggled { get; set; }
        public EventCallback<ChangeEvent<bool>> OnStylesToggled { get; set; }

        public ToolbarProperties()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            // Register exposed events
        }
    }
}
