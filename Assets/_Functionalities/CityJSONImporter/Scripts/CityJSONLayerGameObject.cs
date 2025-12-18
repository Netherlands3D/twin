using System.Collections.Generic;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CityJSONLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public UnityEvent<CityObjectVisualizer> OnFeatureAdded;
        
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
            foreach (var feature in stylingPropertyData.LayerFeatures.Values)
            {
                ApplyStylingToFeature(feature);
            }
        }
        
        private void ApplyStylingToFeature(LayerFeature feature)
        {
            if (feature.Geometry is not CityObjectVisualizer visualizer) return;

            var stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            var symbolizer = stylingPropertyData.AnyFeature.Symbolizer;
            var fillColor = symbolizer.GetFillColor();
            if (fillColor.HasValue)
                visualizer.SetFillColor(fillColor.Value);
        
            var strokeColor = symbolizer.GetStrokeColor();
            if (strokeColor.HasValue)
                visualizer.SetLineColor(strokeColor.Value);
        }
        
        public void AddFeature(CityObjectVisualizer visualizer)
        {
            var layerFeature = CreateFeature(visualizer);
            StylingPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<StylingPropertyData>();
            stylingPropertyData.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            ApplyStylingToFeature(layerFeature);
            OnFeatureAdded.Invoke(visualizer);
        }
    }
}