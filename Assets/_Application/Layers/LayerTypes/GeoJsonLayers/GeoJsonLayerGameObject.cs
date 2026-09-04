using System;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [RequireComponent(typeof(ICredentialHandler))]
    public class GeoJsonLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
    {
        public override BoundingBox Bounds
        {
            get
            {
                var pointBounds = pointFeaturesLayer.GetBoundingBoxOfVisibleFeatures();
                var lineBounds = lineFeaturesLayer.GetBoundingBoxOfVisibleFeatures();
                var polygonBounds = polygonFeaturesLayer.GetBoundingBoxOfVisibleFeatures();

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

        [Header("Visualizer settings")]
        [SerializeField] private GeoJSONPolygonLayer polygonFeaturesLayer;
        [SerializeField] private GeoJSONLineLayer lineFeaturesLayer;
        [SerializeField] private GeoJSONPointLayer pointFeaturesLayer;

        private bool hasPolygons;
        private bool hasLines;
        private bool hasPoints;

        private ICredentialHandler credentialHandler;
        private bool startLoadingDataWhenLayerBecomesActive = false;

        protected override void OnVisualizationInitialize()
        {
            credentialHandler = GetComponent<ICredentialHandler>();
        }

        protected override void OnVisualizationReady()
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            UpdateURL(urlPropertyData.Url);
        }

        protected virtual void UpdateURL(Uri storedUri)
        {
            if (storedUri == credentialHandler.Uri && credentialHandler.Authorization != null)
            {
                HandleCredentials(storedUri, credentialHandler.Authorization);
                return;
            }

            credentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            credentialHandler.ApplyCredentials();
        }

        protected virtual void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth.GetType() != typeof(Public)) //if it is public, we don't want the property panel to show up
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }

            if (auth is FailedOrUnsupported)
            {
                LayerData.HasValidCredentials = false;
                return;
            }

            LayerData.HasValidCredentials = true;

            if (LayerData.ActiveInHierarchy)
            {
                StartLoadingData(uri, auth);
            }
            else
            {
                startLoadingDataWhenLayerBecomesActive = true;
            }
        }

        protected void StartLoadingData(Uri uri, StoredAuthorization auth)
        {
            if (uri.IsStoredInProject())
            {
                string path = AssetUriFactory.GetLocalPath(uri);
                StartCoroutine(parser.ParseGeoJSONLocal(path));
            }
            else if (uri.IsRemoteAsset())
            {
                StartCoroutine(parser.ParseGeoJSONStreamRemote(uri, auth));
            }
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            if (!LayerData.HasValidCredentials) //in case we activate the layer for the first time, and we have invalid credentials, reset the loading flag and wait for valid credentials
            {
                startLoadingDataWhenLayerBecomesActive = false;
                return;
            }

            if (isActive && startLoadingDataWhenLayerBecomesActive) //in case we activate the layer with valid credentials for the first time, and we are still waiting for a load, parse the data.
            {
                var auth = credentialHandler.Authorization;
                var uri = auth.SanitizeUrl(credentialHandler.Uri);
                StartLoadingData(uri, auth);
                startLoadingDataWhenLayerBecomesActive = false;
            }

            polygonFeaturesLayer.OnLayerActiveInHierarchyChanged(isActive);
            lineFeaturesLayer.OnLayerActiveInHierarchyChanged(isActive);
            pointFeaturesLayer.OnLayerActiveInHierarchyChanged(isActive);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
            parser.OnParseError.AddListener(VisualisationError.Invoke);
            
            credentialHandler?.OnAuthorizationHandled.AddListener(HandleCredentials);
            
            polygonFeaturesLayer.FeatureRemoved += OnFeatureRemoved;
            lineFeaturesLayer.FeatureRemoved += OnFeatureRemoved;
            polygonFeaturesLayer.FeatureRemoved += OnFeatureRemoved;
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
            parser.OnParseError.RemoveListener(VisualisationError.Invoke);
            
            credentialHandler?.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            
            polygonFeaturesLayer.FeatureRemoved -= OnFeatureRemoved;
            lineFeaturesLayer.FeatureRemoved -= OnFeatureRemoved;
            polygonFeaturesLayer.FeatureRemoved -= OnFeatureRemoved;
        }

        public void AddFeatureVisualisation(Feature feature)
        {
            var originalCoordinateSystem = GeoJSONParser.GetCoordinateSystem(feature.CRS);
            VisualizeFeature(feature, originalCoordinateSystem);
        }

        /// <summary>
        /// Load properties is only used when restoring a layer from a project file.
        /// After getting the property containing the url, the GeoJSON file is downloaded and parsed.
        /// </summary>
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ColorPropertyData>(properties);
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

        private void VisualizeFeature(Feature feature, CoordinateSystem crs)
        {
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                case GeoJSONObjectType.Polygon:
                    AddFeature(feature, crs, polygonFeaturesLayer);
                    if (!hasPolygons)
                    {
                        InitStylingRules(Symbolizer.FillColorProperty, LayerData.Color);
                        hasPolygons = true;
                    }
                    return;
                case GeoJSONObjectType.MultiLineString:
                case GeoJSONObjectType.LineString:
                    AddFeature(feature, crs, lineFeaturesLayer);
                    if (!hasLines)
                    {
                        InitStylingRules(Symbolizer.StrokeColorProperty, LayerData.Color);
                        hasLines = true;
                    }
                    return;
                case GeoJSONObjectType.MultiPoint:
                case GeoJSONObjectType.Point:
                    AddFeature(feature, crs, pointFeaturesLayer);
                    if (!hasPoints)
                    {
                        InitStylingRules(Symbolizer.PointColorProperty, LayerData.Color);
                        hasPoints = true;
                    }
                    return;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
            }
        }

        private void AddFeature(Feature feature, CoordinateSystem originalCoordinateSystem, IGeoJsonVisualisationLayer layer)
        {
            layer.AddAndVisualizeFeature(feature, originalCoordinateSystem, this);
            CreateFeatureMappingsForFeature(feature, layer);
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

        public override void ApplyStyling()
        {
            if(!hasPolygons &&  !hasLines &&  !hasPoints)
                return;
            
            polygonFeaturesLayer.ApplyStyling(this);
            lineFeaturesLayer.ApplyStyling(this);
            pointFeaturesLayer.ApplyStyling(this);
            
            var colorPropertyData = LayerData.GetProperty<ColorPropertyData>();
            var colorTypes = colorPropertyData.GetUsedColorTypes();
            if (colorTypes.Count == 1)
            {
                switch (colorTypes[0])
                {
                    case Symbolizer.FillColorProperty:
                        LayerData.Color = polygonFeaturesLayer.GetRenderColor();
                        break;
                    case Symbolizer.StrokeColorProperty:
                        LayerData.Color = lineFeaturesLayer.GetRenderColor();
                        break;
                    case Symbolizer.PointColorProperty:
                        LayerData.Color = pointFeaturesLayer.GetRenderColor();
                        break;
                }
            }
        }

        private void InitStylingRules(string propertyKey, Color color)
        {
            var colorPropertyData = LayerData.GetProperty<ColorPropertyData>();
            colorPropertyData.ColorType = propertyKey;
            colorPropertyData.SetDefaultSymbolizerColor(color);
        }
    }
}