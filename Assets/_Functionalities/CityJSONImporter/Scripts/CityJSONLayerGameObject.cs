using System.Collections.Generic;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CityJSONLayerGameObject : HierarchicalObjectLayerGameObject
    {
        List<LayerFeature> layerFeatures = new List<LayerFeature>();
        
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            foreach (var feature in layerFeatures)
            {
                ApplyStylingToFeature(feature);
            }
        }
        
        private void ApplyStylingToFeature(LayerFeature feature)
        {
            if (feature.Geometry is not CityObjectVisualizer visualizer) return;

            StylingPropertyData stylingPropertyData = LayerData.GetProperty<StylingPropertyData>();
            var symbolizer = stylingPropertyData.DefaultStyle.AnyFeature.Symbolizer;
            var fillColor = symbolizer.GetFillColor();
            if (fillColor.HasValue)
                visualizer.SetFillColor(fillColor.Value);
        
            var strokeColor = symbolizer.GetStrokeColor();
            if (strokeColor.HasValue)
                visualizer.SetLineColor(strokeColor.Value);
        
            int bitMask = GetBitMask();
            UpdateBitMaskForMaterials(bitMask, visualizer.Materials);
        }
        
        public void AddFeature(CityObjectVisualizer visualizer)
        {
            var layerFeature = CreateFeature(visualizer);
            layerFeatures.Add(layerFeature);
            ApplyStylingToFeature(layerFeature);
        }
    }
}