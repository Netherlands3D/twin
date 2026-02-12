using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(CartesianTileLayerGameObject))]
    public class CartesianTileLayerFeatureColoring : MonoBehaviour, IVisualizationWithPropertyData
    {
        private CartesianTileLayerGameObject visualization;
        private BinaryMeshLayer binaryMeshLayer;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<CartesianTileLayerGameObject>();
            binaryMeshLayer = visualization.Layer as BinaryMeshLayer;
            visualization.InitProperty<LayerFeatureColorPropertyData>(properties);
            
            //BC
            visualization.ConvertOldStylingDataIntoProperty(properties, LayerFeatureColorPropertyData.ColoringIdentifier, visualization.LayerData.GetProperty<LayerFeatureColorPropertyData>());
            
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
            LayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<LayerFeatureColorPropertyData>();
            
            visualization.OnFeatureCreated += AddAttributesToLayerFeature;
            featureColorPropertyData.OnStylingChanged.AddListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.AddListener(OnDestroyLayer);
            
            for (var materialIndex = 0; materialIndex < binaryMeshLayer.DefaultMaterialList.Count; materialIndex++)
            {
                // Make a copy of the default material, so we can change the color without affecting the original
                var material = binaryMeshLayer.DefaultMaterialList[materialIndex];
                material = new Material(material);
                binaryMeshLayer.DefaultMaterialList[materialIndex] = material;

                var layerFeature = visualization.CreateFeature(material);
                visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
                var color = featureColorPropertyData.GetColor(layerFeature);
                featureColorPropertyData.SetColor(layerFeature, color.GetValueOrDefault(Color.white));
            }
        }
        
        private void OnApplyStyling()
        {
            foreach (var (_, feature) in visualization.LayerFeatures)
            {
                //do cascading to get css result styling
                Symbolizer symbolizer = visualization.GetStyling(feature);

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
        
        protected LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(
                LayerFeatureColorPropertyData.MaterialIndexIdentifier,
                binaryMeshLayer.DefaultMaterialList.IndexOf(mat).ToString()
            );
            feature.Attributes.Add(LayerFeatureColorPropertyData.MaterialNameIdentifier, mat.name);
            
            return feature;
        }

        
        private void OnDestroyLayer()
        {
            LayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<LayerFeatureColorPropertyData>();
            
            visualization.OnFeatureCreated -= AddAttributesToLayerFeature;
            featureColorPropertyData.OnStylingChanged.RemoveListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.RemoveListener(OnDestroyLayer);
        }
    }
}