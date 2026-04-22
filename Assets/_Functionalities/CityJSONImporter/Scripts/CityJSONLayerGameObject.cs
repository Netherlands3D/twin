using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.CityJSON;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class CityJSONLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public UnityEvent<CityObjectVisualizer> OnFeatureAdded;
        CoordinateSystem heightReferenceCoordinateSystem = CoordinateSystem.ETRS89_ECEF;

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var propertydata = LayerData.GetProperty<CityJSONPropertyData>();
            propertydata.OnCRSChanged.AddListener(UpdateCRS);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var propertydata = LayerData.GetProperty<CityJSONPropertyData>();
            propertydata.OnCRSChanged.RemoveListener(UpdateCRS);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);
            var heightPropertyData = properties.OfType<ILayerPropertyDataWithCRS>().FirstOrDefault();
            UpdateCRS(heightPropertyData.ContentCRS);
        }

        private void UpdateCRS(int newCRSValue)
        {
            var newHeight = WorldTransform.Coordinate.Convert((CoordinateSystem)newCRSValue).height;
            var oldHeight = WorldTransform.Coordinate.Convert(heightReferenceCoordinateSystem).height;

            var diff = oldHeight - newHeight;
            var newCoord = WorldTransform.Coordinate;
            newCoord.height += diff;
            WorldTransform.MoveToCoordinate(newCoord);
            heightReferenceCoordinateSystem = (CoordinateSystem)newCRSValue;
        }

        public override void ApplyStyling()
        {
            base.ApplyStyling();
            foreach (var feature in LayerFeatures.Values)
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
            LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            ApplyStylingToFeature(layerFeature);
            OnFeatureAdded.Invoke(visualizer);
        }
    }
}