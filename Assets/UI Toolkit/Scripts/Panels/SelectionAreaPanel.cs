using Netherlands3D.Events;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SelectionAreaPanel : VisualElement
    {
        private ListViewItem polygonButton;
        private ListViewItem lineButton;
        private ListViewItem gridButton;
        
        private PolygonCreationService polygonCreationService;
        private ToolService toolService;
        
        public SelectionAreaPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            polygonButton = this.Q<ListViewItem>("PolygonButton");
            lineButton = this.Q<ListViewItem>("LineButton");
            gridButton = this.Q<ListViewItem>("GridButton");
            
            polygonCreationService = Services.ServiceLocator.GetService<PolygonCreationService>();
            toolService = Services.ServiceLocator.GetService<ToolService>();

            lineButton.RegisterCallback<ClickEvent>(OnLineButtonClicked);
            polygonButton.RegisterCallback<ClickEvent>(OnPolygonButtonClicked);
            gridButton.RegisterCallback<ClickEvent>(OnGridButtonClicked);
        }

        private void OnPolygonButtonClicked(ClickEvent evt)
        {
            polygonButton.SetActivePseudoState(true);
            lineButton.SetActivePseudoState(false);
            polygonCreationService.SetPolygonToCreate();
        }
        
        private void OnLineButtonClicked(ClickEvent evt)
        {
            polygonButton.SetActivePseudoState(false);
            lineButton.SetActivePseudoState(true);
            polygonCreationService.SetLineInputToCreate();
        }

        private void OnGridButtonClicked(ClickEvent evt)
        {
            toolService.GetTool(ToolType.PolygonGrid).Open();
            polygonCreationService.SetPreventRemovingPolygon(false);
            polygonCreationService.SetGridInputModeToCreate();
        }
    }
}