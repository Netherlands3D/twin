using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [PropertySection(typeof(ToggleScatterPropertyData))]
    public class ToggleScatterPropertySection : PropertySection
    {
        [SerializeField] private Toggle convertToggle;

        ToggleScatterPropertyData convertTogglePropertyData;
        // public LayerGameObject LayerGameObject { get; set; }

        public override void Initialize(LayerPropertyData property)
        {
            convertTogglePropertyData = property as ToggleScatterPropertyData;
            SetSectionVisible(convertTogglePropertyData.AllowScatter);
            
            convertTogglePropertyData.AllowScatterChanged.AddListener(SetSectionVisible);
            convertToggle.onValueChanged.AddListener(ToggleScatter);
        }

        private void SetSectionVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
        

        // private void Convert(bool isScattered)
        // {
        //     string oldPrefabId = convertTogglePropertyData.PrefabId;
        //     if (isScattered)
        //     {
        //         App.Layers.VisualizeAs(objectLayer.LayerData, ObjectScatterLayerGameObject.ScatterBasePrefabID, convertedVisualization => //todo: not make lambda to reduce allocations
        //         {
        //             ((ObjectScatterLayerGameObject)convertedVisualization).Initialize(oldPrefabId);
        //         });
        //     }
        //     else
        //     {
        //         - case ObjectScatterLayerGameObject scatterLayer:
        //         -App.Layers.VisualizeAs(scatterLayer.LayerData, scatterLayer.Settings.OriginalPrefabId);
        //     }
        // }

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