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
        private List<IGeoJsonGeometryTypeVisualizer> layers = new List<IGeoJsonGeometryTypeVisualizer>();
        private GeoJsonLayerGameObject visualization;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<CartesianTileLayerFeatureColorPropertyData>(properties);
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            SetupFeatures();
        }
       
        private void SetupFeatures()
        {
            // CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
            
            visualization.OnFeatureCreated += AddAttributesToLayerFeature;
            foreach (var stylingPropertyData in visualization.LayerData.GetProperties<StylingPropertyData>())
                stylingPropertyData.OnStylingChanged.AddListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.AddListener(OnDestroyLayer);
            visualization.OnFeatureAdd.AddListener(OnFeatureAdded);
            visualization.OnFeatureRemove.AddListener(OnFeatureRemoved);
            
            layers.Clear();
            layers.Add(visualization.PolygonLayer);
            layers.Add(visualization.LineLayer);
            layers.Add(visualization.PointLayer);
            
            visualization.PolygonLayer.RenderColor = visualization.LayerData.Color;
            visualization.LineLayer.RenderColor = visualization.LayerData.Color;
            visualization.PointLayer.RenderColor = visualization.LayerData.Color;
        }

        private void OnFeatureAdded(Feature geoJsonFeature)
        {
            CreateStylingFeatureForGeojsonFeature(geoJsonFeature);
            UpdateLayerMaterialColorInPropertyData(geoJsonFeature);
        }

        private void OnFeatureRemoved(Feature geoJsonFeature)
        {
            UpdateLayerMaterialColorInPropertyData(geoJsonFeature);
        }

        private void UpdateLayerMaterialColorInPropertyData(Feature geoJsonFeature)
        {
            var typeVisualizer = CreateOrGetStylingFeatureForGeometryTypeVisualizer(geoJsonFeature);
            var layerMaterialFeature = CreateOrGetStylingFeatureForGeometryTypeVisualizer(typeVisualizer);
            if (int.TryParse(layerMaterialFeature.Attributes[CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey], out var materialIndex))
            {
                CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
                if (typeVisualizer.FeatureCount > 0)
                {
                    string colorProperty = GetColorPropertyTypeForLayer(typeVisualizer);
                    var color = featureColorPropertyData.GetColor(layerMaterialFeature, colorProperty);
                    featureColorPropertyData.SetColor(layerMaterialFeature, color.GetValueOrDefault(visualization.LayerData.Color), colorProperty);
                }
                else
                {
                    featureColorPropertyData.RemoveColorForMaterialIndex(materialIndex);
                }
            }
        }
        
        private void UpdateStylingFeatures(Feature feature)
        {
            CreateStylingFeatureForGeojsonFeature(feature);
            var layer = CreateOrGetStylingFeatureForGeometryTypeVisualizer(feature);
            var layerMaterialFeature = CreateOrGetStylingFeatureForGeometryTypeVisualizer(layer);

            if (int.TryParse(layerMaterialFeature.Attributes[CartesianTileLayerFeatureColorPropertyData.MaterialIndexKey], out var materialIndex))
            {
                CartesianTileLayerFeatureColorPropertyData featureColorPropertyData = visualization.LayerData.GetProperty<CartesianTileLayerFeatureColorPropertyData>();
                if (layer.FeatureCount > 0)
                {
                    string colorProperty = GetColorPropertyTypeForLayer(layer);
                    var color = featureColorPropertyData.GetColor(layerMaterialFeature, colorProperty);
                    featureColorPropertyData.SetColor(layerMaterialFeature, color.GetValueOrDefault(visualization.LayerData.Color), colorProperty);
                }
                else
                {
                    featureColorPropertyData.RemoveColorForMaterialIndex(materialIndex);
                }
            }
        }

        private StylingFeature CreateOrGetStylingFeatureForGeometryTypeVisualizer(IGeoJsonGeometryTypeVisualizer layer)
        {
            var layerMaterialFeature = visualization.GetStylingFeatureByGeometry(layer.RenderMaterial);
            if (layerMaterialFeature == null)
            {
                layerMaterialFeature = visualization.CreateFeature(layer.RenderMaterial);
                visualization.LayerFeatures.Add(layerMaterialFeature.Geometry, layerMaterialFeature);
            }

            return layerMaterialFeature;
        }

        private IGeoJsonGeometryTypeVisualizer CreateOrGetStylingFeatureForGeometryTypeVisualizer(Feature feature)
        {
            IGeoJsonGeometryTypeVisualizer layer = null;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].SupportsGeometryType(feature.Geometry.Type))
                {
                    layer = layers[i];
                    break;
                }
            }

            return layer;
        }

        private void CreateStylingFeatureForGeojsonFeature(Feature feature)
        {
            var layerFeature = visualization.CreateFeature(feature);
            layerFeature.Attributes.Add(TimelineStatusStylingLayerPropertyData.TimelineAttributeIdentifier, null);
            visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            ApplyStylingToFeature(layerFeature);
        }
        
        private void OnApplyStyling()
        {
            foreach (var (_, feature) in visualization.LayerFeatures)
            {
                ApplyStylingToFeature(feature);
            }
        }

        private void ApplyStylingToFeature(StylingFeature feature)
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

            if (feature.Geometry is Feature geojsonFeature)
            {
                var useStroke = geojsonFeature.Geometry.Type == GeoJSONObjectType.LineString || geojsonFeature.Geometry.Type == GeoJSONObjectType.MultiLineString;
                var colorType = useStroke ? Symbolizer.StrokeColorProperty :  Symbolizer.FillColorProperty;
                Color? color = symbolizer.GetColor(colorType);
                visualization.ApplyStylingToFeature(geojsonFeature, color);
            }
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
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.Point) || layers[i].SupportsGeometryType(GeoJSONObjectType.MultiPoint))
                        return "punten";
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.LineString) || layers[i].SupportsGeometryType(GeoJSONObjectType.MultiLineString))
                        return "lijnen";
                    if (layers[i].SupportsGeometryType(GeoJSONObjectType.Polygon) ||  layers[i].SupportsGeometryType(GeoJSONObjectType.MultiPolygon))
                        return "polygonen";
                }
            
            return null;
        }

        public string GetColorPropertyTypeForLayer(IGeoJsonGeometryTypeVisualizer layer)
        {
            if (layer.SupportsGeometryType(GeoJSONObjectType.Point) || layer.SupportsGeometryType(GeoJSONObjectType.MultiPoint))
                return Symbolizer.FillColorProperty;
            if (layer.SupportsGeometryType(GeoJSONObjectType.LineString) || layer.SupportsGeometryType(GeoJSONObjectType.MultiLineString))
                return Symbolizer.StrokeColorProperty;
            if (layer.SupportsGeometryType(GeoJSONObjectType.Polygon) ||  layer.SupportsGeometryType(GeoJSONObjectType.MultiPolygon))
                return Symbolizer.FillColorProperty;
            
            return Symbolizer.FillColorProperty;
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
            visualization.OnFeatureCreated -= AddAttributesToLayerFeature;
            foreach (var stylingPropertyData in visualization.LayerData.GetProperties<StylingPropertyData>())
                stylingPropertyData.OnStylingChanged.RemoveListener(OnApplyStyling);
            visualization.LayerData.LayerDestroyed.RemoveListener(OnDestroyLayer);
            visualization.OnFeatureAdd.RemoveListener(OnFeatureAdded);
            visualization.OnFeatureRemove.RemoveListener(OnFeatureRemoved);
        }
    }
}