using System.IO;
using System.Xml;
using UnityEngine;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System;
using Netherlands3D.Web;
using Netherlands3D.CartesianTiles;
using System.Collections.Generic;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private Material visualizationMaterial;
        [SerializeField] private LineRenderer3D lineRenderer3D;
        [SerializeField] private BatchedMeshInstanceRenderer pointRenderer3D;

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

                // Is there a bbox filter? We need it to do per-tile requests.
                bool bboxFilterCapability = WFSBboxFilterCapability(xmlDocument, namespaceManager);
                if (!bboxFilterCapability)
                {
                    Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                    return false;
                }

                // Does the GetFeature operation support GeoJSON output?
                bool getFeatureNodeHasGeoJsonOutput = NodeHasGeoJSONOutput(getFeatureOperationNode, namespaceManager);
                if (!getFeatureNodeHasGeoJsonOutput)
                {
                    Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                    return false;
                }
            }

            if (getFeatureRequest)
            {
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
            if(getCapabilitiesRequest)
            {
                var featureTypes = GetFeatureTypes(localFile);
                foreach (var featureType in featureTypes)
                    AddWFSLayer(featureType, sourceUrl);
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
                AddWFSLayer(featureType, sourceUrl);
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

        private static bool NodeHasGeoJSONOutput(XmlNode xmlNode, XmlNamespaceManager namespaceManager = null)
        {
            // Check if operation GetFeature has a outputFormat of something like json/geojson
            var featureOutputFormat = xmlNode.SelectSingleNode("ows:Parameter[@name='outputFormat']", namespaceManager);
            var owsAllowedValues = featureOutputFormat.SelectSingleNode("ows:AllowedValues", namespaceManager);
            foreach (XmlNode owsValue in owsAllowedValues.ChildNodes)
            {
                if (owsValue.InnerText.Contains("json") || owsValue.InnerText.Contains("geojson"))
                    return true;
            }

            Debug.LogWarning("WFS GetFeature operation does not support GeoJSON output format.");
            return false;
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
                var featureTypeName = featureTypeNode.SelectSingleNode("//*[local-name()='Name']", namespaceManager).InnerText;
                featureTypes.Add(featureTypeName);
            }

            return featureTypes.ToArray();
        }

        private void AddWFSLayer(string featureType, string sourceUrl)
        {
            Debug.Log("Adding WFS layer: " + featureType);

            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);
            uriBuilder.RemoveQueryParameter("bbox");
            uriBuilder.RemoveQueryParameter("typeNames");
            uriBuilder.RemoveQueryParameter("request");
            uriBuilder.RemoveQueryParameter("outputFormat");
            uriBuilder.RemoveQueryParameter("service");
            uriBuilder.RemoveQueryParameter("version");

            // The exact bbox coordinates will be managed by CartesianTileWFSLayer
            uriBuilder.AddQueryParameter("bbox", "{bbox}");
            uriBuilder.AddQueryParameter("typeNames", featureType);
            uriBuilder.AddQueryParameter("request", "GetFeature");
            uriBuilder.AddQueryParameter("outputFormat", "geojson");
            uriBuilder.AddQueryParameter("service", "WFS");
            uriBuilder.AddQueryParameter("version", "2.0.0");

            var getFeatureUrl = uriBuilder.Uri.ToString();

            // Create a new GeoJSON layer per GetFeature, with a 'live' datasource
            var go = new GameObject(featureType);
            var layer = go.AddComponent<GeoJSONLayer>();
            layer.RandomizeColorPerFeature = true;
            layer.SetDefaultVisualizerSettings(visualizationMaterial, lineRenderer3D, pointRenderer3D);

            // Create a new WFSGeoJSONTileDataLayer that can inject the Features loaded from tiles into the GeoJSONLayer
            var cartesianTileLayer = go.AddComponent<WFSGeoJSONTileDataLayer>();              
            cartesianTileLayer.GeoJSONLayer = layer;
            cartesianTileLayer.WfsUrl = getFeatureUrl;
        }
    }
}
