using System.IO;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Specialized;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.Wfs
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSGeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private WFSGeoJsonLayerGameObject layerPrefab;

        private string wfsVersion = "";
        private const string defaultFallbackVersion = "2.0.0"; // Default to 2.0.0 (released in 2010, compliant with ISO standards)

        private WFSRequest getCapabilitiesRequest;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;

            Debug.Log(sourceUrl);
            
            var urlContainsWfsSignifier = OgcCWebServicesUtility.IsValidURL(sourceUrl, ServiceType.Wfs);

            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            var bodyContents = File.ReadAllText(cachedDataPath);
            var couldBeWfsCapabilities = bodyContents.Contains("<WFS_Capabilities") || bodyContents.Contains("<wfs:WFS_Capabilities");

            if (urlContainsWfsSignifier == false && couldBeWfsCapabilities == false) return false;

            Debug.Log("Checking source WFS url: " + sourceUrl);
            getCapabilitiesRequest = new WFSRequest(sourceUrl, bodyContents);

            //If the body is a specific GetFeature request; directly continue to execute
            bool isGetFeatureRequest = getCapabilitiesRequest.IsGetFeatureRequest();
            if (isGetFeatureRequest)
                return true;

            //If the body is a GetCapabilities request; check if the WFS supports BBOX filter and GeoJSON output
            bool IsGetCapabilitiesRequest = getCapabilitiesRequest.IsGetCapabilitiesRequest();
            if (!IsGetCapabilitiesRequest)
            {
                Debug.Log("<color=orange>WFS: No GetFeature nor GetCapabilities request type found.</color>");
                return false;
            }

            getCapabilitiesRequest.ParseBodyAsXML();
            if (!getCapabilitiesRequest.HasBboxFilterCapability())
            {
                Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                return false;
            }

            if (!getCapabilitiesRequest.HasGetFeatureAsGeoJSON())
            {
                Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                return false;
            }

            wfsVersion = getCapabilitiesRequest.GetWFSVersion();

            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var sourceUrl = localFile.SourceUrl;
            var wfsFolder = new FolderLayer(!string.IsNullOrEmpty(getCapabilitiesRequest.GetTitle()) ? getCapabilitiesRequest.GetTitle() : sourceUrl);

            switch (getCapabilitiesRequest.requestType)
            {
                case WFSRequest.RequestType.GetCapabilities:
                {
                    // add the bounds directly, since we already have the GetCapabilities information anyway
                    WFSBoundingBoxCache.AddWfsBoundingBoxContainer(sourceUrl, getCapabilitiesRequest);
                    var featureTypes = getCapabilitiesRequest.GetFeatureTypes();

                    //Create a folder layer 
                    foreach (var featureType in featureTypes)
                    {
                        string crs = featureType.DefaultCRS;
                        Debug.Log("Adding WFS layer for featureType: " + featureType);
                        AddWFSLayer(featureType.Name, sourceUrl, crs, wfsFolder, featureType.Title);
                    }

                    getCapabilitiesRequest = null;
                    return;
                }
                case WFSRequest.RequestType.GetFeature:
                {
                    NameValueCollection queryParameters = new();
                    new Uri(sourceUrl).TryParseQueryString(queryParameters);
                    var featureType = queryParameters.Get(WFSRequest.ParameterNameOfTypeNameBasedOnVersion(wfsVersion));

                    if (string.IsNullOrEmpty(featureType) == false)
                    {
                        string crs = queryParameters["srsname"];
                        // Can't deduct a human-readable title at the moment, we should add that we always query for the
                        // capabilities; this also helps with things like outputFormat and CRS
                        AddWFSLayer(featureType, sourceUrl, crs, wfsFolder, featureType);
                    }

                    getCapabilitiesRequest = null;
                    return;
                }
                default:
                    Debug.LogError("Unrecognized WFS request type: " + getCapabilitiesRequest.requestType);
                    break;
            }
        }

        private void AddWFSLayer(string featureType, string sourceUrl, string crsType, FolderLayer folderLayer, string title)
        {
            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerUri(featureType, sourceUrl, crsType);
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

        private UriBuilder CreateLayerUri(string featureType, string sourceUrl, string crs)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Make sure we have a wfsVersion set
            if (string.IsNullOrEmpty(wfsVersion))
            {
                Debug.LogWarning("WFS version could not be determined, defaulting to " + defaultFallbackVersion);
                wfsVersion = defaultFallbackVersion;
            }

            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetFeature");
            uriBuilder.SetQueryParameter("version", wfsVersion);
            uriBuilder.SetQueryParameter(WFSRequest.ParameterNameOfTypeNameBasedOnVersion(wfsVersion), featureType);
            if (parameters.Get("outputFormat")?.ToLower() is not ("json" or "geojson"))
            {
                var geoJsonOutputFormatString = getCapabilitiesRequest.GetGeoJsonOutputFormatString();
                uriBuilder.SetQueryParameter(
                    "outputFormat",
                    !string.IsNullOrEmpty(geoJsonOutputFormatString) ? geoJsonOutputFormatString : "application/json"
                );
            }

            uriBuilder.SetQueryParameter("srsname", crs);
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by CartesianTileWFSLayer
            return uriBuilder;
        }
    }
}