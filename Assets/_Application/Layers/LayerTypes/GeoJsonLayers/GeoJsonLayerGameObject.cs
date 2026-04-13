using System;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using UnityEngine.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI;

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

        [Header("Visualizer settings")]
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;

        [Header("Annotation settings")]
        [SerializeField] private WorldAnnotationLayerGameObject annotationLayerPrefab;

        private GeoJSONPolygonLayer polygonFeaturesLayer;
        private GeoJSONLineLayer lineFeaturesLayer;
        private GeoJSONPointLayer pointFeaturesLayer;

        private readonly List<WorldAnnotationLayerGameObject> spawnedAnnotations = new();
        private ICredentialHandler credentialHandler;

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

        protected override void OnVisualizationInitialize()
        {
            credentialHandler = GetComponent<ICredentialHandler>();
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
            parser.OnParseError.AddListener(VisualisationError.Invoke);
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

            credentialHandler.Uri = storedUri;
            credentialHandler.ApplyCredentials();
        }

        protected virtual void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth.GetType() != typeof(Public))
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }

            if (auth is FailedOrUnsupported)
            {
                LayerData.HasValidCredentials = false;
                return;
            }

            LayerData.HasValidCredentials = true;
            StartLoadingData(uri, auth);
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

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            credentialHandler?.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
            parser.OnParseError.RemoveListener(VisualisationError.Invoke);
            credentialHandler?.OnAuthorizationHandled.RemoveListener(HandleCredentials);
        }

        public void AddFeatureVisualisation(Feature feature)
        {
            var originalCoordinateSystem = GeoJSONParser.GetCoordinateSystem(feature.CRS);
            VisualizeFeature(feature, originalCoordinateSystem);
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ColorPropertyData>(properties);
        }

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
            var stylingPropertyData = LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            var childStylingPropertyData = layer.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            ConvertOldStylingDataIntoProperty(layer.LayerData.LayerProperties, "default", childStylingPropertyData);

            var childFillSetExplicitly = childStylingPropertyData.DefaultSymbolizer.GetFillColor().HasValue;
            var childStrokeSetExplicitly = childStylingPropertyData.DefaultSymbolizer.GetStrokeColor().HasValue;

            var fillColor = stylingPropertyData.DefaultSymbolizer.GetFillColor().HasValue ? stylingPropertyData.DefaultSymbolizer.GetFillColor().Value : LayerData.Color;
            var strokeColor = stylingPropertyData.DefaultSymbolizer.GetStrokeColor().HasValue ? stylingPropertyData.DefaultSymbolizer.GetStrokeColor().Value : LayerData.Color;

            var colorType = childStylingPropertyData.ColorType;

            if (!childStrokeSetExplicitly)
            {
                childStylingPropertyData.ColorType = Symbolizer.StrokeColorProperty;
                childStylingPropertyData.SetDefaultSymbolizerColor(strokeColor);
            }

            if (!childFillSetExplicitly)
            {
                childStylingPropertyData.ColorType = Symbolizer.FillColorProperty;
                childStylingPropertyData.SetDefaultSymbolizerColor(fillColor);
            }

            childStylingPropertyData.ColorType = colorType;

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
                    if (IsAnnotationFeature(feature))
                    {
                        SpawnAnnotationFeature(feature, crs);
                        return;
                    }

                    AddFeature(feature, crs, pointFeaturesLayer, pendingPointFeatures, pointLayerPrefab, SetVisualization);
                    return;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
            }
        }

        private bool IsAnnotationFeature(Feature feature)
        {
            if (feature?.Properties == null) return false;

            return HasNonEmptyProperty(feature, "annotationText")
                   || HasNonEmptyProperty(feature, "thumbnailUrl")
                   || HasNonEmptyProperty(feature, "thumbnailPath")
                   || HasNonEmptyProperty(feature, "previewUrl")
                   || HasNonEmptyProperty(feature, "imageUrl")
                   || HasNonEmptyProperty(feature, "imagePath")
                   || HasNonEmptyProperty(feature, "image")
                   || HasNonEmptyProperty(feature, "imageCaption")
                   || HasNonEmptyProperty(feature, "caption");
        }

        private bool HasNonEmptyProperty(Feature feature, string key)
        {
            if (feature?.Properties == null) return false;
            if (!feature.Properties.TryGetValue(key, out var value)) return false;
            return value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }

        private string GetStringProperty(Feature feature, params string[] keys)
        {
            if (feature?.Properties == null) return "";

            foreach (var key in keys)
            {
                if (!feature.Properties.TryGetValue(key, out var value)) continue;
                if (value == null) continue;

                var str = value.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                    return str;
            }

            return "";
        }

        private string GetAnnotationName(Feature feature, string fallbackText)
        {
            var title = GetStringProperty(feature, "title", "name", "label");
            if (!string.IsNullOrWhiteSpace(title))
                return title;

            if (!string.IsNullOrWhiteSpace(fallbackText))
                return textToName(fallbackText);

            return "GeoJSON Annotation";
        }

        private string textToName(string text)
        {
            return text.Length <= 64 ? text : text[..64];
        }

        private void SpawnAnnotationFeature(Feature feature, CoordinateSystem coordinateSystem)
        {
            if (annotationLayerPrefab == null)
            {
                VisualisationError.Invoke("GeoJSON annotations kunnen niet worden ingeladen: annotationLayerPrefab ontbreekt.");
                return;
            }

            string text = GetStringProperty(feature, "annotationText", "text", "description", "title");
            string imageUrl = GetStringProperty(feature, "imageUrl", "imagePath", "image");
            string imagePreviewUrl = GetStringProperty(feature, "thumbnailUrl", "thumbnailPath", "previewUrl");
            if (string.IsNullOrWhiteSpace(imagePreviewUrl))
                imagePreviewUrl = imageUrl;

            string imageCaption = GetStringProperty(feature, "imageCaption", "caption");
            string annotationName = GetAnnotationName(feature, text);

            switch (feature.Geometry)
            {
                case Point point:
                    SpawnAnnotationAtCoordinate(
                        ConvertPointToCoordinate(point.Coordinates, coordinateSystem),
                        annotationName,
                        text,
                        imageUrl,
                        imagePreviewUrl,
                        imageCaption
                    );
                    break;

                case MultiPoint multiPoint:
                    foreach (var pointCoordinates in multiPoint.Coordinates)
                    {
                        SpawnAnnotationAtCoordinate(
                            ConvertPointToCoordinate(pointCoordinates.Coordinates, coordinateSystem),
                            annotationName,
                            text,
                            imageUrl,
                            imagePreviewUrl,
                            imageCaption
                        );
                    }
                    break;
            }
        }

        private Coordinate ConvertPointToCoordinate(IPosition point, CoordinateSystem originalCoordinateSystem)
        {
            var lat = point.Latitude;
            var lon = point.Longitude;
            var alt = point.Altitude;

            Coordinate coord = new Coordinate(originalCoordinateSystem);
            coord.easting = lon;
            coord.northing = lat;

            if (alt != null)
            {
                coord.height = (double)alt;
            }
            else
            {
                coord = coord.Convert(CoordinateSystem.RDNAP);
                coord.height = 0;
            }

            return coord;
        }

        private void SpawnAnnotationAtCoordinate(Coordinate coordinate, string annotationName, string text, string imageUrl, string imagePreviewUrl, string imageCaption)
        {
            ILayerBuilder layerBuilder = LayerBuilder.Create()
                .OfType(annotationLayerPrefab.PrefabIdentifier)
                .NamedAs(annotationName)
                .AddProperty(new AnnotationPropertyData(text, imageUrl, imagePreviewUrl, imageCaption));

            var layer = App.Layers.Add(layerBuilder, spawnedLayer =>
            {
                if (spawnedLayer is WorldAnnotationLayerGameObject annotation)
                {
                    annotation.InitializeFromImportedData(coordinate, text, imageUrl, imagePreviewUrl, imageCaption);
                    spawnedAnnotations.Add(annotation);
                }
            });

            layer.LayerData.SetParent(LayerData);
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
            ProcessFeatureMapping(feature);
        }

        private void CreateLayer(LayerGameObject prefab, UnityAction<LayerGameObject> callBack)
        {
            var childrenInLayerData = LayerData.ChildrenLayers.ToArray();
            var propertiesToAdd = Array.Empty<LayerPropertyData>();
            foreach (var child in childrenInLayerData)
            {
                if (child.PrefabIdentifier == prefab.PrefabIdentifier)
                {
                    App.Layers.Remove(child);
                    propertiesToAdd = child.LayerProperties.ToArray();
                    break;
                }
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create().OfType(prefab.PrefabIdentifier).NamedAs(prefab.name).AddProperties(propertiesToAdd);
            var layer = App.Layers.Add(layerBuilder, callBack);
            layer.LayerData.SetParent(LayerData);
        }

        protected virtual void OnFeatureRemoved(Feature feature)
        {
            IGeoJsonVisualisationLayer layer = GetVisualisationLayerForFeature(feature);
            BoundingBox queryBoundingBox = FeatureMapping.CreateBoundingBoxForFeature(feature, layer);
            List<IMapping> mappings = ObjectSelectorService.MappingTree.Query<FeatureMapping>(queryBoundingBox);
            foreach (FeatureMapping mapping in mappings)
            {
                if (mapping.Feature == feature)
                {
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
