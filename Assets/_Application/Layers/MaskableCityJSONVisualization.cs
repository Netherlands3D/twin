using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class MaskableCityJSONVisualization : MaskableVisualization
    {
        CityJSONLayerGameObject cityJsonLayerGameObject => layerGameObject as CityJSONLayerGameObject;

        private void ApplyMaskingToFeature(CityObjectVisualizer visualizer)
        {
            var bitMask = GetBitMask();
            UpdateBitMaskForMaterials(bitMask, visualizer.Materials);
        }

        protected override void UpdateMaskBitMask(int bitmask)
        {
            foreach (var geometry in cityJsonLayerGameObject.LayerFeatures.Keys)
            {
                var visualizer = geometry as CityObjectVisualizer;
                ApplyMaskingToFeature(visualizer);
            }
        }
    }
}