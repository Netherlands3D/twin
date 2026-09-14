using System.Collections.Generic;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(GeoJsonLayerGameObject))]
    public class GeoJSONLayerFeatureColoring : MonoBehaviour, IVisualizationWithPropertyData
    {
        private List<IGeoJsonVisualisationLayer> layers = new List<IGeoJsonVisualisationLayer>();
        private GeoJsonLayerGameObject visualization;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<CartesianTileLayerFeatureColorPropertyData>(properties);
            
            //BC - todo discuss
            //visualization.ConvertOldStylingDataIntoProperty(properties, CartesianTileLayerFeatureColorPropertyData.ColoringIdentifier, visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>());
            
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
            CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            
            visualization.OnFeatureCreated += AddAttributesToLayerFeature;
            featureColorPropertyData.OnStylingChanged.AddListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.AddListener(OnDestroyLayer);
            visualization.OnFeatureAdd.AddListener(UpdateStyling);
            visualization.OnFeatureRemove.AddListener(UpdateStyling);
            
            layers.Clear();
            layers.Add(visualization.PolygonLayer);
            layers.Add(visualization.LineLayer);
            layers.Add(visualization.PointLayer);
           
            // foreach(var layer in layers)
            // {
            //     var layerFeature = visualization.CreateFeature(layer.RenderMaterial);
            //     visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            //     var color = featureColorPropertyData.GetColor(layerFeature);
            //     featureColorPropertyData.SetColor(layerFeature, color.GetValueOrDefault(Color.white));
            // }
        }

        private void UpdateStyling(Feature feature)
        {
            IGeoJsonVisualisationLayer layer = null;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].SupportsGeometryType(feature.Geometry.Type))
                {
                    layer = layers[i];
                    break;
                }
            }

            var layerFeature = visualization.GetLayerFeatureByGeometry(layer.RenderMaterial);
            if (layerFeature == null)
            {
                layerFeature = visualization.CreateFeature(layer.RenderMaterial);
                visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
            if (int.TryParse(layerFeature.Attributes[CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey], out var materialIndex))
            {
                CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
                if (layer.FeatureCount > 0)
                {
                    var color = featureColorPropertyData.GetColor(layerFeature);
                    featureColorPropertyData.SetColor(layerFeature, color.GetValueOrDefault(Color.white));
                }
                else
                {
                    featureColorPropertyData.RemoveColorForMaterialIndex(materialIndex);
                }
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
                        if (int.TryParse(feature.Attributes[CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey], out var materialIndex))
                        {
                            layers[materialIndex].RenderColor = color.Value;
                        }
                    }
                }
            }
        }

        private IGeoJsonVisualisationLayer GetRenderLayer(Material material)
        {
            for(int i = 0; i < layers.Count; i++)
                if(layers[i].RenderMaterial == material)
                    return layers[i];
            
            return null;
        }
        
        private int GetRenderLayerIndex(Material material)
        {
            for(int i = 0; i < layers.Count; i++)
                if(layers[i].RenderMaterial == material)
                    return i;
            
            return -1;
        }

        private string GetRenderLayerName(Material material)
        {
            for(int i = 0; i < layers.Count; i++)
                if(layers[i].RenderMaterial == material)
                {
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.Point))
                        return "punten";
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.LineString))
                        return "lijnen";
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.Polygon))
                        return "polygonen";
                }
            
            return null;
        }
        
        protected LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(CartesianTileLayerFeatureColorPropertyData.MaterialNameIdentifier, GetRenderLayerName(mat));
            feature.Attributes.Add(CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey, GetRenderLayerIndex(mat).ToString());
            
            return feature;
        }

        
        private void OnDestroyLayer()
        {
            CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            
            visualization.OnFeatureCreated -= AddAttributesToLayerFeature;
            featureColorPropertyData.OnStylingChanged.RemoveListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.RemoveListener(OnDestroyLayer);
            visualization.OnFeatureAdd.RemoveListener(UpdateStyling);
            visualization.OnFeatureRemove.RemoveListener(UpdateStyling);
        }
    }
}