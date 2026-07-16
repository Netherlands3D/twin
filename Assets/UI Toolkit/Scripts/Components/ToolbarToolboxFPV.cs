using System;
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

            snapButton = this.Q<Button>("SnapButton");
            exitButton = this.Q<Button>("ExitButton");
            
            // RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            // RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        // private void OnAttachToPanel(AttachToPanelEvent _)
        // {
        //     Dome.RegisterValueChangedCallback(OnDomeValueChanged);
        //     Screenshot.RegisterCallback<ClickEvent>(OnScreenshotClick);
        // }
        //
        // private void OnDetachFromPanel(DetachFromPanelEvent _)
        // {
        //     Dome.UnregisterValueChangedCallback(OnDomeValueChanged);
        //     Screenshot.UnregisterCallback<ClickEvent>(OnScreenshotClick);
        // }
        //
        // private void OnDomeValueChanged(ChangeEvent<bool> evt) => OnDomeToggled?.Invoke(evt.newValue);
        // private void OnScreenshotClick(ClickEvent _) => OnScreenshotClicked?.Invoke();
        //
        // public void SetDomeValueWithoutNotify(bool isOn)
        // {
        //     Dome.SetValueWithoutNotify(isOn);
        // }
    }
}