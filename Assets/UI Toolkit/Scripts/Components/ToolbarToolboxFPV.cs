using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarToolboxFPV : VisualElement
    {
        private Button snapButton;
        private Button exitButton;
        
        public ToolbarToolboxFPV()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            snapButton = this.Q<Button>("Snap");
            exitButton = this.Q<Button>("Exit");

            exitButton.clicked += ExitFPVMode;
            snapButton.clicked += SnapToGround;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            exitButton.clicked -= ExitFPVMode;
            snapButton.clicked -= SnapToGround;
        }

        private void ExitFPVMode()
        {
            FirstPersonViewer.FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>();
            fpv.ExitViewer(true);
        }
        
        private void SnapToGround()
        {
            FirstPersonViewer.FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>();
            fpv.ResetToGround();
        }
    }
}