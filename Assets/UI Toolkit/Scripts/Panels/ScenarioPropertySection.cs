using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(FolderPropertyData), PropertySectionCategory.Settings)]
    public partial class ScenarioPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private readonly CheckboxToggle scenarioToggle;
        private FolderPropertyData folderPropertyData;

        public ScenarioPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            scenarioToggle = this.Q<CheckboxToggle>("ScenarioToggle");
            scenarioToggle.RegisterValueChangedCallback(OnScenarioToggleChanged);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            folderPropertyData = properties.Get<FolderPropertyData>();
            scenarioToggle.SetValueWithoutNotify(folderPropertyData.IsScenario);
        }

        private void OnScenarioToggleChanged(ChangeEvent<bool> evt)
        {
            folderPropertyData.IsScenario = evt.newValue;
        }
    }
}