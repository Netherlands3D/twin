using System.Collections.Generic;
using System.Xml.Schema;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(ToggleScatterPropertyData))]
    public partial class ScatterPropertySection : VisualElement, IVisualizationWithPropertyData
    { 
        private ToggleScatterPropertyData convertToScatterPropertyData;

        private VisualElement toggleScatterSection; 
        private CheckboxToggle convertToggle;
        
        public ScatterPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            toggleScatterSection = this.Q<VisualElement>("ToggleScatterSection");
            convertToggle =  toggleScatterSection.Q<CheckboxToggle>();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            convertToScatterPropertyData = properties.Get<ToggleScatterPropertyData>();
            SetSectionVisible(convertToScatterPropertyData.AllowScatter);
            
            convertToScatterPropertyData.AllowScatterChanged.AddListener(SetSectionVisible);
            convertToggle.RegisterValueChangedCallback(ToggleScatter);
            convertToggle.SetValueWithoutNotify(convertToScatterPropertyData.IsScattered);
        }

        private void SetSectionVisible(bool isVisible)
        {
            if(isVisible)
                RemoveFromClassList("inactive");
            else
                AddToClassList("inactive");
        }
        
        private void ToggleScatter(ChangeEvent<bool> evt)
        {
            convertToScatterPropertyData.IsScattered = evt.newValue;
        }
    }
}