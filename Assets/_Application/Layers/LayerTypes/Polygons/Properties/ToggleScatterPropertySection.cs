using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [PropertySection(typeof(ToggleScatterPropertyData))]
    public class ToggleScatterPropertySection : MonoBehaviour, IVisualizationWithPropertyData
    {
        [SerializeField] private Toggle convertToggle;

        ToggleScatterPropertyData convertTogglePropertyData;

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            convertTogglePropertyData = properties.Get<ToggleScatterPropertyData>();
            SetSectionVisible(convertTogglePropertyData.AllowScatter);
            
            convertTogglePropertyData.AllowScatterChanged.AddListener(SetSectionVisible);
            convertToggle.onValueChanged.AddListener(ToggleScatter);
        }

        private void SetSectionVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        private void OnDestroy()
        {
            convertTogglePropertyData.AllowScatterChanged.RemoveListener(SetSectionVisible);
            convertToggle.onValueChanged.RemoveListener(ToggleScatter);
        }

        private void Start()
        {
            convertToggle.SetIsOnWithoutNotify(convertTogglePropertyData.IsScattered);
        }

        private void ToggleScatter(bool isOn)
        {
            convertTogglePropertyData.IsScattered = isOn;
        }
    }
}