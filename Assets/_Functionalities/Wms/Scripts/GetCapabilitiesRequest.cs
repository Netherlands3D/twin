using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

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
        
        public List<Map> GetMaps(int width, int height, bool transparent)
        {
            // Select the Layer nodes from the WMS capabilities document
            var capabilityNode = GetSingleNodeByName(xmlDocument, "Capability");
            var mapNodes = capabilityNode.SelectNodes(".//*[local-name()='Layer']/*[local-name()='Layer']", namespaceManager);                

            // Create a template that we can use as a basis for individual layers
            var mapTemplate = CreateMapTemplate(width, height, transparent);

            var maps = new List<Map>();

            // Loop through the Layer nodes and get their names
            foreach (XmlNode mapNode in mapNodes)
            {
                // Extract the Name node for each layer
                var layerNameNode = GetInnerTextForNode(mapNode, "Name");
                if (string.IsNullOrEmpty(layerNameNode)) continue;

                // Extract styles for the layer
                var styles = ExtractStyles(mapNode);

                var map = new Map()
                {
                    name = layerNameNode,
                    version = mapTemplate.version,
                    width = mapTemplate.width,
                    height = mapTemplate.height,
                    transparent = mapTemplate.transparent,
                    spatialReferenceType = mapTemplate.spatialReferenceType,
                    spatialReference = GetInnerTextForNode(mapNode, mapTemplate.spatialReferenceType),
                    style = styles.FirstOrDefault()
                };
                maps.Add(map);
            }

            // Return the list of layer names as an array
            return maps;
        }

        private Map CreateMapTemplate(int width, int height, bool transparent)
        {
            var mapTemplate = new Map
            {
                version = Version,
                width = width,
                height = height,
                transparent = transparent,
                spatialReferenceType = "SRS"
            };

            if (new Version(mapTemplate.version) >= new Version("1.3.0"))
            {
                mapTemplate.spatialReferenceType = "CRS";
            }

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

        private string GetInnerTextForNode(XmlNode layerNode, string nodeName)
        {
            return GetSingleNodeByName(layerNode, nodeName).InnerText;
        }

        private XmlNode GetSingleNodeByName(XmlNode layerNode, string nodeName)
        {
            return layerNode.SelectSingleNode($".//*[local-name()='{nodeName}']", namespaceManager);
        }
    }
}