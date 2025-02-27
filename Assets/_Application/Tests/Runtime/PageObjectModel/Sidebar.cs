using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.E2ETesting.UI;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.Tests.PageObjectModel.InspectorPanels;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests.PageObjectModel
{
    // See the README.md for information on Page Object Models
    public class Sidebar : Element
    {
        public struct ToolButtonsCollection
        {
            public ButtonElement Layers { get; internal set; }
        }

        // Computed properties because inspectors are only there when they are open
        public struct InspectorsCollection
        {
            public LayersPanel Layers => new (E2E.FindComponentOfType<LayerUIManager>().Value);
        }

        public ToolButtonsCollection ToolButtons;
        public InspectorsCollection Inspectors;

        public Sidebar()
        {
            ToolButtons.Layers = new ButtonElement(E2E.FindComponentOnGameObject<Button>("ToolbarButton_Layers").Value);
        }
    }
}