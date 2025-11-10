using System;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public struct PendingFeature
    {
        public Feature Feature;
        public CoordinateSystem CoordinateSystem;

        public PendingFeature(Feature feature, CoordinateSystem coordinateSystem)
        {
            Feature = feature;
            CoordinateSystem = coordinateSystem;
        }
    }

    [RequireComponent(typeof(ICredentialHandler))]
    public class GeoJsonLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        public override BoundingBox Bounds
        {
            get
            {
                var pointBounds = pointFeaturesLayer?.Bounds;
                var lineBounds = lineFeaturesLayer?.Bounds;
                var polygonBounds = polygonFeaturesLayer?.Bounds;

                if (pointBounds != null)
                {
                    pointBounds.Encapsulate(lineBounds);
                    pointBounds.Encapsulate(polygonBounds);
                    return pointBounds;
                }

                if (lineBounds != null)
                {
                    lineBounds.Encapsulate(polygonBounds);
                    return lineBounds;
                }

                return polygonBounds;
            }
        }

        private GeoJSONParser parser = new GeoJSONParser(0.01f);
        public GeoJSONParser Parser => parser;
        
        [SerializeField] private UnityEvent<string> onParseError = new();
        
        [Header("Visualizer settings")]
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;

        private GeoJSONPolygonLayer polygonFeaturesLayer;
        private GeoJSONLineLayer lineFeaturesLayer;
        private GeoJSONPointLayer pointFeaturesLayer;

        List<PendingFeature> pendingPolygonFeatures = new();
        List<PendingFeature> pendingLineFeatures = new();
        List<PendingFeature> pendingPointFeatures = new();

        [Space] protected LayerURLPropertyData urlPropertyData = new();

        public LayerPropertyData PropertyData => urlPropertyData;

        protected override void OnLayerInitialize()
        {
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
            parser.OnParseError.AddListener(onParseError.Invoke);
        }

        protected override void OnLayerReady()
        {
            StartLoadingData();
        }

        protected virtual void StartLoadingData()
        {
            if (urlPropertyData.Data.IsStoredInProject())
            {
                string path = AssetUriFactory.GetLocalPath(urlPropertyData.Data);
                StartCoroutine(parser.ParseGeoJSONLocal(path));
            }
            else if (urlPropertyData.Data.IsRemoteAsset())
            {
                RequestCredentials();
            }
        }

        protected void RequestCredentials()
        {
            var credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.Uri = urlPropertyData.Data;
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            credentialHandler.ApplyCredentials();
        }

        protected virtual void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
            {
                LayerData.HasValidCredentials = false;
                return;
            }

            LayerData.HasValidCredentials = true;
            StartCoroutine(parser.ParseGeoJSONStreamRemote(uri, auth));
        }

        protected override void OnDestroy()
        {
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
            parser.OnParseError.RemoveListener(onParseError.Invoke);

            var credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            base.OnDestroy();
        }

        public void AddFeatureVisualisation(Feature feature)
        {
            VisualizeFeature(feature);
            ProcessFeatureMapping(feature);
        }

        /// <summary>
        /// Load properties is only used when restoring a layer from a project file.
        /// After getting the property containing the url, the GeoJSON file is downloaded and parsed.
        /// </summary>
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        /// <summary>
        /// Removes features based on the bounds of their visualisations
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            if (polygonFeaturesLayer != null)
                polygonFeaturesLayer.RemoveFeaturesOutOfView();

            if (lineFeaturesLayer != null)
                lineFeaturesLayer.RemoveFeaturesOutOfView();

            if (pointFeaturesLayer != null)
                pointFeaturesLayer.RemoveFeaturesOutOfView();
        }

        private void ProcessFeatureMapping(Feature feature)
        {
            var polygonData = polygonFeaturesLayer?.GetMeshData(feature);
            if (polygonData != null)
            {
                CreateFeatureMappings(polygonFeaturesLayer, feature, polygonData);
            }

            var lineData = lineFeaturesLayer?.GetMeshData(feature);
            if (lineData != null)
            {
                CreateFeatureMappings(lineFeaturesLayer, feature, lineData);
            }

            var pointData = pointFeaturesLayer?.GetMeshData(feature);
            if (pointData != null)
            {
                CreateFeatureMappings(pointFeaturesLayer, feature, pointData);
            }
        }

        private void CreateFeatureMappings(IGeoJsonVisualisationLayer layer, Feature feature, List<Mesh> meshes)
        {
            FeatureMapping objectMapping = new FeatureMapping();
            objectMapping.SetFeature(feature);
            objectMapping.SetMeshes(meshes);
            objectMapping.SetVisualisationLayer(layer);
            objectMapping.SetGeoJsonLayerParent(this);
            objectMapping.UpdateBoundingBox();
            ObjectSelectorService.MappingTree.RootInsert(objectMapping);
        }

        private void CreatePolygonLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child.PrefabIdentifier == polygonLayerPrefab.PrefabIdentifier)
                {
                    //todo: check if the async visualisation spawning has issues with destroying the layerData before the visualisation is loaded
                    child.DestroyLayer(); // in case a layer already exists, we destroy it since we need the visualisation and don't have access to it. 
                }
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(polygonLayerPrefab.PrefabIdentifier).NamedAs(polygonLayerPrefab.name);
            App.Layers.Add(layerBuilder, SetPolygonVisualisation);
        }

        private void SetPolygonVisualisation(LayerGameObject layerGameObject)
        {
            polygonFeaturesLayer = layerGameObject as GeoJSONPolygonLayer;

            polygonFeaturesLayer.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            polygonFeaturesLayer.LayerData.RemoveStyle(polygonFeaturesLayer.LayerData.DefaultStyle);
            polygonFeaturesLayer.LayerData.AddStyle(LayerData.DefaultStyle);
            polygonFeaturesLayer.LayerData.SetParent(LayerData);
            polygonFeaturesLayer.FeatureRemoved += OnFeatureRemoved;

            foreach (var pendingFeature in pendingPolygonFeatures)
            {
                switch (pendingFeature.Feature.Geometry)
                {
                    case Polygon:
                        AddPolygonFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                    case MultiPolygon:
                        AddMultiPolygonFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                }
            }

            pendingPolygonFeatures.Clear();
        }

        private void CreateLineLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child.PrefabIdentifier == lineLayerPrefab.PrefabIdentifier)
                {
                    //todo: check if the async visualisation spawning has issues with destroying the layerData before the visualisation is loaded
                    child.DestroyLayer(); // in case a layer already exists, we destroy it since we need the visualisation and don't have access to it. 
                }
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(lineLayerPrefab.PrefabIdentifier).NamedAs(lineLayerPrefab.name);
            App.Layers.Add(layerBuilder, SetLineVisualisation);
        }

        private void SetLineVisualisation(LayerGameObject layerGameObject)
        {
            lineFeaturesLayer = layerGameObject as GeoJSONLineLayer;

            lineFeaturesLayer.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            lineFeaturesLayer.LayerData.RemoveStyle(lineFeaturesLayer.LayerData.DefaultStyle);
            lineFeaturesLayer.LayerData.AddStyle(LayerData.DefaultStyle);
            lineFeaturesLayer.LayerData.SetParent(LayerData);
            lineFeaturesLayer.FeatureRemoved += OnFeatureRemoved;

            foreach (var pendingFeature in pendingLineFeatures)
            {
                switch (pendingFeature.Feature.Geometry)
                {
                    case LineString:
                        AddLineStringFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                    case MultiLineString:
                        AddMultiLineStringFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                }
            }

            pendingLineFeatures.Clear();
        }

        private void CreatePointLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child.PrefabIdentifier == pointLayerPrefab.PrefabIdentifier)
                {
                    //todo: check if the async visualisation spawning has issues with destroying the layerData before the visualisation is loaded
                    child.DestroyLayer(); // in case a layer already exists, we destroy it since we need the visualisation and don't have access to it. 
                }
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(pointLayerPrefab.PrefabIdentifier).NamedAs(pointLayerPrefab.name);
            App.Layers.Add(layerBuilder, SetPointVisualization);
        }

        private void SetPointVisualization(LayerGameObject layerGameObject)
        {
            pointFeaturesLayer = layerGameObject as GeoJSONPointLayer;

            pointFeaturesLayer.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            pointFeaturesLayer.LayerData.RemoveStyle(pointFeaturesLayer.LayerData.DefaultStyle);
            pointFeaturesLayer.LayerData.AddStyle(LayerData.DefaultStyle);
            pointFeaturesLayer.LayerData.SetParent(LayerData);
            pointFeaturesLayer.FeatureRemoved += OnFeatureRemoved;

            foreach (var pendingFeature in pendingPointFeatures)
            {
                switch (pendingFeature.Feature.Geometry)
                {
                    case Point:
                        AddPointFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                    case MultiPoint:
                        AddMultiPointFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
                        break;
                }
            }

            pendingPointFeatures.Clear();
        }

        private void VisualizeFeature(Feature feature)
        {
            var originalCoordinateSystem = GeoJSONParser.GetCoordinateSystem(feature.CRS);
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                    AddMultiPolygonFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.Polygon:
                    AddPolygonFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.MultiLineString:
                    AddMultiLineStringFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.LineString:
                    AddLineStringFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.MultiPoint:
                    AddMultiPointFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.Point:
                    AddPointFeature(feature, originalCoordinateSystem);
                    return;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
            }
        }

        private void AddPointFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (pointFeaturesLayer == null)
            {
                if(pendingPointFeatures.Count == 0)
                    CreatePointLayer(); 
                
                pendingPointFeatures.Add(new PendingFeature(feature, originalCoordinateSystem));
                return;
            }

            pointFeaturesLayer.AddAndVisualizeFeature<Point>(feature, originalCoordinateSystem);
        }

        private void AddMultiPointFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (pointFeaturesLayer == null)
            {
                if(pendingPointFeatures.Count == 0)
                    CreatePointLayer();
                
                pendingPointFeatures.Add(new PendingFeature(feature, originalCoordinateSystem));
                return;
            }

            pointFeaturesLayer.AddAndVisualizeFeature<MultiPoint>(feature, originalCoordinateSystem);
        }

        private void AddLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
            {
                if(pendingLineFeatures.Count == 0)
                    CreateLineLayer();
                
                pendingLineFeatures.Add(new PendingFeature(feature, originalCoordinateSystem));
                return;
            }
            
            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }

        private void AddMultiLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
            {
                if(pendingLineFeatures.Count == 0)
                    CreateLineLayer();
    
                pendingLineFeatures.Add(new PendingFeature(feature, originalCoordinateSystem));
                return;
            }

            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }


        private void AddPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
            {
                if(pendingPolygonFeatures.Count == 0)
                    CreatePolygonLayer();
                
                pendingPolygonFeatures.Add(new PendingFeature(feature, originalCoordinateSystem));
                return;
            }

            polygonFeaturesLayer.AddAndVisualizeFeature<Polygon>(feature, originalCoordinateSystem);
        }

        private void AddMultiPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
            {
                if(pendingPolygonFeatures.Count == 0)
                    CreatePolygonLayer();
    
                var pendingFeature = new PendingFeature(feature, originalCoordinateSystem);
                pendingPolygonFeatures.Add(pendingFeature);
                return;
            }

            polygonFeaturesLayer.AddAndVisualizeFeature<MultiPolygon>(feature, originalCoordinateSystem);
        }

        protected virtual void OnFeatureRemoved(Feature feature)
        {
            //we have to query first to find the corresponding featuremappings, cant do a remove right away
            //alternative could be to make an extra method to query by feature and do remove, or as proposed caching cell ids (but this can cause bugs, since spatial data is "truth")           
            IGeoJsonVisualisationLayer layer = GetVisualisationLayerForFeature(feature);
            BoundingBox queryBoundingBox = FeatureMapping.CreateBoundingBoxForFeature(feature, layer);
            List<IMapping> mappings = ObjectSelectorService.MappingTree.Query<FeatureMapping>(queryBoundingBox);
            foreach (FeatureMapping mapping in mappings)
            {
                if (mapping.Feature == feature)
                {
                    //destroy featuremapping object, there should be no references anywhere else to this object!
                    ObjectSelectorService.MappingTree.Remove(mapping);
                }
            }
        }

        public IGeoJsonVisualisationLayer GetVisualisationLayerForFeature(Feature feature)
        {
            if (feature.Geometry is MultiLineString || feature.Geometry is LineString)
            {
                return lineFeaturesLayer;
            }
            else if (feature.Geometry is MultiPolygon || feature.Geometry is Polygon)
            {
                return polygonFeaturesLayer;
            }
            else if (feature.Geometry is Point || feature.Geometry is MultiPoint)
            {
                return pointFeaturesLayer;
            }

            return null;
        }
    }
}