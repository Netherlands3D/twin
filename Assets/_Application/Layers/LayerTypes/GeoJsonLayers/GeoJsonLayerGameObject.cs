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
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [RequireComponent(typeof(ICredentialHandler))]
    public class GeoJsonLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
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

        List<PendingFeature> pendingPolygonFeatures = new();
        List<PendingFeature> pendingLineFeatures = new();
        List<PendingFeature> pendingPointFeatures = new();
        
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
            LayerURLPropertyData urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData.Url.IsStoredInProject())
            {
                string path = AssetUriFactory.GetLocalPath(urlPropertyData.Url);
                StartCoroutine(parser.ParseGeoJSONLocal(path));
            }
            else if (urlPropertyData.Url.IsRemoteAsset())
            {
                RequestCredentials();
            }
        }

        protected void RequestCredentials()
        {
            var credentialHandler = GetComponent<ICredentialHandler>();
            LayerURLPropertyData urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            credentialHandler.Uri = urlPropertyData.Url;
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            credentialHandler.ApplyCredentials();
        }

        protected virtual void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth.GetType() != typeof(Public))//if it is public, we don't want the property panel to show up
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }
            
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
            var originalCoordinateSystem = GeoJSONParser.GetCoordinateSystem(feature.CRS);
            VisualizeFeature(feature, originalCoordinateSystem);
            ProcessFeatureMapping(feature);
        }

        /// <summary>
        /// Load properties is only used when restoring a layer from a project file.
        /// After getting the property containing the url, the GeoJSON file is downloaded and parsed.
        /// </summary>
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ColorPropertyData>(properties);
            //Initialize the styling with the default color that is gotten from the LayerData.Color
            var stylingPropertyData = LayerData.GetProperty<ColorPropertyData>();
            stylingPropertyData.DefaultSymbolizer.SetFillColor(LayerData.Color);
            stylingPropertyData.DefaultSymbolizer.SetStrokeColor(LayerData.Color);
        }

        /// <summary>
        /// Removes features based on the bounds of their visualisations
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            polygonFeaturesLayer?.RemoveFeaturesOutOfView();            
            lineFeaturesLayer?.RemoveFeaturesOutOfView();
            pointFeaturesLayer?.RemoveFeaturesOutOfView();
        }

        private void ProcessFeatureMapping(Feature feature)
        {
            CreateFeatureMappingsForFeature(feature, polygonFeaturesLayer);
            CreateFeatureMappingsForFeature(feature, lineFeaturesLayer);
            CreateFeatureMappingsForFeature(feature, pointFeaturesLayer);
        }

        private void CreateFeatureMappingsForFeature(Feature feature, IGeoJsonVisualisationLayer layer)
        {
            var meshData = layer?.GetMeshData(feature);
            if (meshData != null)
            {
                CreateFeatureMappings(layer, feature, meshData);
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

        private void SetVisualization(LayerGameObject layerGameObject)
        {
            switch (layerGameObject)
            {
                case GeoJSONPolygonLayer layer:
                    polygonFeaturesLayer = layer;
                    SetVisualization(polygonFeaturesLayer, pendingPolygonFeatures);
                    break;
                case GeoJSONLineLayer layer:
                    lineFeaturesLayer = layer;
                    SetVisualization(lineFeaturesLayer, pendingLineFeatures);
                    break;
                case GeoJSONPointLayer layer:
                    pointFeaturesLayer = layer;
                    SetVisualization(pointFeaturesLayer, pendingPointFeatures);
                    break;
            }
        }

        private void SetVisualization(IGeoJsonVisualisationLayer layer, List<PendingFeature> pendingFeatures)
        {
            ColorPropertyData stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            ColorPropertyData childStylingPropertyData = layer.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            
            var fillColor = stylingPropertyData.DefaultSymbolizer.GetFillColor().HasValue ? stylingPropertyData.DefaultSymbolizer.GetFillColor().Value : LayerData.Color;
            var strokeColor = stylingPropertyData.DefaultSymbolizer.GetStrokeColor().HasValue ? stylingPropertyData.DefaultSymbolizer.GetStrokeColor().Value : LayerData.Color;
            
            //TODO we have to convert this to an enum in the future
            childStylingPropertyData.ActiveToolProperty = Symbolizer.StrokeColorProperty;
            childStylingPropertyData.SetDefaultSymbolizerColor(strokeColor);
            childStylingPropertyData.ActiveToolProperty = Symbolizer.FillColorProperty;
            childStylingPropertyData.SetDefaultSymbolizerColor(fillColor);
            
            layer.FeatureRemoved += OnFeatureRemoved;

            foreach (var pendingFeature in pendingFeatures)
            {
               VisualizeFeature(pendingFeature.Feature, pendingFeature.CoordinateSystem);
            }
            pendingFeatures.Clear();
        }

        private void VisualizeFeature(Feature feature, CoordinateSystem crs)
        {            
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:                    
                case GeoJSONObjectType.Polygon:
                    AddFeature(feature, crs, polygonFeaturesLayer, pendingPolygonFeatures, polygonLayerPrefab, SetVisualization);
                    return;
                case GeoJSONObjectType.MultiLineString:                    
                case GeoJSONObjectType.LineString:
                    AddFeature(feature, crs, lineFeaturesLayer, pendingLineFeatures, lineLayerPrefab, SetVisualization);
                    return;
                case GeoJSONObjectType.MultiPoint:                   
                case GeoJSONObjectType.Point:
                    AddFeature(feature, crs, pointFeaturesLayer, pendingPointFeatures, pointLayerPrefab, SetVisualization);
                    return;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
            }
        }

        private void AddFeature(Feature feature, CoordinateSystem originalCoordinateSystem, IGeoJsonVisualisationLayer layer, List<PendingFeature> pendingFeatures, LayerGameObject prefab, UnityAction<LayerGameObject> callBack)
        {
            if (layer == null)
            {
                if (pendingFeatures.Count == 0)
                    CreateLayer(prefab, callBack);

                var pendingFeature = new PendingFeature(feature, originalCoordinateSystem);
                pendingFeatures.Add(pendingFeature);
                return;
            }

            layer.AddAndVisualizeFeature(feature, originalCoordinateSystem);
        }

        private void CreateLayer(LayerGameObject prefab, UnityAction<LayerGameObject> callBack)
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child.PrefabIdentifier == prefab.PrefabIdentifier)
                {
                    //todo: check if the async visualisation spawning has issues with destroying the layerData before the visualisation is loaded
                    App.Layers.Remove(child); // in case a layer already exists, we destroy it since we need the visualisation and don't have access to it. 
                }
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(prefab.PrefabIdentifier).NamedAs(prefab.name).ChildOf(LayerData);
            App.Layers.Add(layerBuilder, callBack);
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
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                case GeoJSONObjectType.Polygon:
                   return polygonFeaturesLayer;
                case GeoJSONObjectType.MultiLineString:
                case GeoJSONObjectType.LineString:
                    return lineFeaturesLayer;
                case GeoJSONObjectType.MultiPoint:
                case GeoJSONObjectType.Point:
                    return pointFeaturesLayer;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization layer");
            }
        }
    }
}