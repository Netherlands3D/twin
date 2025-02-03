using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class GetCapabilitiesRequest : BaseRequest, IGetCapabilitiesRequest
    {
        public ServiceType ServiceType => ServiceType.Wms;
        protected override Dictionary<string, string> defaultNameSpaces => OgcWebServicesUtility.DefaultWmsNamespaces;
        public bool CapableOfBoundingBoxes => xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox' or local-name()='BoundingBox']", namespaceManager) != null;

        public GetCapabilitiesRequest(Uri url, string xml) : base(url, xml)
        {
        }
        
        public string GetTitle()
        {
            return GetInnerTextForNode(xmlDocument.DocumentElement, "Title");
        }

        public string GetVersion()
        {
            var versionInXml = xmlDocument.DocumentElement.GetAttribute("version");
            return  string.IsNullOrEmpty(versionInXml) ? versionInXml : defaultFallbackVersion;
        }

        public BoundingBoxContainer GetBounds()
        {
            var container = new BoundingBoxContainer(Url.ToString());

            // Select EX_GeographicBoundingBox node first
            var bboxNode = xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox']", namespaceManager);
            if (bboxNode != null)
            {
                double minX = double.Parse(bboxNode.SelectSingleNode("*[local-name()='westBoundLongitude']", namespaceManager)?.InnerText ?? "0");
                double minY = double.Parse(bboxNode.SelectSingleNode("*[local-name()='southBoundLatitude']", namespaceManager)?.InnerText ?? "0");
                double maxX = double.Parse(bboxNode.SelectSingleNode("*[local-name()='eastBoundLongitude']", namespaceManager)?.InnerText ?? "0");
                double maxY = double.Parse(bboxNode.SelectSingleNode("*[local-name()='northBoundLatitude']", namespaceManager)?.InnerText ?? "0");

                var bl = new Coordinate(CoordinateSystem.CRS84, minX, minY);
                var tr = new Coordinate(CoordinateSystem.CRS84, maxX, maxY);

                container.GlobalBoundingBox = new BoundingBox(bl, tr);
            }

            // Select BoundingBox nodes per layer
            var bboxNodes = xmlDocument.SelectNodes("//*[local-name()='Layer']", namespaceManager);
            foreach (XmlNode layerNode in bboxNodes)
            {
                var layerNameNode = layerNode.SelectSingleNode("*[local-name()='Name']", namespaceManager);
                var layerName = layerNameNode?.InnerText;
                if (string.IsNullOrEmpty(layerName)) continue;

                var boundingBoxNode = layerNode.SelectSingleNode("*[local-name()='BoundingBox']", namespaceManager);
                if (boundingBoxNode != null)
                {
                    string crsString = boundingBoxNode.Attributes["CRS"]?.Value;
                    var hasCRS = CoordinateSystems.FindCoordinateSystem(crsString, out var crs);

                    if (!hasCRS)
                    {
                        crs = CoordinateSystem.CRS84; //default
                        Debug.LogWarning("Custom CRS BBox found, but not able to be parsed, defaulting to WGS84 CRS. Founds CRS string: " + crsString);
                    }

                    double minX = double.Parse(boundingBoxNode.Attributes["minx"].Value);
                    double minY = double.Parse(boundingBoxNode.Attributes["miny"].Value);
                    double maxX = double.Parse(boundingBoxNode.Attributes["maxx"].Value);
                    double maxY = double.Parse(boundingBoxNode.Attributes["maxy"].Value);

                    var bl = new Coordinate(crs, minX, minY);
                    var tr = new Coordinate(crs, maxX, maxY);

                    container.LayerBoundingBoxes[layerName] = new BoundingBox(bl, tr);
                }
            }

            return container;
        }

        public static bool Supports(Uri url, string contents)
        {
            if (OgcWebServicesUtility.IsSupportedUrl(url, ServiceType.Wms, RequestType.GetCapabilities))
            {
                return true;
            }

            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            return contents.Contains("<WMS_Capabilities") || contents.Contains("<wms:WMS_Capabilities");
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
            var version = GetVersion();
            return new MapFilters
            {
                version = version,
                width = width,
                height = height,
                transparent = transparent,
                spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(version))
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