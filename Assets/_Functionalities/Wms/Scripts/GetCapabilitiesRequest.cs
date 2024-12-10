using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Netherlands3D.Twin.Wms
{
    public class GetCapabilitiesRequest : BaseRequest
    {
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

                var bboxFilter = boundingBoxNodes?.Count > 0;

                // Loop through each BoundingBox node to check if it exists
                boundingBoxNodes = xmlDocument.SelectNodes("//*[local-name()='BoundingBox']", namespaceManager);
                if (boundingBoxNodes?.Count > 0)
                {
                    bboxFilter = true; // Set to true if any BoundingBox nodes exist
                }

                return bboxFilter;
            }
        }

        public static bool Supports(Uri url, string contents)
        {
            if (IsSupportedUrl(url, "GetCapabilities"))
            {
                return true;
            }

            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            return contents.Contains("<WMS_Capabilities") || contents.Contains("<wms:WMS_Capabilities");
        }
        
        public GetCapabilitiesRequest(Uri url, string cachedBodyFilePath) : base(url, cachedBodyFilePath)
        {
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
            return new MapFilters
            {
                version = Version,
                width = width,
                height = height,
                transparent = transparent,
                spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(Version))
            };
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
    }
}