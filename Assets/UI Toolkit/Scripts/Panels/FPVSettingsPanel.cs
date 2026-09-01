using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Slider = Netherlands3D.UI.Components.Slider;
using Toggle = UnityEngine.UIElements.Toggle;
using Netherlands3D.UI_Toolkit.Scripts;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class FPVSettingsPanel : VisualElement
    {
        public const string FPV_ID = "first-person-viewer";
        private Functionality fpvFunctionality;
        private Toggle mouseLockToggle;
        private Slider mouseSensitivitySlider;
        
        public FPVSettingsPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            mouseLockToggle = this.Q<Toggle>("MouseLockToggle");
            mouseSensitivitySlider = this.Q<Slider>("MouseSensitivitySlider");
        }

        public FPVSettingsPanel(Functionality fpvFunctionality) : this()
        {
            this.fpvFunctionality = fpvFunctionality;
            //when we get many more different sections (per functionality) we should consider making dedicated 
            
            SetSectionActive(fpvFunctionality != null && fpvFunctionality.IsEnabled);
            RegisterFPVPanelListeners();
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void RegisterFPVPanelListeners()
        {
            fpvFunctionality?.OnEnable.AddListener(SetFPVSectionActive);
            fpvFunctionality?.OnDisable.AddListener(SetFPVSectionInactive);
            
            var isLocked = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.GetMouseLockModus();
            mouseLockToggle.SetValueWithoutNotify(isLocked);
            mouseLockToggle.RegisterValueChangedCallback(OnMouseLockModeChanged);

            var sensitivity = ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().FirstPersonCamera.GetSensitivity();
            mouseSensitivitySlider.SetValueWithoutNotify(sensitivity * 100);
            mouseSensitivitySlider.RegisterValueChangedCallback(OnSensitivityChanged);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            fpvFunctionality?.OnEnable.RemoveListener(SetFPVSectionActive);
            fpvFunctionality?.OnDisable.RemoveListener(SetFPVSectionInactive);

            mouseLockToggle.UnregisterValueChangedCallback(OnMouseLockModeChanged);
            mouseSensitivitySlider.UnregisterValueChangedCallback(OnSensitivityChanged);
        }

        private void SetFPVSectionActive()
        {
            SetSectionActive(true);    
        }

        private void SetFPVSectionInactive()
        {
            SetSectionActive(false);
        }
        
        private void SetSectionActive(bool show)
        {
            this.EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
        
        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().FirstPersonCamera.SetSensitivity(evt.newValue / 100);
        }

        private void OnMouseLockModeChanged(ChangeEvent<bool> evt)
        {
            ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.SetMouseLockModus(evt.newValue);
            App.Debug.DisplayMessage("Functie voorkeuren succesvol aangepast", IconImage.CHECKMARK);
        }
    }
}