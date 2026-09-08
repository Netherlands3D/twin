using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    public class FeatureObject : MonoBehaviour
    {
        [SerializeField] private LayerGameObject visualization;
        private Dictionary<string, FeaturePropertyData.FeatureData> featureIds = new();
        private ObjectSelectorService selectorService;
        
        private void OnEnable()
        {
            selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectFeature.AddListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.AddListener(ClearFeatureMappingsForLayer);
        }

        private void OnDisable()
        {
            selectorService.SelectFeature.RemoveListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.RemoveListener(ClearFeatureMappingsForLayer);
        }

        private void ProcessFeatureMappingForLayer(FeatureMapping mapping)
        {
            if (mapping == null || visualization.LayerData != mapping.LayerData)
            {
                ClearFeatureMappingsForLayer();
                return;
            }

            FeaturePropertyData propertyData = visualization.LayerData.GetProperty<FeaturePropertyData>();
            featureIds.Clear();
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            foreach (KeyValuePair<string, IMapping> kv in selectorService.SelectedMappings)
            {
                if (kv.Value is FeatureMapping map)
                {
                    BoundingBox bbox = map.BoundingBox;
                    Dictionary<string, object> properties = map.Feature.Properties as Dictionary<string, object>;
                    FeaturePropertyData.FeatureData data = new FeaturePropertyData.FeatureData();
                    data.Properties = properties;
                    data.BoundingBox = bbox;
                    featureIds.Add(kv.Key, data);
                }
            }
            propertyData.FeatureIds = featureIds;
        }

        private void ClearFeatureMappingsForLayer()
        {
            if(visualization == null)
                return;
            
            FeaturePropertyData propertyData = visualization.LayerData.GetProperty<FeaturePropertyData>();
            featureIds.Clear();
            propertyData.FeatureIds = null; //dont clear but set to null to trigger changed event
        }
    }
}