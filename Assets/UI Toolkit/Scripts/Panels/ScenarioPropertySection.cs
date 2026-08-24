using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(IScenarioConvertiblePropertyData), PropertySectionCategory.Settings)]
    public partial class ScenarioPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private readonly CheckboxToggle scenarioToggle;

        public ScenarioPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            scenarioToggle = this.Q<CheckboxToggle>("ScenarioToggle");
            scenarioToggle.RegisterValueChangedCallback(OnScenarioToggleChanged);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var isScenario = properties.Get<ScenarioPropertyData>() != null;
            scenarioToggle.SetValueWithoutNotify(isScenario);
        }

        private void OnScenarioToggleChanged(ChangeEvent<bool> evt)
        {
            var propertyPanelBehaviour = ServiceLocator.GetService<PropertyPanelBehaviour>();
            var layer = propertyPanelBehaviour.activeLayer;

            if (layer == null)
                return;

            ScenarioManager.SetScenarioState(layer, evt.newValue);
        }
    }
}