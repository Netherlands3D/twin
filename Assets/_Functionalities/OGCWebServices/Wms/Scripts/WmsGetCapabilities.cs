using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.OgcWebServices.Wms
{
    public class WmsGetCapabilities : BaseRequest, IGetCapabilities
    {
        public Uri GetCapabilitiesUri => Url;
        public const string DefaultFallbackVersion = "1.3.0";

        public ServiceType ServiceType => ServiceType.Wms;
        protected override Dictionary<string, string> defaultNameSpaces => new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wms", "http://www.opengis.net/wms" },
            { "sld", "http://www.opengis.net/sld" },
            { "ms", "http://mapserver.gis.umn.edu/mapserver" },
            { "schemaLocation", "http://www.opengis.net/wms" }
        };

        public bool CapableOfBoundingBoxes => xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox' or local-name()='BoundingBox']", namespaceManager) != null;

        public bool HasBounds //todo: this is suboptimal because it uses the GetBounds function, maybe cache the bounds
        {
            get
            {
                var bounds = GetBounds();
                if (bounds.GlobalBoundingBox == null && bounds.LayerBoundingBoxes.Count == 0)
                    return false;
                return true;
            }
        }

        public WmsGetCapabilities(Uri url, string xml) : base(url, xml)
        {
        }

        public string GetVersion()
        {
            //try to get version from the url
            var urlLower = Url.ToString().ToLower();
            var versionQueryKey = "version=";
            if (urlLower.Contains(versionQueryKey))
                return urlLower.Split(versionQueryKey)[1].Split("&")[0];

            //try to get the version from the body, or return the default
            var versionInXml = xmlDocument.DocumentElement.GetAttribute("version");
            return !string.IsNullOrEmpty(versionInXml) ? versionInXml : DefaultFallbackVersion;
        }

        public string GetTitle()
        {
            return GetInnerTextForNode(xmlDocument.DocumentElement, "Title");
        }

        public List<string> GetLayerNames()
        {
            List<string> layerNames = new List<string>();
            var LayerNodes = xmlDocument.SelectNodes("//*[local-name()='Layer']", namespaceManager);
            foreach (XmlNode layerNode in LayerNodes)
            {
                var nameNode = layerNode.SelectSingleNode("*[local-name()='Name']", namespaceManager);
                if (nameNode != null && !string.IsNullOrEmpty(nameNode.InnerText))
                {
                    layerNames.Add(nameNode.InnerText);
                }
            }

            return layerNames;
        }

        public BoundingBoxContainer GetBounds()
        {
            var container = new BoundingBoxContainer(Url.ToString());

            // Select EX_GeographicBoundingBox node first
            var bboxNode = xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox']", namespaceManager);
            if (bboxNode != null)
            {
                container.GlobalBoundingBox = ParseBoundingBox(bboxNode, CoordinateSystem.CRS84);
            }

            // Select BoundingBox nodes per layer
            var LayerNodes = xmlDocument.SelectNodes("//*[local-name()='Layer']", namespaceManager);
            foreach (XmlNode layerNode in LayerNodes)
            {
                string layerName = layerNode.SelectSingleNode("*[local-name()='Name']", namespaceManager)?.InnerText;
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

                    container.LayerBoundingBoxes[layerName] = ParseBoundingBox(boundingBoxNode, crs);
                }
            }

            return container;
        }

        private BoundingBox ParseBoundingBox(XmlNode node, CoordinateSystem crs)
        {
            if (node == null)
                return null;

            var minXString = node.SelectSingleNode("*[local-name()='westBoundLongitude' or @minx]", namespaceManager)?.InnerText;
            var minYString = node.SelectSingleNode("*[local-name()='southBoundLatitude' or @miny]", namespaceManager)?.InnerText;
            var maxXString = node.SelectSingleNode("*[local-name()='eastBoundLongitude' or @maxx]", namespaceManager)?.InnerText;
            var maxYString = node.SelectSingleNode("*[local-name()='northBoundLatitude' or @maxy]", namespaceManager)?.InnerText;

            if (!double.TryParse(minXString, out var minX))
                return null;
            if (!double.TryParse(minYString, out var minY))
                return null;
            if (!double.TryParse(maxXString, out var maxX))
                return null;
            if (!double.TryParse(maxYString, out var maxY))
                return null;

            var bl = new Coordinate(crs, minX, minY);
            var tr = new Coordinate(crs, maxX, maxY);

            return new BoundingBox(bl, tr);
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
        public Dictionary<string, string> GetLegendUrls()
        {
            Dictionary<string, string> legendUrls = new Dictionary<string, string>();
            XmlNodeList layers = xmlDocument.GetElementsByTagName("Layer");

            foreach (XmlNode layer in layers)
            {
                string layerName = layer.SelectSingleNode("*[local-name()='Name']", namespaceManager)?.InnerText;
                if (string.IsNullOrEmpty(layerName)) continue;

                XmlNodeList legendNodes = layer.SelectNodes(".//*[local-name()='LegendURL']/*[local-name()='OnlineResource']", namespaceManager);
                foreach (XmlNode legendNode in legendNodes)
                {
                    string legendUrl = legendNode.Attributes["xlink:href"]?.Value;
                    if (!string.IsNullOrEmpty(legendUrl) && !legendUrls.ContainsKey(layerName))
                    {
                        legendUrls.Add(layerName, legendUrl);
                    }
                }
            }

            return legendUrls;
        }
    }
}