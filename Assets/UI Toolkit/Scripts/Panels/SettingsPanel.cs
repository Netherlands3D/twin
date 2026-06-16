using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Configuration.UI;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Quality;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;
using QualitySettings = UnityEngine.QualitySettings;
using RadioButtonGroup = UnityEngine.UIElements.RadioButtonGroup;
using Slider = Netherlands3D.UI.Components.Slider;
using Toggle = Netherlands3D.UI.Components.Toggle;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class SettingsPanel : BaseInspectorContentPanel
    {
        public override string Title => "Instellingen";
        private ContentContainer settingsSection;
        private RadioButtonGroup qualityRadioButtonGroup;
        private ListView functionalitiesListView;

        private const string FPV_ID = "first-person-viewer";
        private Functionality fpvFunctionality;
        private ContentContainer fpvSection;
        private Toggle mouseLockToggle;
        private Slider mouseSensitivitySlider;
        
        public SettingsPanel()
        {
        }

        public SettingsPanel(Configuration configuration) : this()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            settingsSection = this.Q<ContentContainer>("SettingsSection");
            
            qualityRadioButtonGroup = settingsSection.Q<RadioButtonGroup>("QualitySettings");
            qualityRadioButtonGroup.value = QualitySettings.GetQualityLevel();
            qualityRadioButtonGroup.RegisterValueChangedCallback(OnQualitySettingsChanged);

            functionalitiesListView = settingsSection.Q<ListView>("Functionalities");
            functionalitiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            functionalitiesListView.makeItem = MakeFunctionalityItem;
            functionalitiesListView.bindItem = BindFunctionalityItem;
            functionalitiesListView.itemsSource = configuration.Functionalities;
            
            fpvSection = this.Q<ContentContainer>("FPVSection");
            mouseLockToggle = fpvSection.Q<Toggle>("MouseLockToggle");
            mouseSensitivitySlider = fpvSection.Q<Slider>("MouseSensitivitySlider");
            
            fpvFunctionality = configuration.Functionalities.FirstOrDefault(f => f.Id == FPV_ID);
            SetFPVSectionActive(fpvFunctionality != null && fpvFunctionality.IsEnabled);
            RegisterFPVPanelListeners();
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private VisualElement MakeFunctionalityItem()
        {
            var toggle = new CheckboxToggle();
            var listViewItem = new ListViewItem(toggle);
            return listViewItem;
        }

        private void BindFunctionalityItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<CheckboxToggle>() is not CheckboxToggle toggle) return;

            Functionality functionality = functionalitiesListView.itemsSource[index] as Functionality;
            toggle.LabelText = functionality.Title;
            toggle.SetValueWithoutNotify(functionality.IsEnabled);

            toggle.RegisterValueChangedCallback(evt => functionality.IsEnabled = evt.newValue);
        }

        private void OnQualitySettingsChanged(ChangeEvent<int> evt)
        {
            var level = (GraphicsQualityLevel)evt.newValue;
            Twin.Quality.QualitySettings.SetGraphicsQuality(level, true);
        }
        
        private void RegisterFPVPanelListeners()
        {
            fpvFunctionality?.OnEnable.AddListener(SetFPVSectionActive);
            fpvFunctionality?.OnDisable.AddListener(SetFPVSectionInactive);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            fpvFunctionality?.OnEnable.AddListener(SetFPVSectionActive);
            fpvFunctionality?.OnDisable.AddListener(SetFPVSectionInactive);
        }


        private void SetFPVSectionActive()
        {
            SetFPVSectionActive(true);    
        }

        private void SetFPVSectionInactive()
        {
            SetFPVSectionActive(false);
        }
        
        private void SetFPVSectionActive(bool show)
        {
            fpvSection.EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}