using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.CityJSON;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    [RequireComponent(typeof(CityJSON))]
    public class CityJSONLayerGameObject : HierarchicalObjectLayerGameObject
    {
        public UnityEvent<CityObjectVisualizer> OnFeatureAdded;
        CoordinateSystem heightReferenceCoordinateSystem = CoordinateSystem.ETRS89_ECEF;

        private CityJSON cityJson;
        public CityJSON CityJson => cityJson;

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            cityJson = GetComponent<CityJSON>();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var propertydata = LayerData.GetProperty<CityJSONPropertyData>();
            propertydata.OnCRSChanged.AddListener(UpdateCRS);
            cityJson.onAllCityObjectsProcessed.AddListener(InitializeStylingFeatures);
            
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var propertydata = LayerData.GetProperty<CityJSONPropertyData>();
            propertydata.OnCRSChanged.RemoveListener(UpdateCRS);
            cityJson.onAllCityObjectsProcessed.RemoveListener(InitializeStylingFeatures);
            
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

        protected override void OnImportedObjectVisualized(GameObject importedObject)
        {
            //do not call the base because we do not want to create LayerFeatures for the meshrenderers, just for the Visualizers
            
            foreach (var visualizer in importedObject.GetComponentsInChildren<CityObjectVisualizer>())
            {
                ApplyStylingToFeature(LayerFeatures[visualizer]);
            }
        }
        
        public override void ApplyStylingToFeature(StylingFeature feature)
        {
            if (feature.Geometry is not CityObjectVisualizer visualizer) return;
            
            if(!visualizer.HasData)
                return;
            
            var symbolizer = GetStyling(feature);
            var fillColor = symbolizer.GetFillColor();
            if (fillColor.HasValue)
                visualizer.SetFillColor(fillColor.Value);
            
            var strokeColor = symbolizer.GetStrokeColor();
            if (strokeColor.HasValue)
                visualizer.SetLineColor(strokeColor.Value);
        }
        
        private void InitializeStylingFeatures()
        {
            foreach (var co in cityJson.CityObjects)
            {
                AddStylingFeatureForCityObject(co);
            }

        }

        public void AddStylingFeatureForCityObject(CityObject co)
        {
            var visualizers = co.GetComponents<CityObjectVisualizer>();
            foreach (var visualizer in visualizers)
            {
                AddStylingFeature(visualizer);
            }
        }
        
        private void AddStylingFeature(CityObjectVisualizer visualizer)
        {
            if(LayerFeatures.ContainsKey(visualizer))
                return;
            
            var layerFeature = CreateFeature(visualizer);
            LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            ApplyStylingToFeature(layerFeature);
            OnFeatureAdded.Invoke(visualizer);
        }
    }
}