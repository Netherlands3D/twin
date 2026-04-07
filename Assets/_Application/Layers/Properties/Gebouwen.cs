using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class Gebouwen : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualization;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<BuildingPropertyData>(properties);
        }

        private void OnEnable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectSubObjectWithBagId.AddListener(ProcessMeshMappingForLayer);
        }

        private void OnDisable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectSubObjectWithBagId.RemoveListener(ProcessMeshMappingForLayer);
        }

        private void ProcessMeshMappingForLayer(MeshMapping mapping, string bagId)
        {
            if(visualization == null || visualization.LayerData != mapping.LayerData)
                return;

            BuildingPropertyData propertyData = visualization.LayerData.GetProperty<BuildingPropertyData>();
            propertyData.buildingIds.Clear();
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            foreach (KeyValuePair<string, IMapping> kv in selectorService.SelectedMappings)
            {
                if(kv.Value is MeshMapping)
                    propertyData.buildingIds.Add(kv.Key);
            }
            Debug.Log(propertyData.buildingIds.Count);
        }
    }
}