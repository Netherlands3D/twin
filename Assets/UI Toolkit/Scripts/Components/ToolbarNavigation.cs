using System;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarNavigation : VisualElement
    {
        private Button North => this.Q<Button>("North");
        private Toggle Perspective => this.Q<Toggle>("Perspective");
        private Button FPV => this.Q<Button>("FPV");
        
        public ToolbarNavigation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            FPV.Q<Icon>().AddManipulator(new FirstPersonViewManipulator(0));
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            North.RegisterCallback<ClickEvent>(OnNorthClick);
            Perspective.RegisterValueChangedCallback(OnToggleOrthographicView);
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent _)
        {
            UpdatePerspectiveIcon();
            schedule.Execute(()=>
            {
                ServiceLocator.GetService<Compass>()?.ChangeDirection.AddListener(UpdateCompass);
            });
        }

        private void OnNorthClick(ClickEvent _)
        {
            ServiceLocator.GetService<Compass>().SwitchToNorth();
        }

        public void UpdateCompass(float yawInDegrees)
        {
            North.Q<Icon>().style.rotate = new StyleRotate(new Rotate(-yawInDegrees));
        }

        private void OnToggleOrthographicView(ChangeEvent<bool> evt)
        {
            ServiceLocator.GetService<CameraService>().ActiveCamera.GetComponent<FreeCamera>()?.EnableOrtographic(evt.newValue);
            UpdatePerspectiveIcon();
        }

        private void UpdatePerspectiveIcon()
        {
            Perspective.Image = Perspective.value ? IconImage.ORTHOGONAL_VIEW : IconImage.PERSPECTIVE_VIEW;
        }
    }
}