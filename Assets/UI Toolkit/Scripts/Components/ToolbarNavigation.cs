using System;
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

        public event Action OrientToNorth;
        public event Action<bool> ToggleOrthographicView;

        
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
        }

        private void OnNorthClick(ClickEvent _)
        {
            OrientToNorth?.Invoke();
        }

        public void UpdateCompass(float yawInDegrees)
        {
            North.Q<Icon>().style.rotate = new StyleRotate(new Rotate(yawInDegrees));
            North.EnableInClassList("toolbar-navigation__compass--north", yawInDegrees is > 359.0f or < 1.0f);
        }

        private void OnToggleOrthographicView(ChangeEvent<bool> value)
        {
            ToggleOrthographicView?.Invoke(value.newValue);
        }

        private void UpdatePerspectiveIcon()
        {
            Perspective.Image = Perspective.value ? IconImage.ORTHOGONAL_VIEW : IconImage.PERSPECTIVE_VIEW;
        }
    }
}