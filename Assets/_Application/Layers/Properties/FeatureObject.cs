using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class FeatureObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualization;
        private Dictionary<string, FeaturePropertyData.FeatureData> featureIds = new();
        private SelectionService selectorService;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<FeaturePropertyData>(properties);
            visualization.LayerData.LayerSelected.AddListener(OnLayerSelected);
        }

        private void OnDestroy()
        {
            visualization.LayerData.LayerSelected.RemoveListener(OnLayerSelected);
        }

        private void OnEnable()
        {
            selectorService = ServiceLocator.GetService<SelectionService>();
            selectorService.SelectFeature.AddListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.AddListener(ClearFeatureMappingsForLayer);
        }

        private void OnDisable()
        {
            selectorService.SelectFeature.RemoveListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.RemoveListener(ClearFeatureMappingsForLayer);
        }

        private void OnLayerSelected(LayerData layer)
        {
            SelectionService selectorService = ServiceLocator.GetService<SelectionService>();
            TransformLayerPropertyData transformLayerPropertyData = layer.GetProperty<TransformLayerPropertyData>();
            //is this a sublayer of geojson? 
            if (transformLayerPropertyData != null)
            {
                selectorService.SelectGeoJsonFeatureAtPositionForLayer(transformLayerPropertyData.Position.ToUnity(), layer);
            }
        }
        
        private void ProcessFeatureMappingForLayer(FeatureMapping mapping)
        {
            FeaturePropertyData propertyData = visualization.LayerData.GetProperty<FeaturePropertyData>();
            if (mapping == null || propertyData == null)
            {
                ClearFeatureMappingsForLayer();
                return;
            }
            
            featureIds.Clear();
            SelectionService selectorService = ServiceLocator.GetService<SelectionService>();
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