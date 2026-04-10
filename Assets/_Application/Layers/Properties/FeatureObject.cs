using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    //TODO maybe the name for this class is not right, component to be compatible with buildingpropertysection to show bag id information
    [RequireComponent(typeof(LayerGameObject))]
    public class FeatureObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualization;
        private Dictionary<string, (Coordinate, Dictionary<string, object>)> featureIds = new();
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<FeaturePropertyData>(properties);
        }

        private void OnEnable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectFeature.AddListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.AddListener(ClearFeatureMappingsForLayer);
        }

        private void OnDisable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectFeature.RemoveListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.RemoveListener(ClearFeatureMappingsForLayer);
        }

        private void ProcessFeatureMappingForLayer(FeatureMapping mapping)
        {
            if (visualization == null || mapping == null || visualization.LayerData != mapping.LayerData)
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
                    Dictionary<string, object> properties = map.Feature.Properties as Dictionary<string, object>;
                    Coordinate coord = map.GetCoordinateForFeatureMapping();
                    featureIds.Add(kv.Key, (coord, properties));
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