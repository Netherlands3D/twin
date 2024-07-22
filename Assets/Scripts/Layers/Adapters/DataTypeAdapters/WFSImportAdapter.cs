using System.IO;
using System.Xml;
using UnityEngine;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System;
using Netherlands3D.Web;
using Netherlands3D.CartesianTiles;

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

                // Can we request specific features via GetFeature requests?
                XmlNode getFeatureOperationNode = ReadGetFeatureNode(xmlDocument);
                if (getFeatureOperationNode == null){
                    Debug.Log("<color=orange>WFS GetFeature operation not found.</color>");
                    return false;
                }

                // Is there a bbox filter? We need it to do per-tile requests.
                bool bboxFilterCapability = WFSBboxFilterCapability(xmlDocument);
                if (!bboxFilterCapability){
                    Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                    return false;
                }

                // Does the GetFeature operation support GeoJSON output?
                bool getFeatureNodeHasGeoJsonOutput = NodeHasGeoJSONOutput(getFeatureOperationNode);
                if(!getFeatureNodeHasGeoJsonOutput){
                    Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                    return false;
                }
            }

            if(getFeatureRequest)
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

        private static bool WFSBboxFilterCapability(XmlDocument xmlDocument)
        {
            // Find the Filter_Capabilities in the root of the XMLdocucument
            var filterCapabilitiesNodeInRoot = xmlDocument.SelectSingleNode("wfs:WFS_Capabilities/fes:Filter_Capabilities/fes:Spatial_Capabilities/fes:SpatialOperators");
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

        private static bool NodeHasGeoJSONOutput(XmlNode xmlNode)
        {
            // Check if operation GetFeature has a outputFormat of something like json/geojson
            var featureOutputFormat = xmlNode.SelectSingleNode("ows:Parameter[@name='outputFormat']");
            var owsAllowedValues = featureOutputFormat.SelectSingleNode("ows:AllowedValues");
            foreach (XmlNode owsValue in owsAllowedValues.ChildNodes)
            {
                if (owsValue.InnerText.Contains("json") || owsValue.InnerText.Contains("geojson"))
                    return true;
            }

            Debug.LogWarning("WFS GetFeature operation does not support GeoJSON output format.");
            return false;
        }

        private static XmlNode ReadGetFeatureNode(XmlDocument xmlDocument)
        {
            // Find the ows:Operation node with name GetFeature
            var operationNodes = xmlDocument.GetElementsByTagName("ows:Operation");
            XmlNode getFeatureOperationNode = null;
            foreach (XmlNode operationNode in operationNodes)
            {
                if (operationNode.Attributes["name"].Value == "GetFeature")
                {
                    getFeatureOperationNode = operationNode;
                }
            }

            if (getFeatureOperationNode == null)
                Debug.LogWarning("WFS GetFeature operation not found.");

            return getFeatureOperationNode;
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

        private string[] GetFeatureTypes(LocalFile localFile)
        {
            // Read the XML data to find the list of feature types
            var cachedDataPath = localFile.LocalFilePath;
            var dataAsText = File.ReadAllText(cachedDataPath);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(dataAsText);

            // Find the FeatureTypeList in the root of the XMLdocucument
            var featureTypeListNodeInRoot = xmlDocument.SelectSingleNode("wfs:WFS_Capabilities/wfs:FeatureTypeList");
            var featureTypes = new string[featureTypeListNodeInRoot.ChildNodes.Count];
            for (int i = 0; i < featureTypeListNodeInRoot.ChildNodes.Count; i++)
            {
                featureTypes[i] = featureTypeListNodeInRoot.ChildNodes[i].SelectSingleNode("Name").InnerText;
            }

            return featureTypes;
        }

        private void AddWFSLayer(string featureType, string sourceUrl)
        {
            Debug.Log("Adding WFS layer: " + featureType);

            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);
            uriBuilder.RemoveQueryParameter("bbox");
            uriBuilder.RemoveQueryParameter("typeNames");
            uriBuilder.RemoveQueryParameter("request");

            // The exact bbox coordinates will be managed by CartesianTileWFSLayer
            uriBuilder.AddQueryParameter("bbox", "{bbox}");
            uriBuilder.AddQueryParameter("typeNames", featureType);
            uriBuilder.AddQueryParameter("request", "GetFeature");

            var getFeatureUrl = uriBuilder.Uri.ToString();

            // Create a new GeoJSON layer per feature, with a 'live' datasource
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
