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
            polygonButton.clicked += polygonEvent.InvokeStarted;
            lineButton.clicked += lineEvent.InvokeStarted;
            gridButton.clicked += gridEvent.InvokeStarted;
        }
    }
}