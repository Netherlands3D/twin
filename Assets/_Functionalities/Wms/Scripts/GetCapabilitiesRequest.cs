using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.Twin.Wms
{
    public class GetCapabilitiesRequest
    {
        private readonly XmlDocument xmlDocument;
        private readonly XmlNamespaceManager namespaceManager;

        public static bool Supports(Uri url, string contents)
        {
            var queryString = url.Query.ToLower();
            
            if (queryString.Contains("service=wms") && queryString.Contains("request=getcapabilities"))
            {
                return true;
            }

            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            return contents.Contains("<WMS_Capabilities") || contents.Contains("<wms:WMS_Capabilities");
        }
        
        public GetCapabilitiesRequest(string cachedBodyFilePath)
        {
            this.xmlDocument = new XmlDocument();
            this.xmlDocument.Load(cachedBodyFilePath);
            this.namespaceManager = CreateNameSpaceManager(this.xmlDocument);
        }

        public string Version
        {
            get
            {
                // Use XPath to select the root node and get the version attribute
                var rootNode = xmlDocument.SelectSingleNode("/*");

                // Return null if root node or version attribute is not found
                if (rootNode == null || rootNode.Attributes == null) return null; 

                // if the root node is found and retrieve the version attribute
                var versionAttribute = rootNode.Attributes["version"];

                return versionAttribute?.Value; // Return the version value or null if not found
            }
        }
        
        public bool CapableOfBoundingBoxes
        {
            get
            {
                // Select all BoundingBox nodes in the document
                var boundingBoxNodes =
                    xmlDocument.SelectNodes("//*[local-name()='EX_GeographicBoundingBox']", namespaceManager);

                var bboxFilter = boundingBoxNodes is { Count: > 0 };

                // Loop through each BoundingBox node to check if it exists
                boundingBoxNodes = xmlDocument.SelectNodes("//*[local-name()='BoundingBox']", namespaceManager);
                if (boundingBoxNodes is { Count: > 0 })
                {
                    bboxFilter = true; // Set to true if any BoundingBox nodes exist
                }

                return bboxFilter;
            }
        }

        private XmlNamespaceManager CreateNameSpaceManager(XmlDocument xmlDocument)
        {
            XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
            XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
            namespaceManager.AddNamespace("wms", "http://www.opengis.net/wms");
            namespaceManager.AddNamespace("sld", "http://www.opengis.net/sld");
            namespaceManager.AddNamespace("ms", "http://mapserver.gis.umn.edu/mapserver");

            if (elementsWithNamespaces == null) return namespaceManager;

            foreach (XmlElement element in elementsWithNamespaces)
            {
                if (string.IsNullOrEmpty(element.NamespaceURI)) continue;
                
                string prefix = element.Name.Split(':')[0];
                if (string.IsNullOrEmpty(prefix) || namespaceManager.LookupNamespace(prefix) != null) continue;
                
                namespaceManager.AddNamespace(prefix, element.NamespaceURI);
            }

            return namespaceManager;
        }
        
        public List<MapFilters> GetMaps(int width, int height, bool transparent)
        {
            // Select the Layer nodes from the WMS capabilities document
            var capabilityNode = GetSingleNodeByName(xmlDocument, "Capability");
            var mapNodes = capabilityNode.SelectNodes(".//*[local-name()='Layer']/*[local-name()='Layer']", namespaceManager);                

            // Create a template that we can use as a basis for individual layers
            var mapTemplate = CreateMapTemplate(width, height, transparent);

            var maps = new List<MapFilters>();

            // Loop through the Layer nodes and get their names
            foreach (XmlNode mapNode in mapNodes)
            {
                // Extract the Name node for each layer
                var layerNameNode = GetInnerTextForNode(mapNode, "Name");
                if (string.IsNullOrEmpty(layerNameNode)) continue;

                // Extract styles for the layer
                var styles = ExtractStyles(mapNode);

                // CRS/SRS may be defined in the current MapNode, but can also inherit from a parent if it is not
                // specified the flag at the end of this function will check the current node and its parents
                var spatialReference = GetInnerTextForNode(mapNode, mapTemplate.spatialReferenceType, true);
                
                var map = new MapFilters()
                {
                    name = layerNameNode,
                    version = mapTemplate.version,
                    width = mapTemplate.width,
                    height = mapTemplate.height,
                    transparent = mapTemplate.transparent,
                    spatialReferenceType = mapTemplate.spatialReferenceType,
                    spatialReference = spatialReference,
                    style = styles.FirstOrDefault()
                };
                maps.Add(map);
            }

            // Return the list of layer names as an array
            return maps;
        }

        private MapFilters CreateMapTemplate(int width, int height, bool transparent)
        {
            var mapTemplate = new MapFilters
            {
                version = Version,
                width = width,
                height = height,
                transparent = transparent,
                spatialReferenceType = GetMapRequest.SpatialReferenceTypeFromVersion(new Version(Version))
            };
            
            return mapTemplate;
        }

        private IEnumerable<string> ExtractStyles(XmlNode layerNode)
        {
            var styleNodes = GetMultipleNodesByName(layerNode, "Style");

            var styles = new List<string>();
            foreach (XmlNode styleNode in styleNodes)
            {
                var styleNameNode = GetInnerTextForNode(styleNode, "Name");
                if (string.IsNullOrEmpty(styleNameNode)) continue;

                styles.Add(styleNameNode);
            }

            return styles;
        }

        private XmlNodeList GetMultipleNodesByName(XmlNode layerNode, string nodeName)
        {
            return layerNode.SelectNodes($".//*[local-name()='{nodeName}']", namespaceManager);
        }

        private string GetInnerTextForNode(XmlNode layerNode, string nodeName, bool searchInParents = false)
        {
            try
            {
                return GetSingleNodeByName(layerNode, nodeName, searchInParents).InnerText;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get text for node {nodeName} in node {layerNode.InnerXml}: " + e.Message);
                return "";
            }
        }

        private XmlNode GetSingleNodeByName(XmlNode layerNode, string nodeName, bool searchInParents = false)
        {
            // Base query that will attempt to find the node; but we need more ...
            var queryForNode = $"*[local-name()='{nodeName}']";
            
            if (searchInParents)
            {
                // ... when this flag is provided, check 'self' first and then traverse ancestors, or ...
                return layerNode.SelectSingleNode($"ancestor-or-self::*/child::{queryForNode}", namespaceManager);
            }

            // ... without the flag we limit the search to the layerNode for performance and to prevent unwanted hits
            return layerNode.SelectSingleNode($".//{queryForNode}", namespaceManager);
        }
    }
}