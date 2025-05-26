using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject, ILayerWithPropertyPanels
    {
        public const string MaterialNameIdentifier = "data-materialname";
        public const string MaterialIndexIdentifier = "data-materialindex";

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
                
                CreateFeature(material);
            }
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
            foreach (var feature in GetLayerFeatures())
            {
                ApplyStyling(feature.Value);
            }

            base.ApplyStyling();
        }

        private void ApplyStyling(LayerFeature feature)
        {
            var styling = GetStyling(feature);
            var meshLayer = GetTileLayerAsBinaryMeshLayer();

            Color? color = styling.GetFillColor();
            if (!color.HasValue) return;

            if (!int.TryParse(feature.Attributes[MaterialIndexIdentifier], out var materialIndex)) return;

            meshLayer.DefaultMaterialList[materialIndex].color = color.Value;
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if (feature.Geometry is not Material mat) return feature;

            var meshLayer = GetTileLayerAsBinaryMeshLayer();
            
            feature.Attributes.Add(MaterialIndexIdentifier, meshLayer.DefaultMaterialList.IndexOf(mat).ToString());
            feature.Attributes.Add(MaterialNameIdentifier, mat.name);

            return feature;
        }

        private BinaryMeshLayer GetTileLayerAsBinaryMeshLayer()
        {
            if (layer is not BinaryMeshLayer meshLayer)
            {
                throw new FormatException("Tile layer is not of type BinaryMeshLayer, but " + layer.GetType());
            }
            
            return meshLayer;
        }
    }
}