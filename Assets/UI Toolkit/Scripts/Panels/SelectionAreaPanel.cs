using Netherlands3D.Events;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SelectionAreaPanel : VisualElement
    {
        private Button polygonButton;
        private Button lineButton;
        private Button gridButton;
        
        private PolygonCreationService polygonCreationService;
        private ToolService toolService;
        
        public SelectionAreaPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            polygonButton = this.Q<Button>("PolygonButton");
            lineButton = this.Q<Button>("LineButton");
            gridButton = this.Q<Button>("GridButton");
            
            polygonCreationService = Services.ServiceLocator.GetService<PolygonCreationService>();
            toolService = Services.ServiceLocator.GetService<ToolService>();

            lineButton.clicked += OnLineButtonClicked;
            polygonButton.clicked += OnPolygonButtonClicked;
            gridButton.clicked += OnGridButtonClicked;
        }

        private void OnPolygonButtonClicked()
        {
            polygonButton.SetActivePseudoState(true);
            lineButton.SetActivePseudoState(false);
            polygonCreationService.SetPolygonToCreate();
        }
        
        private void OnLineButtonClicked()
        {
            polygonButton.SetActivePseudoState(false);
            lineButton.SetActivePseudoState(true);
            polygonCreationService.SetLineInputToCreate();
        }

        private void OnGridButtonClicked()
        {
            toolService.GetTool(ToolType.PolygonGrid).Open();
            polygonCreationService.SetGridInputModeToCreate();
        }
    }
}