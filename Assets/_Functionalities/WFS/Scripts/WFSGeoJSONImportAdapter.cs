using System.IO;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Specialized;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.Wfs
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSGeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private WFSGeoJsonLayerGameObject layerPrefab;

        private string wfsVersion = "";

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;
            var url = new Uri(sourceUrl);

            var bodyContents = File.ReadAllText(cachedDataPath);

            // if this is not a capabilities uri, it should be a GetFeature uri; otherwise we do not support this
            if (!OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wfs))
            {
                return OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wfs, RequestType.GetFeature);
            }

            Debug.Log("Checking source WFS url: " + sourceUrl);
            var wfsGetCapabilities = new WfsGetCapabilities(new Uri(sourceUrl), bodyContents);

            //If the body is a specific GetFeature request; directly continue to execute
            bool isGetFeatureRequest = OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wfs, RequestType.GetFeature);
            if (isGetFeatureRequest)
                return true;

            //If the body is a GetCapabilities request; check if the WFS supports BBOX filter and GeoJSON output
            bool IsGetCapabilitiesRequest = OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(new Uri(sourceUrl), bodyContents, ServiceType.Wfs);
            if (!IsGetCapabilitiesRequest)
            {
                Debug.Log("<color=orange>WFS: No GetFeature nor GetCapabilities request type found.</color>");
                return false;
            }

            if (!wfsGetCapabilities.WFSBboxFilterCapability())
            {
                Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                return false;
            }

            if (!wfsGetCapabilities.HasGetFeatureAsGeoJSON())
            {
                Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                return false;
            }

            wfsVersion = wfsGetCapabilities.GetVersion();

            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;
            var url = new Uri(sourceUrl);
            var bodyContents = File.ReadAllText(cachedDataPath);

            var isWfsGetCapabilities = OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wfs);
            FolderLayer wfsFolder = null;
            string geoJsonOutputFormatString = "application/json";
            
            if (isWfsGetCapabilities)
            {
                var wfsGetCapabilities = new WfsGetCapabilities(new Uri(sourceUrl), bodyContents);
                wfsFolder = new FolderLayer(!string.IsNullOrEmpty(wfsGetCapabilities.GetTitle()) ? wfsGetCapabilities.GetTitle() : sourceUrl);
                var geoJsonOutputFormatStringFromGetCapabilities = wfsGetCapabilities.GetGeoJsonOutputFormatString();
                if (!string.IsNullOrEmpty(geoJsonOutputFormatStringFromGetCapabilities))
                    geoJsonOutputFormatString = geoJsonOutputFormatStringFromGetCapabilities; 

                // add the bounds directly, since we already have the GetCapabilities information anyway
                BoundingBoxCache.AddBoundingBoxContainer(wfsGetCapabilities);
                var featureTypes = wfsGetCapabilities.GetFeatureTypes();

                //Create a folder layer 
                foreach (var featureType in featureTypes)
                {
                    string crs = featureType.DefaultCRS;
                    Debug.Log("Adding WFS layer for featureType: " + featureType);
                    AddWFSLayer(featureType.Name, sourceUrl, crs, wfsFolder, featureType.Title, geoJsonOutputFormatString);
                }
                return;
            }

            var isWfsGetFeature = OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wfs, RequestType.GetFeature);
            if (isWfsGetFeature)
            {
                NameValueCollection queryParameters = new();
                new Uri(sourceUrl).TryParseQueryString(queryParameters);
                var featureType = queryParameters.Get(WfsGetCapabilities.ParameterNameOfTypeNameBasedOnVersion(wfsVersion));

                if (string.IsNullOrEmpty(featureType) == false)
                {
                    string crs = queryParameters["srsname"];
                    // Can't deduct a human-readable title at the moment, we should add that we always query for the
                    // capabilities; this also helps with things like outputFormat and CRS
                    AddWFSLayer(featureType, sourceUrl, crs, wfsFolder, featureType, geoJsonOutputFormatString);
                }
                return;
            }
            
            Debug.LogError("Unrecognized WFS request type: " + url);
        }

        private void AddWFSLayer(string featureType, string sourceUrl, string crsType, FolderLayer folderLayer, string title, string geoJsonOutputFormatString)
        {
            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerGetFeatureUri(featureType, sourceUrl, crsType, geoJsonOutputFormatString);
            var getFeatureUrl = uriBuilder.Uri.ToString();

            Debug.Log($"Adding WFS layer '{featureType}' with url '{getFeatureUrl}'");

            //Spawn a new WFS GeoJSON layer
            WFSGeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = title;

            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = AssetUriFactory.CreateRemoteAssetUri(getFeatureUrl);

            //GeoJSON layer+visual colors are set to random colors until user can pick colors in UI
            var randomLayerColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(0.5f, 1f), 1);
            randomLayerColor.a = 0.5f;
            newLayer.LayerData.Color = randomLayerColor;

            var symbolizer = newLayer.LayerData.DefaultSymbolizer;
            symbolizer?.SetFillColor(randomLayerColor);
            symbolizer?.SetStrokeColor(randomLayerColor);
        }

        private UriBuilder CreateLayerGetFeatureUri(string featureType, string sourceUrl, string crs, string geoJsonOutputFormatString)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Make sure we have a wfsVersion set
            if (string.IsNullOrEmpty(wfsVersion))
            {
                Debug.LogWarning("WFS version could not be determined, defaulting to " + WfsGetCapabilities.DefaultFallbackVersion);
                wfsVersion = WfsGetCapabilities.DefaultFallbackVersion;
            }

            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetFeature");
            uriBuilder.SetQueryParameter("version", wfsVersion);
            uriBuilder.SetQueryParameter(WfsGetCapabilities.ParameterNameOfTypeNameBasedOnVersion(wfsVersion), featureType);
            if (parameters.Get("outputFormat")?.ToLower() is not ("json" or "geojson"))
            {
                uriBuilder.SetQueryParameter(
                    "outputFormat",
                    geoJsonOutputFormatString
                );
            }

            uriBuilder.SetQueryParameter("srsname", crs);
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by CartesianTileWFSLayer
            return uriBuilder;
        }
    }
}