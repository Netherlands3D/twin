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

        public event Action OrientToNorth;
        public event Action<bool> ToggleOrthographicView;

        
        public ToolbarNavigation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ => UpdateDynamicAttributes());
            North.RegisterCallback<ClickEvent>(_ => OrientToNorth?.Invoke());
            Perspective.RegisterValueChangedCallback(OnToggleOrthographicView);
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

        private void UpdateDynamicAttributes()
        {
            Perspective.Image = Perspective.value ? IconImage.OrthogonalView : IconImage.PerspectiveView;
        }
    }
}