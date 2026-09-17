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
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    [RequireComponent(typeof(ICredentialHandler))]
    public class GeoJsonLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds
        {
            get
            {
                BoundingBox box = null;
                foreach (var layer in visualisationLayers)
                {
                    if (box == null)
                        box = layer.GetBoundingBoxOfVisibleFeatures();
                    else
                        box.Encapsulate(layer.GetBoundingBoxOfVisibleFeatures());
                }
                return box;
            }
        }

        public UnityEvent<Feature> OnFeatureAdd = new();
        public UnityEvent<Feature> OnFeatureRemove = new();

        private GeoJSONParser parser = new GeoJSONParser(0.01f);
        
        public IGeoJsonVisualisationLayer[] VisualisationLayers => visualisationLayers;

        private IGeoJsonVisualisationLayer[] visualisationLayers;
        
        private ICredentialHandler credentialHandler;
        private bool startLoadingDataWhenLayerBecomesActive = false;
        
        protected override void OnVisualizationInitialize()
        {
            credentialHandler = GetComponent<ICredentialHandler>();
            visualisationLayers = GetComponentsInChildren<IGeoJsonVisualisationLayer>();
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

            foreach (var layer in visualisationLayers)
            {
                layer.OnLayerActiveInHierarchyChanged(isActive);
            }
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
            parser.OnParseError.AddListener(VisualisationError.Invoke);
            
            credentialHandler?.OnAuthorizationHandled.AddListener(HandleCredentials);

            foreach (var layer in visualisationLayers)
            {
                layer.FeatureRemoved += OnFeatureRemoved;
            }
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
            parser.OnParseError.RemoveListener(VisualisationError.Invoke);
            
            credentialHandler?.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            
            foreach (var layer in visualisationLayers)
            {
                layer.FeatureRemoved -= OnFeatureRemoved;
            }
        }

        public void AddFeatureVisualisation(Feature feature)
        {
            var originalCoordinateSystem = GeoJSONParser.GetCoordinateSystem(feature.CRS);
            VisualizeFeature(feature, originalCoordinateSystem);
            OnFeatureAdd.Invoke(feature);
        }

        /// <summary>
        /// Removes features based on the bounds of their visualisations
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            foreach (var layer in visualisationLayers)
            {
                layer.RemoveFeaturesOutOfView();
            }
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
            SelectionService.MappingTree.RootInsert(objectMapping);
        }

        private void VisualizeFeature(Feature feature, CoordinateSystem crs)
        {
            IGeoJsonVisualisationLayer layer = GetVisualisationLayerForFeature(feature);
            if (layer == null)
            {
                Debug.LogError("No existing geojsonvisualisationlayer for feature: " + feature.Id);
                return;
            }
            
            AddFeature(feature, crs, layer);
        }

        private void AddFeature(Feature feature, CoordinateSystem originalCoordinateSystem, IGeoJsonVisualisationLayer layer)
        {
            layer.AddAndVisualizeFeature(feature, originalCoordinateSystem, LayerData.ActiveInHierarchy);
            CreateFeatureMappingsForFeature(feature, layer);
        }
        
        protected void OnFeatureRemoved(Feature feature)
        {
            OnFeatureRemove.Invoke(feature);
            //we have to query first to find the corresponding featuremappings, cant do a remove right away
            //alternative could be to make an extra method to query by feature and do remove, or as proposed caching cell ids (but this can cause bugs, since spatial data is "truth")           
            IGeoJsonVisualisationLayer layer = GetVisualisationLayerForFeature(feature);
            BoundingBox queryBoundingBox = FeatureMapping.CreateBoundingBoxForFeature(feature, layer);
            List<IMapping> mappings = SelectionService.MappingTree.Query<FeatureMapping>(queryBoundingBox);
            foreach (FeatureMapping mapping in mappings)
            {
                if (mapping.Feature == feature)
                {
                    //destroy featuremapping object, there should be no references anywhere else to this object!
                    SelectionService.MappingTree.Remove(mapping);
                }
            }
        }

        public IGeoJsonVisualisationLayer GetVisualisationLayerForFeature(Feature feature)
        {
            foreach (var layer in  visualisationLayers)
            {
                if(layer.SupportsGeometryType(feature))
                    return layer;
            }
            return null;
        }
    }
}