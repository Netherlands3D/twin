using System;
using System.Collections.Generic;
using System.IO;
using KindMen.Uxios;
using KindMen.Uxios.Http;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.Wfs.LayerPresets;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Services;
using Netherlands3D.Web;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wfs
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSGeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter<Layer>
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

        public Layer Execute(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;
            var url = new Uri(sourceUrl);
            var bodyContents = File.ReadAllText(cachedDataPath);

            var isWfsGetCapabilities = OgcWebServicesUtility.IsSupportedGetCapabilitiesUrl(url, bodyContents, ServiceType.Wfs);
            LayerData wfsFolder = null;
            string geoJsonOutputFormatString = "application/json";
            
            if (isWfsGetCapabilities)
            {
                var wfsGetCapabilities = new WfsGetCapabilities(new Uri(sourceUrl), bodyContents);
                var folderName = string.IsNullOrEmpty(wfsGetCapabilities.GetTitle()) ? sourceUrl : wfsGetCapabilities.GetTitle();
                wfsFolder = AddFolderLayer(folderName);
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
                // we return the parent layer, the sub layers will be created internally by the parent
                return new Layer(wfsFolder);
            }

            var isWfsGetFeature = OgcWebServicesUtility.IsValidUrl(url, ServiceType.Wfs, RequestType.GetFeature);
            if (isWfsGetFeature)
            {
                var queryParameters = QueryString.Decode(sourceUrl);
                var paramName = WfsGetCapabilities.ParameterNameOfTypeNameBasedOnVersion(OgcWebServicesUtility.GetVersionFromUrl(url));
                string featureType = null;
                string crs = null;
                foreach (KeyValuePair<string, QueryParameter> kv in queryParameters)
                {
                    if(string.Equals(kv.Key, paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        string val = kv.Value.ToString();
                        int equalsIndex = val.IndexOf('=');
                        featureType = equalsIndex >= 0 && equalsIndex < val.Length - 1 ? val.Substring(equalsIndex + 1) : val;                        
                    }
                    if(string.Equals(kv.Key, "srsname", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = kv.Value.ToString();
                        int equalsIndex = val.IndexOf('=');
                        crs = equalsIndex >= 0 && equalsIndex < val.Length - 1 ? val.Substring(equalsIndex + 1) : val;
                    }
                }
                if (string.IsNullOrEmpty(featureType) == false)
                {                     
                    // Can't deduct a human-readable title at the moment, we should add that we always query for the
                    // capabilities; this also helps with things like outputFormat and CRS
                    return AddWFSLayer(featureType, sourceUrl, crs, wfsFolder, featureType, geoJsonOutputFormatString);
                }
            }
            
            throw new ArgumentException("Unrecognized WFS request type: " + url);
        }
        
        private LayerData AddFolderLayer(string folderName)
        {
            var builder = new LayerBuilder().OfType("folder").NamedAs(folderName); //todo: make preset?
            var wfsFolder = App.Layers.Add(builder);
            return wfsFolder.LayerData;
        }

        private Layer AddWFSLayer(string featureType, string sourceUrl, string crsType, LayerData folderLayer, string title, string geoJsonOutputFormatString)
        {
            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerGetFeatureUri(featureType, sourceUrl, crsType, geoJsonOutputFormatString);
            var getFeatureUrl = uriBuilder.Uri;

            Debug.Log($"Adding WFS layer '{featureType}' with url '{getFeatureUrl}'");

            return App.Layers.Add(
                new WfsLayerPreset.Args(
                    getFeatureUrl,
                    title,
                    folderLayer
                )
            );
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

            var parameters = QueryString.Decode(uriBuilder.Query);

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetFeature");
            uriBuilder.SetQueryParameter("version", wfsVersion);
            uriBuilder.SetQueryParameter(WfsGetCapabilities.ParameterNameOfTypeNameBasedOnVersion(wfsVersion), featureType);
            if (parameters.Single("outputFormat")?.ToLower() is not ("json" or "geojson"))
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