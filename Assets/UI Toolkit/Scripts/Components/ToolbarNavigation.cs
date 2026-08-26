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
        private VisualElement FPV => this.Q<VisualElement>("FPV");
        
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
            /*Disabled to keep the north arrow base style at all camera angles.*/
            // North.EnableInClassList("toolbar-navigation__compass--north", yawInDegrees is > 359.0f or < 1.0f);
        }

        private void OnToggleOrthographicView(ChangeEvent<bool> evt)
        {
            ServiceLocator.GetService<CameraService>().ActiveCamera.GetComponent<FreeCamera>()?.EnableOrtographic(evt.newValue);
        }

        private void UpdatePerspectiveIcon()
        {
            Perspective.Image = Perspective.value ? IconImage.ORTHOGONAL_VIEW : IconImage.PERSPECTIVE_VIEW;
        }
    }
}