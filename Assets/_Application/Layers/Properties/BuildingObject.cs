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
    public class BuildingObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualization;
        private Dictionary<string, Coordinate> buildingIds = new();
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<BuildingPropertyData>(properties);
        }

        private void OnEnable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectSubObjectWithBagId.AddListener(ProcessMeshMappingForLayer);
            selectorService.OnDeselect.AddListener(ClearMeshMappingsForLayer);
        }

        private void OnDisable()
        {
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectSubObjectWithBagId.RemoveListener(ProcessMeshMappingForLayer);
            selectorService.OnDeselect.RemoveListener(ClearMeshMappingsForLayer);
        }

        private void ProcessMeshMappingForLayer(MeshMapping mapping, string bagId)
        {
            if (visualization == null || mapping == null || visualization.LayerData != mapping.LayerData)
            {
                ClearMeshMappingsForLayer();
                return;
            }

            BuildingPropertyData propertyData = visualization.LayerData.GetProperty<BuildingPropertyData>();
            buildingIds.Clear();
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            foreach (KeyValuePair<string, IMapping> kv in selectorService.SelectedMappings)
            {
                if (kv.Value is MeshMapping map)
                {
                    Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, map.ObjectMapping.items[kv.Key]);
                    buildingIds.Add(kv.Key, coord);
                }
            }
            propertyData.BuildingIds = buildingIds;
        }

        private void ClearMeshMappingsForLayer()
        {
            if(visualization == null)
                return;
            
            BuildingPropertyData propertyData = visualization.LayerData.GetProperty<BuildingPropertyData>();
            buildingIds.Clear();
            propertyData.BuildingIds = null; //dont clear but set to null to trigger changed event
        }
    }
}