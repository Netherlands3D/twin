using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class HiddenObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        public bool debugFeatures = false;
        
        private LayerGameObject visualization;
        
        private Dictionary<string, Color> hiddenColors = new Dictionary<string, Color>();
        private int hiddenLayerId = -2;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<HiddenObjectsPropertyData>(properties);
            
            //BC
            visualization.ConvertOldStylingDataIntoProperty(properties, HiddenObjectsPropertyData.VisibilityIdentifier, visualization.LayerData.GetProperty<HiddenObjectsPropertyData>());
            
            SetupFeatures();
        }

        private void SetupFeatures()
        {
            HiddenObjectsPropertyData hiddenObjectsPropertyData = visualization.LayerData.GetProperty<HiddenObjectsPropertyData>();
            
            visualization.OnFeatureCreated += AddAttributesToLayerFeature;
            hiddenObjectsPropertyData.OnStylingChanged.AddListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.AddListener(OnDestroyLayer);

            if(debugFeatures)
            {
                ObjectSelectorService.MappingTree.OnMappingAdded.AddListener(OnDebugMapping);
            }   
            
            if(visualization is not CartesianTileLayerGameObject cartesianTile) return;
            if (cartesianTile.Layer is not BinaryMeshLayer binaryMeshLayer) return;

            //we have to apply styling when mappings are created, before we cannot load the values like from awake
            binaryMeshLayer.OnMappingCreated.AddListener(OnAddedMapping);         
            binaryMeshLayer.OnMappingRemoved.AddListener(OnRemovedMapping);
        }

        private void OnApplyStyling()
        {
            foreach (var (_, feature) in visualization.LayerFeatures)
            {
                //do cascading to get css result styling
                Symbolizer symbolizer = visualization.GetStyling(feature);

                if (feature.Geometry is ObjectMappingItem item)
                {
                    bool? visiblity = symbolizer.GetVisibility();
                    if (visiblity.HasValue)
                    {
                        string id = feature.Attributes[HiddenObjectsPropertyData.VisibilityAttributeIdentifier];
                        Color storedColor = symbolizer.GetFillColor() ?? Color.white;
                        var visibilityColor = visiblity == true ? storedColor : Color.clear;
                        hiddenColors[id] = visibilityColor;
                    }
                }
            }
            
            if(visualization is not CartesianTileLayerGameObject cartesianTile) return;
            if (cartesianTile.Layer is not BinaryMeshLayer binaryMeshLayer) return;

            foreach (KeyValuePair<Vector2Int, ObjectMapping> kv in binaryMeshLayer.Mappings)
            {
                Interaction.ApplyColors(hiddenColors, kv.Value);
            }
        }
        
         //a simple debugging method to have x items hidden on startup in the hiddenobjects property panel
        private void OnDebugMapping(IMapping mapping)
        {
            if (debugFeatures)
            {
                HiddenObjectsPropertyData hiddenPropertyData = visualization.LayerData.GetProperty<HiddenObjectsPropertyData>();
                if (mapping is MeshMapping map)
                {
                    for(int i = 0; i < 10; i++)
                    {
                        map.CacheItems();
                        ObjectMappingItem item = map.Items[i].ObjectMappingItem;
                        Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, item);
                        var layerFeature = visualization.CreateFeature(item);
                        hiddenPropertyData.SetVisibilityForSubObject(layerFeature, false, coord);
                    }                    
                }
                debugFeatures = false;
            }
        }
        
        private void OnAddedMapping(ObjectMapping mapping)
        {  
            foreach (ObjectMappingItem item in mapping.items.Values)
            {
                var layerFeature = visualization.CreateFeature(item);
                visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
            OnApplyStyling();
        }

        private void OnRemovedMapping(ObjectMapping mapping)
        {
            foreach (ObjectMappingItem item in mapping.items.Values)
            {
                visualization.LayerFeatures.Remove(item);
            }
        }

        //todo check if this method probably needs a refactor since the between step of a layerfeature is not needed to store hiddenobject data
        public static LayerFeature GetLayerFeatureFromBagId(string bagId)
        {
            CartesianTileLayerGameObject[] cartesianTileLayerGameObjects = FindObjectsByType<CartesianTileLayerGameObject>(FindObjectsSortMode.None);
            foreach(CartesianTileLayerGameObject cartesian in cartesianTileLayerGameObjects)
            {
                if (cartesian.Layer is not BinaryMeshLayer binaryMeshLayer) continue;

                foreach (ObjectMapping mapping in binaryMeshLayer.Mappings.Values)
                {
                    foreach (ObjectMappingItem item in mapping.items.Values)
                    {
                        if (item.objectID == bagId)
                        {
                            var layerFeature = cartesian.CreateFeature(item);
                            return layerFeature;
                        }
                    }
                }
            }            
            return null;
        }
        
        protected LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if(feature.Geometry is ObjectMappingItem item)
            {
                feature.Attributes.Add(HiddenObjectsPropertyData.VisibilityAttributeIdentifier, item.objectID); 
            }
            return feature;
        }

        private void OnDestroyLayer()
        {
            HiddenObjectsPropertyData hiddenObjectsPropertyData = visualization.LayerData.GetProperty<HiddenObjectsPropertyData>();
            
            visualization.OnFeatureCreated -= AddAttributesToLayerFeature;
            hiddenObjectsPropertyData.OnStylingChanged.RemoveListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.RemoveListener(OnDestroyLayer);
            
            if(visualization is not CartesianTileLayerGameObject cartesianTile) return;
            if (cartesianTile.Layer is not BinaryMeshLayer binaryMeshLayer) return;
            
            hiddenColors.Clear();
            foreach (KeyValuePair<Vector2Int, ObjectMapping> kv in binaryMeshLayer.Mappings)
            {
                Interaction.ApplyColors(hiddenColors, kv.Value);
            }
            
            binaryMeshLayer.OnMappingCreated.RemoveListener(OnAddedMapping);
            binaryMeshLayer.OnMappingRemoved.RemoveListener(OnRemovedMapping);
        }
    }
}