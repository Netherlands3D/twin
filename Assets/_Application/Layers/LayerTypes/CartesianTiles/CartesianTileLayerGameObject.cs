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

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject//, ILayerWithPropertyPanels
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

        protected override void OnLayerInitialize()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);           
        }

        protected override void OnLayerReady()
        {
            base.OnLayerReady();
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

            CartesianTileLayerFeatureColorPropertyData layerFeaturePropertyData = LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();

            for (var materialIndex = 0; materialIndex < binaryMeshLayer.DefaultMaterialList.Count; materialIndex++)
            {
                // Make a copy of the default material, so we can change the color without affecting the original
                // TODO: This should be part of the BinaryMeshLayer itself?
                var material = binaryMeshLayer.DefaultMaterialList[materialIndex];
                material = new Material(material);
                binaryMeshLayer.DefaultMaterialList[materialIndex] = material;

                var layerFeature = CreateFeature(material);
                layerFeaturePropertyData.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
        }
       
        //a simple debugging method to have x items hidden on startup in the hiddenobjects property panel
        private void OnDebugMapping(IMapping mapping)
        {
            if (debugFeatures)
            {
                StylingPropertyData stylingPropertyData = LayerData.GetProperty<StylingPropertyData>();
                if (mapping is MeshMapping map)
                {
                    for(int i = 0; i < 10; i++)
                    {
                        map.CacheItems();
                        ObjectMappingItem item = map.Items[i].ObjectMappingItem;
                        Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, item);
                        var layerFeature = CreateFeature(item);
                        CartesianTileLayerStyler.SetVisibilityForSubObject(layerFeature, false, coord, stylingPropertyData);
                    }                    
                }
                debugFeatures = false;
            }
        }

        private void OnAddedMapping(ObjectMapping mapping)
        {         
            CartesianTileLayerFeatureColorPropertyData layerFeaturePropertyData = LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            foreach (ObjectMappingItem item in mapping.items)
            {
                var layerFeature = CreateFeature(item);
                layerFeaturePropertyData.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
            StylingPropertyData stylingPropertyData = LayerData.GetProperty<StylingPropertyData>();
            stylingPropertyData.OnStylingApplied.Invoke();
        }

        private void OnRemovedMapping(ObjectMapping mapping)
        {
            CartesianTileLayerFeatureColorPropertyData layerFeaturePropertyData = LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            foreach (ObjectMappingItem item in mapping.items)
            {
                layerFeaturePropertyData.LayerFeatures.Remove(item);
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

        public override void OnSelect()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(true);
        }

        public override void OnDeselect()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(layer is BinaryMeshLayer binaryMeshLayer)
            {
                binaryMeshLayer.OnMappingCreated.RemoveListener(OnAddedMapping);
                binaryMeshLayer.OnMappingRemoved.RemoveListener(OnRemovedMapping);

            }
            if (Application.isPlaying && tileHandler && layer)
            {
                tileHandler.RemoveLayer(layer);
            }

        }
        
        public override void ApplyStyling()
        {
            CartesianTileLayerFeatureColorPropertyData stylingPropertyData = LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            // WMS and other projection layers also use this class as base - but they should not apply this styling
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                foreach (var (_, feature) in stylingPropertyData.LayerFeatures)
                {
                    Symbolizer symbolizer = GetStyling(feature);
                    CartesianTileLayerStyler.Apply(symbolizer, feature);
                    if (feature.Geometry is not Material material) continue;

                    Color? color = symbolizer.GetFillColor();
                    if (color.HasValue)
                    {
                        if (int.TryParse(feature.Attributes[CartesianTileLayerStyler.MaterialIndexIdentifier], out var materialIndex))
                        {                            
                            binaryMeshLayer.DefaultMaterialList[materialIndex].color = color.Value;
                        }
                    }
                }
            }
            base.ApplyStyling();
        }

        public override void UpdateMaskBitMask(int bitmask)
        {
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                UpdateBitMaskForMaterials(bitmask, binaryMeshLayer.DefaultMaterialList);
            }
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // WMS and other projection layers also use this class as base - but they should not add their materials
            if (layer is not BinaryMeshLayer meshLayer) return feature;

            if(feature.Geometry is ObjectMappingItem item)
            {
                feature.Attributes.Add(CartesianTileLayerStyler.VisibilityAttributeIdentifier, item.objectID); 
            }

            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(
                CartesianTileLayerStyler.MaterialIndexIdentifier,
                meshLayer.DefaultMaterialList.IndexOf(mat).ToString()
            );
            feature.Attributes.Add(CartesianTileLayerStyler.MaterialNameIdentifier, mat.name);

            return feature;
        }
    }
}