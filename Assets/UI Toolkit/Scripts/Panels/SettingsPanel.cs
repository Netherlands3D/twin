using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Quality;
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
        private ListView functionalitiesListView;
        private ListView experimentalFunctionalitiesListView;
        
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
            functionalitiesListView = settingsSection.Q<ListView>("Functionalities");
            functionalitiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            functionalitiesListView.makeItem = MakeFunctionalityItem;
            functionalitiesListView.bindItem = (item, index) => BindFunctionalityItem(item, index, functionalitiesListView);
            functionalitiesListView.itemsSource = configuration.Functionalities.Where(f => !f.IsExperimental).ToList();
            
            experimentalFunctionalitiesListView = settingsSection.Q<ListView>("ExperimentalFunctionalities");
            experimentalFunctionalitiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            experimentalFunctionalitiesListView.makeItem = MakeFunctionalityItem;
            experimentalFunctionalitiesListView.bindItem = (item, index) => BindFunctionalityItem(item, index, experimentalFunctionalitiesListView);
            experimentalFunctionalitiesListView.itemsSource = configuration.Functionalities.Where(f => f.IsExperimental).ToList();
            
            var fpvFunctionality = configuration.Functionalities.FirstOrDefault(f => f.Id == FPVSettingsPanel.FPV_ID);
            if (fpvFunctionality != null)
            {
                var fpvSection = new FPVSettingsPanel(fpvFunctionality);
                this.Q<ScrollView>().Add(fpvSection);
            }
        }
        
        private VisualElement MakeFunctionalityItem()
        {
            var toggle = new CheckboxToggle();
            var listViewItem = new ListViewItem(toggle);
            return listViewItem;
        }

        private void BindFunctionalityItem(VisualElement item, int index, ListView source)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<CheckboxToggle>() is not CheckboxToggle toggle) return;

            Functionality functionality = source.itemsSource[index] as Functionality;
            toggle.LabelText = functionality.Title;
            toggle.SetValueWithoutNotify(functionality.IsEnabled);

            toggle.RegisterValueChangedCallback(evt =>
            { 
                functionality.IsEnabled = evt.newValue;
                App.Debug.DisplayMessage("Functie voorkeuren succesvol aangepast", IconImage.CHECKMARK);
            });
        }

        private void OnQualitySettingsChanged(ChangeEvent<int> evt)
        {
            var level = (GraphicsQualityLevel)evt.newValue;
            Twin.Quality.QualitySettings.SetGraphicsQuality(level, true);
            App.Debug.DisplayMessage("Functie voorkeuren succesvol aangepast", IconImage.CHECKMARK);
        }
    }
}