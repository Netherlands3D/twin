using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ObjectPanel : VisualElement
    {
        public UnityEvent OnClose = new();
        
        // private Button button;
        // private Button Button => button ??= this.Q<Button>("Button");
        
        private GameObject target;

        public ObjectPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }
        
        public ObjectPanel(GameObject target) :  this()
        {
            this.target = target;
            //Button.clicked += OnClose.Invoke;
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            //Button.clicked -= OnClose.Invoke;
        }
    }
}