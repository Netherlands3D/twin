using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.SubObjects;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area

        public Layer Layer => layer;

        private Layer layer;
        private TileHandler tileHandler;

        bool debugFeatures = false;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!layer || layer.isEnabled == isActive) return;

            layer.isEnabled = isActive;
        }

        protected override void OnVisualizationInitialize()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);         
            
            SetupFeatures();
        }

        public int TileHandlerLayerIndex => tileHandler.layers.IndexOf(layer);
        
        /// <summary>
        /// Cartesian Tiles have 'virtual' features, each type of terrain (grass, cycling path, etc) can be styled
        /// independently and thus is a feature. At the moment, the most concrete list of criteria for which features
        /// exist is the list of materials per terrain type.
        ///
        /// As such we create a LayerFeature per material with the material name and index as attribute, this allows
        /// for the styling system to apply styles per material - and thus: per feature type. 
        /// </summary>
        private void SetupFeatures()
        {
            if (layer is not BinaryMeshLayer binaryMeshLayer) return;

            //we have to apply styling when mappings are created, before we cannot load the values like from awake
            binaryMeshLayer.OnMappingCreated.AddListener(OnAddedMapping);         
            binaryMeshLayer.OnMappingRemoved.AddListener(OnRemovedMapping);

            if(debugFeatures)
            {
                ObjectSelectorService.MappingTree.OnMappingAdded.AddListener(OnDebugMapping);
            }

            StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
            if(stylingPropertyData == null) return;
                
            for (var materialIndex = 0; materialIndex < binaryMeshLayer.DefaultMaterialList.Count; materialIndex++)
            {
                // Make a copy of the default material, so we can change the color without affecting the original
                // TODO: This should be part of the BinaryMeshLayer itself?
                var material = binaryMeshLayer.DefaultMaterialList[materialIndex];
                material = new Material(material);
                binaryMeshLayer.DefaultMaterialList[materialIndex] = material;

                var layerFeature = CreateFeature(material);
                stylingPropertyData.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
        }
       
        //a simple debugging method to have x items hidden on startup in the hiddenobjects property panel
        private void OnDebugMapping(IMapping mapping)
        {
            if (debugFeatures)
            {
                HiddenObjectsPropertyData hiddenPropertyData = LayerData.GetProperty<HiddenObjectsPropertyData>();
                if (mapping is MeshMapping map)
                {
                    for(int i = 0; i < 10; i++)
                    {
                        map.CacheItems();
                        ObjectMappingItem item = map.Items[i].ObjectMappingItem;
                        Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, item);
                        var layerFeature = CreateFeature(item);
                        hiddenPropertyData.SetVisibilityForSubObject(layerFeature, false, coord);
                    }                    
                }
                debugFeatures = false;
            }
        }

        private void OnAddedMapping(ObjectMapping mapping)
        {   
            StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
            if (stylingPropertyData == null) return;
            
            foreach (ObjectMappingItem item in mapping.items)
            {
                var layerFeature = CreateFeature(item);
                stylingPropertyData.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
            ApplyStyling();
        }

        private void OnRemovedMapping(ObjectMapping mapping)
        {
            StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
            if (stylingPropertyData == null) return;
            
            foreach (ObjectMappingItem item in mapping.items)
            {
                stylingPropertyData.LayerFeatures.Remove(item);
            }
        }

        public static LayerFeature GetLayerFeatureFromBagId(string bagId)
        {
            CartesianTileLayerGameObject[] cartesianTileLayerGameObjects = FindObjectsByType<CartesianTileLayerGameObject>(FindObjectsSortMode.None);
            foreach(CartesianTileLayerGameObject cartesian in cartesianTileLayerGameObjects)
            {
                if (cartesian.Layer is not BinaryMeshLayer binaryMeshLayer) continue;

                foreach (ObjectMapping mapping in binaryMeshLayer.Mappings.Values)
                {
                    foreach (ObjectMappingItem item in mapping.items)
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

        public override void OnSelect(LayerData layer)
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(true);
        }

        public override void OnDeselect(LayerData layer)
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(false);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            if(layer is BinaryMeshLayer binaryMeshLayer)
            {
                binaryMeshLayer.OnMappingCreated.RemoveListener(OnAddedMapping);
                binaryMeshLayer.OnMappingRemoved.RemoveListener(OnRemovedMapping);
            }
        }

        protected void OnDestroy()
        {
            if (Application.isPlaying && tileHandler && layer)
            {
                tileHandler.RemoveLayer(layer);
            }
        }
        
        public override void ApplyStyling()
        {           
            // WMS and other projection layers also use this class as base - but they should not apply this styling
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
                if (stylingPropertyData == null) return;
                
                foreach (var (_, feature) in stylingPropertyData.LayerFeatures)
                {
                    //do cascading to get css result styling
                    Symbolizer symbolizer = GetStyling(feature);

                    if (feature.Geometry is ObjectMappingItem item)
                    {
                        bool? visiblity = symbolizer.GetVisibility();
                        if (visiblity.HasValue)
                        {
                            string id = feature.Attributes[HiddenObjectsPropertyData.VisibilityAttributeIdentifier];
                            Color storedColor = symbolizer.GetFillColor() ?? Color.white;
                            var visibilityColor = visiblity == true ? storedColor : Color.clear;
                            GeometryColorizer.InsertCustomColorSet(-2, new Dictionary<string, Color>() { { id, visibilityColor } });
                        }
                    }

                    if (feature.Geometry is Material material)
                    {
                        Color? color = symbolizer.GetFillColor();
                        if (color.HasValue)
                        {
                            if (int.TryParse(feature.Attributes[LayerFeatureColorPropertyData.MaterialIndexIdentifier], out var materialIndex))
                            {
                                binaryMeshLayer.DefaultMaterialList[materialIndex].color = color.Value;
                            }
                        }
                    }
                }
            }
            base.ApplyStyling();
        }
        

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // WMS and other projection layers also use this class as base - but they should not add their materials
            if (layer is not BinaryMeshLayer meshLayer) return feature;

            if(feature.Geometry is ObjectMappingItem item)
            {
                feature.Attributes.Add(HiddenObjectsPropertyData.VisibilityAttributeIdentifier, item.objectID); 
            }

            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(
                LayerFeatureColorPropertyData.MaterialIndexIdentifier,
                meshLayer.DefaultMaterialList.IndexOf(mat).ToString()
            );
            feature.Attributes.Add(LayerFeatureColorPropertyData.MaterialNameIdentifier, mat.name);

            return feature;
        }
    }
}