using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Quality;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;
using QualitySettings = UnityEngine.QualitySettings;
using RadioButtonGroup = UnityEngine.UIElements.RadioButtonGroup;
using ScrollView = Netherlands3D.UI.Components.ScrollView;
using Netherlands3D.UI_Toolkit.Scripts;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class SettingsPanel : BaseInspectorContentPanel
    {
        public override string Title => "Instellingen";
        private ContentContainer settingsSection;
        private RadioButtonGroup qualityRadioButtonGroup;
        
        public SettingsPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            settingsSection = this.Q<ContentContainer>("SettingsSection");
            
            qualityRadioButtonGroup = settingsSection.Q<RadioButtonGroup>("QualitySettings");
            qualityRadioButtonGroup.value = QualitySettings.GetQualityLevel();
            qualityRadioButtonGroup.RegisterValueChangedCallback(OnQualitySettingsChanged);
        }

        public SettingsPanel(Configuration configuration) : this()
        {
            var optionalFunctionalities = configuration.Functionalities.Where(f => f.Type == FunctionalityType.Optional).ToList();
            var experimentalFunctionalities = configuration.Functionalities.Where(f => f.Type == FunctionalityType.Experimental).ToList();
            ConfigureFunctionalityList(settingsSection.Q<ListView>("Functionalities"), optionalFunctionalities);

            if (experimentalFunctionalities.Count > 0)
            {
                var experimentalSection = settingsSection.Q<VisualElement>("ExperimentalSection");
                var experimentalList = experimentalSection?.Q<ListView>("ExperimentalFunctionalities");
                if (experimentalList != null)
                {
                    ConfigureFunctionalityList(experimentalList, experimentalFunctionalities);
                    experimentalSection.RemoveFromClassList(UtilityClassConstants.HIDDEN);
                }
            }
            
            var fpvFunctionality = optionalFunctionalities.Concat(experimentalFunctionalities).FirstOrDefault(f => f.Id == FPVSettingsPanel.FPV_ID);
            if (fpvFunctionality != null)
            {
                var fpvSection = new FPVSettingsPanel(fpvFunctionality);
                this.Q<ScrollView>().Add(fpvSection);
            }
        }
        
        private void ConfigureFunctionalityList(ListView listView, List<Functionality> functionalities)
        {
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.makeItem = MakeFunctionalityItem;
            listView.bindItem = (item, index) => BindFunctionalityItem(item, functionalities[index]);
            listView.itemsSource = functionalities;
        }

        private VisualElement MakeFunctionalityItem()
        {
            var toggle = new CheckboxToggle();
            // ListView reuses items, so register the callback once and update the functionality when binding.
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (toggle.userData is not Functionality functionality) return;

                functionality.IsEnabled = evt.newValue;
                App.Debug.DisplayMessage("Functie voorkeuren succesvol aangepast", IconImage.CHECKMARK);
            });
            return new ListViewItem(toggle);
        }

        private void BindFunctionalityItem(VisualElement item, Functionality functionality)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<CheckboxToggle>() is not CheckboxToggle toggle) return;

            toggle.userData = functionality;
            toggle.LabelText = functionality.Title;
            toggle.SetValueWithoutNotify(functionality.IsEnabled);
        }

        private void OnQualitySettingsChanged(ChangeEvent<int> evt)
        {
            var level = (GraphicsQualityLevel)evt.newValue;
            Twin.Quality.QualitySettings.SetGraphicsQuality(level, true);
            App.Debug.DisplayMessage("Functie voorkeuren succesvol aangepast", IconImage.CHECKMARK);
        }
    }
}
