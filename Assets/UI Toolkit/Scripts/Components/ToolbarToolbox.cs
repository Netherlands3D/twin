using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarToolbox : VisualElement
    {
        private Toggle Dome => this.Q<Toggle>("Dome");
        private Button Screenshot => this.Q<Button>("Screenshot");

        public event Action<bool> OnDomeToggled;
        public event Action OnScreenshotClicked;

        public ToolbarToolbox()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            Dome.RegisterValueChangedCallback(OnDomeValueChanged);
            Screenshot.RegisterCallback<ClickEvent>(OnScreenshotClick);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            Dome.UnregisterValueChangedCallback(OnDomeValueChanged);
            Screenshot.UnregisterCallback<ClickEvent>(OnScreenshotClick);
        }

        private void OnDomeValueChanged(ChangeEvent<bool> evt) => OnDomeToggled?.Invoke(evt.newValue);
        private void OnScreenshotClick(ClickEvent _) => OnScreenshotClicked?.Invoke();

        public void SetDomeValueWithoutNotify(bool isOn)
        {
            Dome.SetValueWithoutNotify(isOn);
        }
    }
}