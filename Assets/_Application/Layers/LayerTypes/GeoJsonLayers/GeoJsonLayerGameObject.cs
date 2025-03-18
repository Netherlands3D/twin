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
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Credentials;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Credentials.StoredAuthorization;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
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
        [SerializeField] private int maxFeatureVisualsPerFrame = 20;
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;
        
        public int MaxFeatureVisualsPerFrame { get => maxFeatureVisualsPerFrame; set => maxFeatureVisualsPerFrame = value; }

        [Space]
        protected LayerURLPropertyData urlPropertyData = new();
        public LayerPropertyData PropertyData => urlPropertyData;

        private ICredentialHandler credentialHandler;
        private Dictionary<string, string> customHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> CustomHeaders { get => customHeaders; private set => customHeaders = value; }
        private Dictionary<string, string> customQueryParams = new Dictionary<string, string>();
        public Dictionary<string, string> CustomQueryParameters { get => customQueryParams; private set => customQueryParams = value; }

        private void Awake()
        {
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);

            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);

            //we need to resolve the listener to the datatypechain because this is a prefab and it doesnt know about what is present in the scene
            DataTypeChain chain = FindObjectOfType<DataTypeChain>();
            if (chain != null)
                credentialHandler.CredentialsSucceeded.AddListener(chain.DetermineAdapter);
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
                StartCoroutine(parser.ParseGeoJSONStreamRemote(urlPropertyData.Data));
            }
        }

        private void HandleCredentials(StoredAuthorization auth)
        {
            ClearCredentials();

            if (auth is BearerToken bearerToken) //todo: moet BearerToken inheriten van InferableSingle key of niet?
            {
                AddCustomHeader("Authorization", "Bearer " + bearerToken.key);
                StartLoadingData();
            }
            else if (auth is InferableSingleKey inferableSingleKey)
            {
                AddCustomQueryParameter(inferableSingleKey.queryKeyName, inferableSingleKey.key);
                StartLoadingData();
            }
            else if (auth is UsernamePassword usernamePassword)
            {
                AddCustomHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(usernamePassword.username + ":" + usernamePassword.password)), true);
                StartLoadingData();
            }
        }

        public void ClearCredentials()
        {
            ClearCustomHeaders();
            ClearCustomQueryParameters();
        }

        /// <summary>
        /// Add custom headers for all internal WebRequests
        /// </summary>
        public void AddCustomHeader(string key, string value, bool replace = true)
        {
            if (replace && customHeaders.ContainsKey(key))
                customHeaders[key] = value;
            else
                customHeaders.Add(key, value);
        }

        public void ClearCustomHeaders()
        {
            customHeaders.Clear();
        }

        public void AddCustomQueryParameter(string key, string value, bool replace = true)
        {
            if (replace && customQueryParams.ContainsKey(key))
                customQueryParams[key] = value;
            else
                customQueryParams.Add(key, value);
        }

        public void ClearCustomQueryParameters()
        {
            customQueryParams.Clear();
        }

        private void OnDestroy()
        {
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
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
                UpdateURL(urlPropertyData.Data);
            }
        }

        private void UpdateURL(Uri urlWithoutQuery)
        {
            credentialHandler.BaseUri = urlWithoutQuery; //apply the URL from what is stored in the Project data
            //WMSProjectionLayer.WmsUrl = urlWithoutQuery.ToString();
            credentialHandler.ApplyCredentials();
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
                if(mapping.Feature == feature)
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