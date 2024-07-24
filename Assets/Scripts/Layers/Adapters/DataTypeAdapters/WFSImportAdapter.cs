using System.IO;
using System.Xml;
using UnityEngine;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System;
using Netherlands3D.Web;
using Netherlands3D.CartesianTiles;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI.LayerInspector;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;

        private string geoJsonOutputFormat = "";
        private string wfsVersion = "";
        private const string defaultFallbackVersion = "2.0.0"; // Default to 2.0.0 (released in 2010, compliant with ISO standards)

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;

            Debug.Log("Checking source WFS url: " + sourceUrl);

            // There are a lot of different WFS urls formats in the wild. This is a very basic check to see if it's a WFS service url.
            var getCapabilitiesRequest = sourceUrl.ToLower().Contains("request=getcapabilities");
            var getFeatureRequest = sourceUrl.ToLower().Contains("request=getfeature");

            if(!getCapabilitiesRequest && !getFeatureRequest)
            {
                Debug.Log("<color=orange>WFS url does not contain a GetCapabilities or GetFeature request.</color>");
                return false;
            }


            //Check if a GetFeature operation with GeoJSON as output format is supported
            var dataAsText = File.ReadAllText(cachedDataPath);
            if(getCapabilitiesRequest)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(dataAsText);

                // Read namespace managers from the root WFS_Capabilities node
                XmlNamespaceManager namespaceManager = ReadNameSpaceManager(xmlDocument);
                
                // Can we request specific features via GetFeature requests?
                XmlNode getFeatureOperationNode = ReadGetFeatureNode(xmlDocument, namespaceManager);
                if (getFeatureOperationNode == null)
                {
                    Debug.Log("<color=orange>WFS GetFeature operation not found.</color>");
                    return false;
                }

                // Is there a bbox filter? We need it that capability to do per-tile requests.
                bool bboxFilterCapability = WFSBboxFilterCapability(xmlDocument, namespaceManager);
                if (!bboxFilterCapability)
                {
                    Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                    return false;
                }

                // Does the GetFeature operation support GeoJSON output?
                geoJsonOutputFormat = GetGeoJSONOutputFormat(getFeatureOperationNode, namespaceManager);
                if (string.IsNullOrEmpty(geoJsonOutputFormat))
                {
                    Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                    return false;
                }

                // Get the WFS version
                wfsVersion = GetWFSVersion(sourceUrl);
                if(string.IsNullOrEmpty(wfsVersion))
                    wfsVersion = GetWFSVersion(xmlDocument, namespaceManager);
            }

            if (getFeatureRequest)
            {
                // Get wfs version from url
                wfsVersion = GetWFSVersion(sourceUrl);

                //Check if text is GeoJSON by trying to parse feature collection
                var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(dataAsText);
                if(featureCollection == null || featureCollection.Features.Count == 0)
                {
                    Debug.Log("<color=orange>WFS GetFeature request does not contain GeoJSON data.</color>");
                    return false;
                }
            }

            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var sourceUrl = localFile.SourceUrl;

            var getCapabilitiesRequest = sourceUrl.ToLower().Contains("request=getcapabilities");
            var wfsFolder = new FolderLayer(sourceUrl);

            if(getCapabilitiesRequest)
            {
                var featureTypes = GetFeatureTypes(localFile);

                //Create a folder layer 
                foreach (var featureType in featureTypes)
                {
                    Debug.Log("Adding WFS layer for featureType: " + featureType);
                    AddWFSLayer(featureType, sourceUrl, wfsFolder);
                }
                return;
            }

            var getFeatureRequest = sourceUrl.ToLower().Contains("request=getfeature");
            if(getFeatureRequest)
            {
                // Get the feature type from the url
                var featureType = string.Empty;
                if (sourceUrl.ToLower().Contains("typename="))
                {
                    //WFS 1.0.0 uses 'typename'
                    featureType = sourceUrl.ToLower().Split("typename=")[1].Split("&")[0];
                }
                else if (sourceUrl.ToLower().Contains("typenames="))
                {
                    //WFS 2 uses plural 'typenames'
                    featureType = sourceUrl.ToLower().Split("typenames=")[1].Split("&")[0];
                }
                AddWFSLayer(featureType, sourceUrl, wfsFolder);
                return;
            }
        }


        private XmlNamespaceManager ReadNameSpaceManager(XmlDocument xmlDocument)
        {
            XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
            XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
            namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs");   

            if (elementsWithNamespaces != null)
            {
                foreach (XmlElement element in elementsWithNamespaces)
                {
                    if (!string.IsNullOrEmpty(element.NamespaceURI))
                    {
                        string prefix = element.Name.Split(':')[0];
                        if (!string.IsNullOrEmpty(prefix) && namespaceManager.LookupNamespace(prefix) == null)
                        {
                            namespaceManager.AddNamespace(prefix, element.NamespaceURI);
                        }
                    }
                }
            }

            return namespaceManager;
        }

        private static bool WFSBboxFilterCapability(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
        {
            // Find the SpatialOperators
            var filterCapabilitiesNodeInRoot = xmlDocument.SelectSingleNode("//fes:SpatialOperators", namespaceManager);
            var bboxFilter = false;
            foreach (XmlNode spatialOperator in filterCapabilitiesNodeInRoot.ChildNodes)
            {
                if (spatialOperator.Attributes["name"].Value.ToLower() == "bbox")
                {
                    bboxFilter = true;
                }
            }

            return bboxFilter;
        }

        private static string GetGeoJSONOutputFormat(XmlNode xmlNode, XmlNamespaceManager namespaceManager = null)
        {
            // Check if operation GetFeature has a outputFormat of something like json/geojson
            var featureOutputFormat = xmlNode.SelectSingleNode("ows:Parameter[@name='outputFormat']", namespaceManager);
            var owsAllowedValues = featureOutputFormat.SelectSingleNode("ows:AllowedValues", namespaceManager);
            foreach (XmlNode owsValue in owsAllowedValues.ChildNodes)
            {
                // GeoJSON would be the best match, but a lot of different notations are used in the wild
                if (owsValue.InnerText.ToLower().Contains("geojson") || owsValue.InnerText.ToLower().Contains("json"))
                    return "geojson";
            }

            Debug.LogWarning("WFS GetFeature operation does not support GeoJSON output format.");
            return "";
        }

        private static string GetWFSVersion(XmlNode xmlNode, XmlNamespaceManager namespaceManager = null)
        {
            // Get ServiceTypeVersion node inner text
            var serviceTypeVersion = xmlNode.SelectSingleNode("//ows:ServiceTypeVersion", namespaceManager);
            if(serviceTypeVersion != null)
            {
                Debug.Log("WFS version found: " + serviceTypeVersion.InnerText);
                return serviceTypeVersion.InnerText;
            }
            return "";
        }
        private static string GetWFSVersion(string url)
        {
            var urlLower = url.ToLower();
            var versionQueryKey = "version=";
            if (urlLower.Contains(versionQueryKey))
                return urlLower.Split(versionQueryKey)[1].Split("&")[0];

            return "";
        }

        private static XmlNode ReadGetFeatureNode(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
        {
            // Find the <ows:Operation name="GetFeature"> node
            var getFeatureOperationNode = xmlDocument.SelectSingleNode("//ows:Operation[@name='GetFeature']", namespaceManager);
            
            if (getFeatureOperationNode == null)
                Debug.LogWarning("WFS GetFeature operation not found.");

            return getFeatureOperationNode;
        }

        private string[] GetFeatureTypes(LocalFile localFile)
        {
            // Read the XML data to find the list of feature types
            var cachedDataPath = localFile.LocalFilePath;
            var dataAsText = File.ReadAllText(cachedDataPath);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(dataAsText);

            XmlNamespaceManager namespaceManager = ReadNameSpaceManager(xmlDocument);

            // Find the FeatureTypeList node somewhere in xmldocument(that might not start with wfs:)
            var featureTypeListNodeInRoot = xmlDocument.SelectSingleNode("//*[local-name()='FeatureTypeList']", namespaceManager);
            var featureTypeChildNodes = featureTypeListNodeInRoot.ChildNodes;
            var featureTypes = new List<string>();

            foreach(XmlNode featureTypeNode in featureTypeChildNodes)
            {
                var featureTypeName = featureTypeNode.SelectSingleNode(".//*[local-name()='Name']", namespaceManager).InnerText;
                featureTypes.Add(featureTypeName);
            }

            return featureTypes.ToArray();
        }

        private void AddWFSLayer(string featureType, string sourceUrl, FolderLayer folderLayer)
        {
            Debug.Log("Adding WFS layer: " + featureType);

            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Make sure we have a wfsVersion set
            if(string.IsNullOrEmpty(wfsVersion))
            {
                Debug.LogWarning("WFS version could not be determined, defaulting to " + defaultFallbackVersion);
                wfsVersion = defaultFallbackVersion;
            }

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetFeature");
            uriBuilder.SetQueryParameter("version", wfsVersion);
            uriBuilder.SetQueryParameter("typeNames", featureType);
            uriBuilder.SetQueryParameter("outputFormat", geoJsonOutputFormat);
            uriBuilder.SetQueryParameter("bbox", "{bbox}"); // Bbox value is injected by CartesianTileWFSLayer

            var getFeatureUrl = uriBuilder.Uri.ToString();

            // Create a new GeoJSON layer per GetFeature, with a 'live' datasource
            var layerGameObject = new GameObject(featureType);
            var layer = layerGameObject.AddComponent<GeoJSONLayer>();
            layer.ReferencedProxy.SetParent(folderLayer);
            layer.RandomizeColorPerFeature = true;
            layer.SetDefaultVisualizerSettings(visualizationMaterial, lineRenderer3D, pointRenderer3D);

            // Create a new WFSGeoJSONTileDataLayer that can inject the Features loaded from tiles into the GeoJSONLayer
            var cartesianTileLayer = layerGameObject.AddComponent<WFSGeoJSONTileDataLayer>();              
            cartesianTileLayer.GeoJSONLayer = layer;
            cartesianTileLayer.WfsUrl = getFeatureUrl;
        }
    }
}
