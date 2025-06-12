using Netherlands3D.CartesianTiles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Quality;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area
        
        private Layer layer;
        private TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!layer || layer.isEnabled == isActive) return;

            layer.isEnabled = isActive;
        }
        
        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);

            SetupFeatures();
        }

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

            for (var materialIndex = 0; materialIndex < binaryMeshLayer.DefaultMaterialList.Count; materialIndex++)
            {
                // Make a copy of the default material, so we can change the color without affecting the original
                // TODO: This should be part of the BinaryMeshLayer itself?
                var material = binaryMeshLayer.DefaultMaterialList[materialIndex];
                material = new Material(material);
                binaryMeshLayer.DefaultMaterialList[materialIndex] = material;

                //todo this is a dirty hack to restore the waterreflection texture to the waterreflection camera
                if(material.name.Contains("Twin_Water"))
                {
                    WaterReflectionCamera waterCamera = FindAnyObjectByType<WaterReflectionCamera>();
                    waterCamera.SetMaterial(material);
                }

                var layerFeature = CreateFeature(material);
                LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
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
        
        protected virtual void OnDestroy()
        {
            if (Application.isPlaying && tileHandler && layer)
            {
                tileHandler.RemoveLayer(layer);
            }
        }

        private List<IPropertySectionInstantiator> propertySections;

        protected List<IPropertySectionInstantiator> PropertySections
        {
            get
            {
                if (propertySections == null)
                {
                    propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
                }

                return propertySections;
            }
            set => propertySections = value;
        }       

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return PropertySections;
        }

        public override void ApplyStyling()
        {
            // WMS and other projection layers also use this class as base - but they should not apply this styling
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                foreach (var (_, feature) in LayerFeatures)
                {
                    CartesianTileLayerStyler.Apply(binaryMeshLayer, GetStyling(feature), feature);
                }
            }

            base.ApplyStyling();
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // WMS and other projection layers also use this class as base - but they should not add their materials
            if (layer is not BinaryMeshLayer meshLayer) return feature;
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