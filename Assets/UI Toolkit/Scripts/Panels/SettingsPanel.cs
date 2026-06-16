using System.Collections.Generic;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Configuration.UI;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Quality;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;
using QualitySettings = UnityEngine.QualitySettings;
using RadioButtonGroup = UnityEngine.UIElements.RadioButtonGroup;
using Toggle = Netherlands3D.UI.Components.Toggle;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [InspectorPanel]
    public partial class SettingsPanel : BaseInspectorContentPanel
    {
        public override string Title => "Instellingen";
        private RadioButtonGroup qualityRadioButtonGroup;
        private ListView functionalitiesListView;

        public SettingsPanel()
        {
        }

        public SettingsPanel(Configuration configuration) : this()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            qualityRadioButtonGroup = this.Q<RadioButtonGroup>("QualitySettings");
            qualityRadioButtonGroup.value = QualitySettings.GetQualityLevel();
            qualityRadioButtonGroup.RegisterValueChangedCallback(OnQualitySettingsChanged);

            functionalitiesListView = this.Q<ListView>("Functionalities");
            functionalitiesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            functionalitiesListView.makeItem = MakeFunctionalityItem;
            functionalitiesListView.bindItem = BindFunctionalityItem;
            functionalitiesListView.itemsSource = configuration.Functionalities;
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
    }
}