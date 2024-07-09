using System.IO;
using System.Xml;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;


namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;

            // There are a lot of different WFS urls formats in the wild. This is a very basic check to see if it's a WFS service url.
            var getCapabilitiesRequest = sourceUrl.ToLower().Contains("request=getcapabilities");
            var getFeatureRequest = sourceUrl.ToLower().Contains("request=getfeature");

            if(!getCapabilitiesRequest || getFeatureRequest)
                return false;


            //Check if a GetFeature operation with GeoJSON as output format is supported
            var dataAsText = File.ReadAllText(cachedDataPath);
            if(getCapabilitiesRequest)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(dataAsText);

                // Can we request specific features via GetFeature requests?
                XmlNode getFeatureOperationNode = ReadGetFeatureNode(xmlDocument);
                if (getFeatureOperationNode == null)
                    return false;

                // Is there a bbox filter? We need it to do per-tile requests.
                bool bboxFilterCapability = WFSBboxFilterCapability(xmlDocument);
                if (!bboxFilterCapability)
                    return false;

                // Does the GetFeature operation support GeoJSON output?
                bool getFeatureNodeHasGeoJsonOutput = NodeHasGeoJSONOutput(getFeatureOperationNode);
                if(!getFeatureNodeHasGeoJsonOutput)
                    return false;
            }

            if(getFeatureRequest)
            {
                //Check if text is GeoJSON by trying to parse feature collection
                var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(dataAsText);
                if(featureCollection == null || featureCollection.Features.Count == 0)
                    return false;
            }

            return false;
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

            return getFeatureOperationNode;
        }

        public void Execute(LocalFile localFile)
        {
            // GetCapabilities? Retrieve all possible feature types
            // GetFeature? Retrieve specific feature type

            // Construct specific bbox query URL's from source url for CartesianTiles layer
            // Generate the layer
        }
    }
}
