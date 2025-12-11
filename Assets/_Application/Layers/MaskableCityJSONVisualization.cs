using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class MaskableCityJSONVisualization : MaskableVisualization
    {
        CityJSONLayerGameObject cityJsonLayerGameObject => layerGameObject as CityJSONLayerGameObject;
        private HashSet<CityObjectVisualizer> pendingVisualizers = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            cityJsonLayerGameObject.OnFeatureAdded.AddListener(OnFeatureAdded); //when a feature is added, but not visualized yet
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            cityJsonLayerGameObject.OnFeatureAdded.RemoveListener(OnFeatureAdded);
        }

        private void OnFeatureAdded(CityObjectVisualizer visualizer)
        {
            pendingVisualizers.Add(visualizer);
        }

        private void Update()
        {
            if(pendingVisualizers.Count == 0)
                return;
            
            foreach (var visualizer in pendingVisualizers.ToArray())
            {
                if (visualizer.IsVisualized)
                {
                    ApplyMaskingToFeature(visualizer);
                    pendingVisualizers.Remove(visualizer);
                }
            }
        }


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