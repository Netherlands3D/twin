using System;
using System.Collections.Generic;
using System.IO;
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
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
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

        private GeoJSONPolygonLayer polygonFeaturesLayer;
        private GeoJSONLineLayer lineFeaturesLayer;
        private GeoJSONPointLayer pointFeaturesLayer;

        [Header("Visualizer settings")]
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;

        [Space] protected LayerURLPropertyData urlPropertyData = new();

        public LayerPropertyData PropertyData => urlPropertyData;

        private void Awake()
        {
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
        }

        protected override void Start()
        {
            base.Start();
            StartLoadingData();
        }

        protected virtual void StartLoadingData()
        {
            if (urlPropertyData.Data.IsStoredInProject())
            {
                string path = Path.Combine(Application.persistentDataPath, urlPropertyData.Data.LocalPath.TrimStart('/', '\\'));
                StartCoroutine(parser.ParseGeoJSONLocal(path));
            }
            else if (urlPropertyData.Data.IsRemoteAsset())
            {
                RequestCredentials();
            }
        }

        private void RequestCredentials()
        {
            var credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.Uri = urlPropertyData.Data;
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            credentialHandler.ApplyCredentials();
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if(auth is FailedOrUnsupported)
            {
                    LayerData.HasValidCredentials = false;
                    return;
            }
            
            LayerData.HasValidCredentials = true;
            StartCoroutine(parser.ParseGeoJSONStreamRemote(uri, auth.GetConfig()));
        }

        private void OnDestroy()
        {
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
            var credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
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
            BagInspector.MappingTree.RootInsert(objectMapping);
        }

        private GeoJSONPolygonLayer CreateOrGetPolygonLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONPolygonLayer polygonLayer) continue;

                return polygonLayer;
            }

            GeoJSONPolygonLayer newPolygonLayerGameObject = Instantiate(polygonLayerPrefab);
            newPolygonLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newPolygonLayerGameObject.LayerData.RemoveStyle(newPolygonLayerGameObject.LayerData.DefaultStyle);
            newPolygonLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);
            newPolygonLayerGameObject.LayerData.SetParent(LayerData);
            newPolygonLayerGameObject.FeatureRemoved += OnFeatureRemoved;
            return newPolygonLayerGameObject;
        }

        private GeoJSONLineLayer CreateOrGetLineLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONLineLayer lineLayer) continue;

                return lineLayer;
            }

            GeoJSONLineLayer newLineLayerGameObject = Instantiate(lineLayerPrefab);
            newLineLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newLineLayerGameObject.LayerData.RemoveStyle(newLineLayerGameObject.LayerData.DefaultStyle);
            newLineLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);
            newLineLayerGameObject.LayerData.SetParent(LayerData);
            newLineLayerGameObject.FeatureRemoved += OnFeatureRemoved;
            return newLineLayerGameObject;
        }

        private GeoJSONPointLayer CreateOrGetPointLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONPointLayer pointLayer) continue;

                return pointLayer;
            }

            GeoJSONPointLayer newPointLayerGameObject = Instantiate(pointLayerPrefab);
            newPointLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newPointLayerGameObject.LayerData.RemoveStyle(newPointLayerGameObject.LayerData.DefaultStyle);
            newPointLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);
            newPointLayerGameObject.LayerData.SetParent(LayerData);
            newPointLayerGameObject.FeatureRemoved += OnFeatureRemoved;
            return newPointLayerGameObject;
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
                pointFeaturesLayer = CreateOrGetPointLayer();

            pointFeaturesLayer.AddAndVisualizeFeature<Point>(feature, originalCoordinateSystem);
        }

        private void AddMultiPointFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (pointFeaturesLayer == null)
                pointFeaturesLayer = CreateOrGetPointLayer();

            pointFeaturesLayer.AddAndVisualizeFeature<MultiPoint>(feature, originalCoordinateSystem);
        }

        private void AddLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
                lineFeaturesLayer = CreateOrGetLineLayer();

            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }

        private void AddMultiLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
                lineFeaturesLayer = CreateOrGetLineLayer();

            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }

        private void AddPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
                polygonFeaturesLayer = CreateOrGetPolygonLayer();

            polygonFeaturesLayer.AddAndVisualizeFeature<Polygon>(feature, originalCoordinateSystem);
        }

        private void AddMultiPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
                polygonFeaturesLayer = CreateOrGetPolygonLayer();

            polygonFeaturesLayer.AddAndVisualizeFeature<MultiPolygon>(feature, originalCoordinateSystem);
        }

        protected virtual void OnFeatureRemoved(Feature feature)
        {
            //we have to query first to find the corresponding featuremappings, cant do a remove right away
            //alternative could be to make an extra method to query by feature and do remove, or as proposed caching cell ids (but this can cause bugs, since spatial data is "truth")           
            IGeoJsonVisualisationLayer layer = GetVisualisationLayerForFeature(feature);
            BoundingBox queryBoundingBox = FeatureMapping.CreateBoundingBoxForFeature(feature, layer);
            List<IMapping> mappings = BagInspector.MappingTree.Query<FeatureMapping>(queryBoundingBox);
            foreach (FeatureMapping mapping in mappings)
            {
                if (mapping.Feature == feature)
                {
                    //destroy featuremapping object, there should be no references anywhere else to this object!
                    BagInspector.MappingTree.Remove(mapping);
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