using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.CityJSON;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
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
            
            var transformPropertyData = LayerData.GetProperty<TransformLayerPropertyData>();
            transformPropertyData.OnPositionChanged.AddListener(UpdateCityObjectTransforms);
            
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var propertydata = LayerData.GetProperty<CityJSONPropertyData>();
            propertydata.OnCRSChanged.RemoveListener(UpdateCRS);
            cityJson.onAllCityObjectsProcessed.RemoveListener(InitializeStylingFeatures);
            
            var transformPropertyData = LayerData.GetProperty<TransformLayerPropertyData>();
            transformPropertyData.OnPositionChanged.RemoveListener(UpdateCityObjectTransforms);
            
        }

        private void UpdateCityObjectTransforms(Coordinate coordinate)
        {
            foreach (var m in mappings)
            {
                m.UpdateBoundingBox();
            }
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
        
        public override void ApplyStylingToFeature(LayerFeature feature)
        {
            if (feature.Geometry is not CityObjectVisualizer visualizer) return;
            
            if(!visualizer.HasData)
                return;
            
            var symbolizer = GetStyling(feature);
            
            var isVisibleStyling = symbolizer.GetVisibility();
            var isVisible = isVisibleStyling.HasValue ?  isVisibleStyling.Value : true;

            visualizer.gameObject.SetActive(isVisible);
            if(!isVisible)
                return;
            
            var fillColor = symbolizer.GetFillColor();
            visualizer.SetFillColor(fillColor);
            
            var strokeColor = symbolizer.GetStrokeColor();
            visualizer.SetLineColor(strokeColor);
        }
        
        private void InitializeStylingFeatures()
        {
            foreach (var co in cityJson.CityObjects)
            {
                AddStylingFeatureForCityObject(co);
                CreateFeatureMappings(co);
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
        
        private void CreateFeatureMappings(CityObject feature)
        {
            CityJSONMapping objectMapping = new CityJSONMapping();
            objectMapping.SetFeature(feature);
            objectMapping.SetLocalPosition(feature.AbsoluteCenter - cityJson.AbsoluteCenter);
            objectMapping.SetCityJsonLayerParent(this);
            objectMapping.UpdateBoundingBox();
            SelectionService.MappingTree.RootInsert(objectMapping);
            mappings.Add(objectMapping);
        }
        
        protected override void OnMouseClickNothing()
        {
           //base NO!
        }
        
        private List<CityJSONMapping> mappings = new List<CityJSONMapping>();

        protected override void Update()
        {
            base.Update();
            foreach (var m in mappings)
            {
                m.Debug();
            }
        }
    }
}