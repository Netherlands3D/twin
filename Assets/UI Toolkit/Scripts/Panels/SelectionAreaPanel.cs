using Netherlands3D.Events;
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
        private TriggerEvent polygonEvent;
        private TriggerEvent lineEvent;
        
        public SelectionAreaPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            polygonButton = this.Q<Button>("PolygonButton");
            lineButton = this.Q<Button>("LineButton");
            gridButton = this.Q<Button>("GridButton");
        }

        
        public void SetEvents(TriggerEvent polygonEvent, TriggerEvent lineEvent, TriggerEvent gridEvent)
        {
            this.polygonEvent = polygonEvent;
            this.lineEvent = lineEvent;
            polygonButton.clicked += OnPolygonButtonClicked;
            lineButton.clicked += OnLineButtonClicked;
            gridButton.clicked += gridEvent.InvokeStarted;
        }

        private void OnPolygonButtonClicked()
        {
            if(lineButton.hasActivePseudoState)
                polygonEvent.InvokeStarted(); //cancel the line tool by invoking the polygon tool, a second InvokeStarted will activate the tool
            
            polygonButton.SetActivePseudoState(true);
            lineButton.SetActivePseudoState(false);
            polygonEvent.InvokeStarted();
        }
        
        private void OnLineButtonClicked()
        {
            if(polygonButton.hasActivePseudoState)
                lineEvent.InvokeStarted(); //cancel the polygon tool by invoking the line event, a second InvokeStarted will activate the tool
            
            polygonButton.SetActivePseudoState(false);
            lineButton.SetActivePseudoState(true);
            lineEvent.InvokeStarted();
        }
    }
}