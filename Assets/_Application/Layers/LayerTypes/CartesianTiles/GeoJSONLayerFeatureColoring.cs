using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(GeoJsonLayerGameObject))]
    public class GeoJSONLayerFeatureColoring : MonoBehaviour, IVisualizationWithPropertyData
    {
        private IGeoJsonGeometryTypeVisualizer[] layers => visualization.VisualisationLayers;
        private GeoJsonLayerGameObject visualization;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<CartesianTileLayerFeatureColorPropertyData>(properties);
            
            SetupFeatures();
        }
       
        private void SetupFeatures()
        {
            CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            
            featureColorPropertyData.OnStylingChanged.AddListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.AddListener(OnDestroyLayer);
            visualization.OnFeatureAdd.AddListener(UpdateStyling);
            visualization.OnFeatureRemove.AddListener(UpdateStyling);

            foreach (var layer in layers)
            {
                var layerFeature = visualization.GetStylingFeatureByGeometry(layer.RenderMaterial);
                if (layerFeature == null)
                {
                    layerFeature = visualization.CreateFeature(layer.RenderMaterial);
                    AddAttributesToLayerFeature(layerFeature);
                    visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
                }
                layer.RenderMaterial.color = visualization.LayerData.Color;
            }
        }

        private void UpdateStyling(Feature feature)
        {
            IGeoJsonGeometryTypeVisualizer layer = null;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].SupportsGeometryType(feature))
                {
                    layer = layers[i];
                    break;
                }
            }

            var layerFeature = visualization.GetStylingFeatureByGeometry(layer.RenderMaterial);

            if (int.TryParse(layerFeature.Attributes[CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey], out var materialIndex))
            {
                CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
                if (layer.FeatureCount > 0)
                {
                    var color = featureColorPropertyData.GetColor(layerFeature, layer.StylingColorProperty);
                    featureColorPropertyData.SetColor(layerFeature, color.GetValueOrDefault(visualization.LayerData.Color), layer.StylingColorProperty);
                }
                else
                {
                    featureColorPropertyData.RemoveColorForMaterialIndex(materialIndex);
                }
            }
        }
        
        public void OnApplyStyling()
        {
            foreach (var (_, stylingFeature) in visualization.LayerFeatures)
            {
                visualization.ApplyStylingToFeature(stylingFeature);
            }
        }
        
        private int GetRenderLayerIndex(Material material)
        {
            for(int i = 0; i < layers.Length; i++)
                if(layers[i].RenderMaterial == material)
                    return i;
            
            return -1;
        }

        private string GetRenderLayerName(Material material)
        {
            for(int i = 0; i < layers.Length; i++)
                if(layers[i].RenderMaterial == material)
                {
                    return layers[i].DisplayName;
                }
            
            return null;
        }
        
        protected StylingFeature AddAttributesToLayerFeature(StylingFeature feature)
        {
            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(CartesianTileLayerFeatureColorPropertyData.MaterialNameIdentifier, GetRenderLayerName(mat));
            feature.Attributes.Add(CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey, GetRenderLayerIndex(mat).ToString());
            
            return feature;
        }

        
        private void OnDestroyLayer()
        {
            CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            
            featureColorPropertyData.OnStylingChanged.RemoveListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.RemoveListener(OnDestroyLayer);
            visualization.OnFeatureAdd.RemoveListener(UpdateStyling);
            visualization.OnFeatureRemove.RemoveListener(UpdateStyling);
        }
    }
}